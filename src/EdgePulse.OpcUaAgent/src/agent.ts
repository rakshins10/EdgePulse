import { loadAgentConfig } from "./config";
import { RabbitMqPublisher } from "./publisher/RabbitMqPublisher";
import { OpcUaSubscriber } from "./opcua/OpcUaSubscriber";

/**
 * Runs the OPC-UA agent:
 *   1. Load config
 *   2. Connect to RabbitMQ
 *   3. Connect to OPC-UA server and start monitoring
 *   4. Publish batched TelemetryReading messages at publishIntervalMs
 */
export async function runAgent(): Promise<void> {
  const cfg = loadAgentConfig();

  console.log(`[Agent] Starting EdgePulse OPC-UA Agent "${cfg.name}"`);
  console.log(`[Agent] OPC-UA server: ${cfg.opcua.serverUrl}`);
  console.log(`[Agent] Devices: ${cfg.devices.length}`);
  console.log(`[Agent] Publish interval: ${cfg.publishIntervalMs}ms`);

  const publisher = new RabbitMqPublisher(cfg.rabbitmq);
  const subscriber = new OpcUaSubscriber(cfg, publisher);

  // Graceful shutdown
  const shutdown = async (signal: string) => {
    console.log(`\n[Agent] ${signal} received — shutting down.`);
    await subscriber.stopAsync();
    await publisher.stopAsync();
    process.exit(0);
  };

  process.on("SIGINT",  () => void shutdown("SIGINT"));
  process.on("SIGTERM", () => void shutdown("SIGTERM"));

  // Start both concurrently — RabbitMQ connect and OPC-UA connect both retry internally
  await Promise.all([
    publisher.connectAsync(),
    subscriber.startAsync(),
  ]);

  console.log("[Agent] Running. Press Ctrl+C to stop.");
}
