// ── TelemetryReading ─────────────────────────────────────────────────────────
// Matches the C# TelemetryReading model in EdgePulse.TelemetryProcessor.
// JSON is published to RabbitMQ 'telemetry.readings' queue.

export interface MetricReading {
  key: string;
  value: number;
  unit?: string;
}

export interface TelemetryReading {
  deviceId: string;
  tenantId: string;
  millId: string;
  areaId: string;
  timestamp: string; // ISO 8601 UTC
  metrics: MetricReading[];
}

// ── Configuration ─────────────────────────────────────────────────────────────

export interface MetricConfig {
  /** OPC-UA node ID, e.g. "ns=1;s=NordPulp.LW_FeedWaterPump.bearing_temp" */
  nodeId: string;
  /** Metric key published to the processor, e.g. "bearing_temp" */
  key: string;
  /** Engineering unit sent with each reading, e.g. "°C" */
  unit?: string;
}

export interface DeviceConfig {
  deviceId: string;  // UUID matching DemoIds
  tenantId: string;
  millId: string;
  areaId: string;
  /** Human-readable label used in log output only */
  label?: string;
  metrics: MetricConfig[];
}

export interface OpcUaConfig {
  /** OPC-UA server endpoint, e.g. "opc.tcp://opcua-simulator:4840" */
  serverUrl: string;
  /** Milliseconds between reconnect attempts */
  reconnectDelayMs: number;
  /** Sampling interval for monitored items (ms) */
  samplingIntervalMs: number;
}

export interface RabbitMqConfig {
  /** AMQP URL, e.g. "amqp://user:pass@host:5672/vhost" */
  url: string;
  queue: string;
  /** Whether to declare the queue as durable (must match TelemetryProcessor) */
  durable: boolean;
  /** Milliseconds between reconnect attempts */
  reconnectDelayMs: number;
}

export interface AgentConfig {
  /** Friendly name shown in logs */
  name: string;
  /**
   * How often to snapshot all device metrics and publish to RabbitMQ.
   * Should be >= samplingIntervalMs.
   */
  publishIntervalMs: number;
  opcua: OpcUaConfig;
  rabbitmq: RabbitMqConfig;
  devices: DeviceConfig[];
}

// ── Simulator ─────────────────────────────────────────────────────────────────

export interface SimulatorConfig {
  /** TCP port the OPC-UA server listens on */
  port: number;
  /** Update interval for simulated values (ms) */
  updateIntervalMs: number;
}
