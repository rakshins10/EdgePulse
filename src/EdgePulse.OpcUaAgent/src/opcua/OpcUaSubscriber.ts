import {
  OPCUAClient,
  MessageSecurityMode,
  SecurityPolicy,
  AttributeIds,
  ClientMonitoredItem,
  TimestampsToReturn,
  type DataValue,
  type ClientSession,
  type ClientSubscription,
} from "node-opcua";
import type { AgentConfig, DeviceConfig, MetricReading, TelemetryReading } from "../types";
import type { RabbitMqPublisher } from "../publisher/RabbitMqPublisher";

/**
 * Connects to an OPC-UA server, monitors all configured node IDs,
 * and periodically publishes batched TelemetryReading messages to RabbitMQ.
 *
 * Batching:
 *   - Each OPC-UA value-change event updates an in-memory snapshot.
 *   - Every publishIntervalMs, one TelemetryReading per device is built
 *     from the latest snapshot and published.
 *   - Devices with no readings yet (e.g. OPC-UA nodes not yet live) are skipped.
 */
export class OpcUaSubscriber {
  private readonly cfg: AgentConfig;
  private readonly publisher: RabbitMqPublisher;

  // Latest known value for each (deviceId, metricKey)
  private readonly snapshots = new Map<string, Map<string, MetricReading>>();

  private client: OPCUAClient | null = null;
  private session: ClientSession | null = null;
  private subscription: ClientSubscription | null = null;
  private publishTimer: NodeJS.Timeout | null = null;
  private stopped = false;

  constructor(cfg: AgentConfig, publisher: RabbitMqPublisher) {
    this.cfg = cfg;
    this.publisher = publisher;

    // Pre-create snapshot maps for each device
    for (const device of cfg.devices) {
      this.snapshots.set(device.deviceId, new Map());
    }
  }

  async startAsync(): Promise<void> {
    await this.connectWithRetry();
    this.startPublishTimer();
  }

  async stopAsync(): Promise<void> {
    this.stopped = true;
    if (this.publishTimer) {
      clearInterval(this.publishTimer);
      this.publishTimer = null;
    }
    await this.teardown();
  }

  // ── Connection lifecycle ───────────────────────────────────────────────────

  private async connectWithRetry(): Promise<void> {
    while (!this.stopped) {
      try {
        await this.connect();
        return;
      } catch (err) {
        console.error(
          `[OPC-UA] Connect failed: ${String(err)} — retrying in ${this.cfg.opcua.reconnectDelayMs}ms`
        );
        await sleep(this.cfg.opcua.reconnectDelayMs);
      }
    }
  }

  private async connect(): Promise<void> {
    console.log(`[OPC-UA] Connecting to ${this.cfg.opcua.serverUrl}...`);

    this.client = OPCUAClient.create({
      applicationName: this.cfg.name,
      connectionStrategy: {
        initialDelay: this.cfg.opcua.reconnectDelayMs,
        maxRetry: 1,
      },
      securityMode: MessageSecurityMode.None,
      securityPolicy: SecurityPolicy.None,
      endpointMustExist: false,
      keepSessionAlive: true,
    });

    this.client.on("backoff", (_retry: number, delay: number) => {
      console.log(`[OPC-UA] Connection retry in ${delay}ms...`);
    });

    this.client.on("connection_lost", () => {
      console.warn("[OPC-UA] Connection lost — attempting reconnect.");
      if (!this.stopped) {
        void this.handleReconnect();
      }
    });

    await this.client.connect(this.cfg.opcua.serverUrl);
    console.log("[OPC-UA] Connected.");

    this.session = await this.client.createSession();
    console.log("[OPC-UA] Session created.");

    this.subscription = await this.session.createSubscription2({
      requestedPublishingInterval: this.cfg.opcua.samplingIntervalMs,
      requestedMaxKeepAliveCount: 20,
      requestedLifetimeCount: 100,
      maxNotificationsPerPublish: 1000,
      publishingEnabled: true,
      priority: 1,
    });

    await this.monitorAll();
    console.log(
      `[OPC-UA] Monitoring ${this.cfg.devices.reduce((n, d) => n + d.metrics.length, 0)} nodes ` +
      `across ${this.cfg.devices.length} devices.`
    );
  }

  private async handleReconnect(): Promise<void> {
    await this.teardown();
    await sleep(this.cfg.opcua.reconnectDelayMs);
    await this.connectWithRetry();
  }

  private async teardown(): Promise<void> {
    try {
      await this.subscription?.terminate();
    } catch { /* ignore */ }
    try {
      await this.session?.close();
    } catch { /* ignore */ }
    try {
      await this.client?.disconnect();
    } catch { /* ignore */ }
    this.subscription = null;
    this.session = null;
    this.client = null;
  }

  // ── Monitoring ─────────────────────────────────────────────────────────────

  private async monitorAll(): Promise<void> {
    for (const device of this.cfg.devices) {
      for (const metric of device.metrics) {
        await this.monitorItem(device, metric.nodeId, metric.key, metric.unit);
      }
    }
  }

  private async monitorItem(
    device: DeviceConfig,
    nodeId: string,
    metricKey: string,
    unit?: string
  ): Promise<void> {
    if (!this.subscription) return;

    const monitoredItem = ClientMonitoredItem.create(
      this.subscription,
      { nodeId, attributeId: AttributeIds.Value },
      {
        samplingInterval: this.cfg.opcua.samplingIntervalMs,
        discardOldest: true,
        queueSize: 1,
      },
      TimestampsToReturn.Source
    );

    monitoredItem.on("changed", (dataValue: DataValue) => {
      if (dataValue.statusCode.value !== 0) return; // bad quality
      const rawValue = dataValue.value?.value;
      if (typeof rawValue !== "number" || isNaN(rawValue)) return;

      const deviceMap = this.snapshots.get(device.deviceId);
      if (deviceMap) {
        deviceMap.set(metricKey, { key: metricKey, value: rawValue, unit });
      }
    });

    monitoredItem.on("err", (msg: string) => {
      console.warn(`[OPC-UA] Monitor error for ${nodeId}: ${msg}`);
    });
  }

  // ── Publish timer ──────────────────────────────────────────────────────────

  private startPublishTimer(): void {
    this.publishTimer = setInterval(() => {
      this.publishSnapshot();
    }, this.cfg.publishIntervalMs);
  }

  private publishSnapshot(): void {
    const timestamp = new Date().toISOString();

    for (const device of this.cfg.devices) {
      const deviceMap = this.snapshots.get(device.deviceId);
      if (!deviceMap || deviceMap.size === 0) continue;

      const metrics = Array.from(deviceMap.values());
      const reading: TelemetryReading = {
        deviceId: device.deviceId,
        tenantId: device.tenantId,
        millId: device.millId,
        areaId: device.areaId,
        timestamp,
        metrics,
      };

      this.publisher.publish(reading);
    }
  }
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
