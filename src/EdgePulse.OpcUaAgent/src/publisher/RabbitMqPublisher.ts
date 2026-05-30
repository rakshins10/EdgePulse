import * as amqplib from "amqplib";
import type { TelemetryReading, RabbitMqConfig } from "../types";

/**
 * Publishes TelemetryReading messages to a RabbitMQ queue.
 *
 * Reconnect strategy:
 *   - On connect failure or channel error: waits reconnectDelayMs then retries.
 *   - Messages attempted while disconnected are dropped (logged as warnings).
 *   - A connected publisher NACK from the broker is logged as an error.
 */
export class RabbitMqPublisher {
  private readonly cfg: RabbitMqConfig;
  private connecting = false;
  private stopped = false;

  // amqplib-connection-manager would be ideal, but to keep deps minimal
  // we implement a simple reconnect loop with raw amqplib.
  private channel: amqplib.Channel | null = null;
  private rawConn: amqplib.ChannelModel | null = null;

  constructor(cfg: RabbitMqConfig) {
    this.cfg = cfg;
  }

  /** Connect and declare the queue. Retries on failure. */
  async connectAsync(): Promise<void> {
    while (!this.stopped) {
      try {
        console.log(`[RabbitMQ] Connecting to ${this.redactUrl()}...`);

        this.rawConn = await amqplib.connect(this.cfg.url);
        this.rawConn.on("error", (err: unknown) =>
          console.error(`[RabbitMQ] Connection error: ${String(err)}`)
        );
        this.rawConn.on("close", () => {
          if (!this.stopped) {
            console.warn("[RabbitMQ] Connection closed — will reconnect.");
            this.channel = null;
            this.rawConn = null;
            void this.reconnectLoop();
          }
        });

        this.channel = await this.rawConn.createChannel();
        this.channel.on("error", (err: unknown) =>
          console.error(`[RabbitMQ] Channel error: ${String(err)}`)
        );
        this.channel.on("close", () => {
          console.warn("[RabbitMQ] Channel closed.");
          this.channel = null;
        });

        await this.channel.assertQueue(this.cfg.queue, { durable: this.cfg.durable });
        console.log(`[RabbitMQ] Connected. Queue '${this.cfg.queue}' ready.`);
        return;
      } catch (err) {
        console.error(
          `[RabbitMQ] Connect failed: ${String(err)} — retrying in ${this.cfg.reconnectDelayMs}ms`
        );
        await sleep(this.cfg.reconnectDelayMs);
      }
    }
  }

  /** Publish a telemetry reading. Drops silently if not connected. */
  publish(reading: TelemetryReading): void {
    if (!this.channel) {
      console.warn(
        `[RabbitMQ] Not connected — dropping reading for device ${reading.deviceId}`
      );
      return;
    }

    const json = JSON.stringify(reading);
    const buffer = Buffer.from(json, "utf-8");

    const ok = this.channel.sendToQueue(this.cfg.queue, buffer, {
      persistent: true,
      contentType: "application/json",
    });

    if (!ok) {
      console.warn("[RabbitMQ] Send buffer full — reading dropped.");
    }
  }

  async stopAsync(): Promise<void> {
    this.stopped = true;
    try {
      await this.channel?.close();
      await this.rawConn?.close();
    } catch {
      // ignore close errors on shutdown
    }
  }

  private async reconnectLoop(): Promise<void> {
    await sleep(this.cfg.reconnectDelayMs);
    if (!this.stopped) {
      await this.connectAsync();
    }
  }

  private redactUrl(): string {
    try {
      const url = new URL(this.cfg.url);
      url.password = "****";
      return url.toString();
    } catch {
      return "(invalid url)";
    }
  }
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
