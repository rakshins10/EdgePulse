import {
  OPCUAServer,
  DataType,
  Variant,
  type AddressSpace,
  type UAObject,
  type Namespace,
} from "node-opcua";
import { DEVICE_PROFILES, type MetricProfile } from "./profiles";
import type { SimulatorConfig } from "../types";

/**
 * OPC-UA server that generates synthetic Pulp & Paper sensor data.
 *
 * Value generation per metric:
 *   - NORMAL mode: normalBase + Gaussian(0, normalNoise)
 *   - SPIKE mode:  spikeValue + small noise; lasts spikeIntervals cycles
 *   - Transition NORMAL → SPIKE occurs randomly at spikeProbabilityPerMin
 *   - After spikeIntervals, value returns to NORMAL for at least 10 cycles
 *     (prevents immediate re-entry into spike, giving alert time to resolve)
 *
 * Node IDs use the pattern: ns=1;s=NordPulp.<DeviceBrowseName>.<metricKey>
 * This matches the nodeId strings in config/config.nordpulp.json.
 */
export class OpcUaSimulator {
  private readonly cfg: SimulatorConfig;
  private server?: OPCUAServer;
  private updateTimer?: NodeJS.Timeout;

  // Per-metric state: current value + spike countdown
  private readonly state = new Map<string, { value: number; spikeCountdown: number; cooldown: number }>();

  constructor(cfg: SimulatorConfig) {
    this.cfg = cfg;
  }

  async startAsync(): Promise<void> {
    this.server = new OPCUAServer({
      port: this.cfg.port,
      resourcePath: "/UA/EdgePulse",
      buildInfo: {
        productName: "EdgePulse OPC-UA Simulator",
        buildNumber: "1",
        buildDate: new Date(),
      },
      // Allow anonymous access for demo
      allowAnonymous: true,
    });

    await this.server.initialize();

    const addressSpace = this.server.engine.addressSpace as AddressSpace;
    const namespace = addressSpace.getOwnNamespace() as Namespace;

    // Root folder for all NordPulp devices
    const nordPulpFolder = namespace.addObject({
      organizedBy: addressSpace.rootFolder.objects,
      browseName: "NordPulp",
      nodeId: "ns=1;s=NordPulp",
    });

    // Create one OPC-UA object per device and one variable per metric
    for (const device of DEVICE_PROFILES) {
      const deviceObj = namespace.addObject({
        organizedBy: nordPulpFolder,
        browseName: device.browseName,
        nodeId: `ns=1;s=NordPulp.${device.browseName}`,
      }) as UAObject;

      for (const metric of device.metrics) {
        const stateKey = `${device.browseName}.${metric.key}`;

        // Initialise state
        this.state.set(stateKey, {
          value: this.normalValue(metric),
          spikeCountdown: 0,
          cooldown: 0,
        });

        const capturedStateKey = stateKey;
        const capturedMetric = metric;

        namespace.addVariable({
          componentOf: deviceObj,
          browseName: metric.key,
          nodeId: `ns=1;s=NordPulp.${device.browseName}.${metric.key}`,
          dataType: DataType.Double,
          value: {
            get: () => {
              const s = this.state.get(capturedStateKey);
              return new Variant({
                dataType: DataType.Double,
                value: s?.value ?? capturedMetric.normalBase,
              });
            },
          },
        });
      }
    }

    await this.server.start();
    console.log(
      `[Simulator] OPC-UA server running on opc.tcp://0.0.0.0:${this.cfg.port}`
    );
    console.log(
      `[Simulator] Exposing ${DEVICE_PROFILES.length} devices, ` +
      `${DEVICE_PROFILES.reduce((n, d) => n + d.metrics.length, 0)} metrics.`
    );

    // Start the value update loop
    this.updateTimer = setInterval(
      () => this.updateAllValues(),
      this.cfg.updateIntervalMs
    );
  }

  async stopAsync(): Promise<void> {
    if (this.updateTimer) {
      clearInterval(this.updateTimer);
    }
    await this.server?.shutdown(500);
    console.log("[Simulator] Stopped.");
  }

  // ── Value generation ───────────────────────────────────────────────────────

  private updateAllValues(): void {
    const intervalMinutes = this.cfg.updateIntervalMs / 60_000;

    for (const device of DEVICE_PROFILES) {
      for (const metric of device.metrics) {
        const stateKey = `${device.browseName}.${metric.key}`;
        const s = this.state.get(stateKey);
        if (!s) continue;

        if (s.spikeCountdown > 0) {
          // In spike — keep spiking with small noise
          s.value = clamp(
            metric.spikeValue + gaussianNoise(metric.normalNoise * 0.3),
            metric.minClamp ?? -Infinity,
            metric.maxClamp ?? Infinity
          );
          s.spikeCountdown--;
          if (s.spikeCountdown === 0) {
            s.cooldown = 10; // 10 intervals before next spike is allowed
          }
        } else if (s.cooldown > 0) {
          // In cooldown — return to normal
          s.value = this.normalValue(metric);
          s.cooldown--;
        } else {
          // Normal operation — maybe start a spike
          const spikeProb = metric.spikeProbabilityPerMin * intervalMinutes;
          if (Math.random() < spikeProb) {
            s.spikeCountdown = metric.spikeIntervals;
            s.value = clamp(
              metric.spikeValue + gaussianNoise(metric.normalNoise * 0.3),
              metric.minClamp ?? -Infinity,
              metric.maxClamp ?? Infinity
            );
          } else {
            s.value = this.normalValue(metric);
          }
        }
      }
    }
  }

  private normalValue(metric: MetricProfile): number {
    return clamp(
      metric.normalBase + gaussianNoise(metric.normalNoise),
      metric.minClamp ?? -Infinity,
      metric.maxClamp ?? Infinity
    );
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Box-Muller Gaussian noise with std-dev = stddev */
function gaussianNoise(stddev: number): number {
  const u1 = Math.random();
  const u2 = Math.random();
  const z = Math.sqrt(-2 * Math.log(Math.max(u1, 1e-10))) * Math.cos(2 * Math.PI * u2);
  return z * stddev;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}
