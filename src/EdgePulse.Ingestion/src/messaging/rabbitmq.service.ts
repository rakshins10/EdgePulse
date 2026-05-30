import { Injectable, Logger, OnModuleDestroy, OnModuleInit } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import * as amqp from 'amqplib';
import { TelemetryReading } from '../ingestion/dto/ingest.dto';

const EXCHANGE = 'edgepulse.telemetry';
const QUEUE = 'telemetry.readings';
const ROUTING_KEY = 'reading';

@Injectable()
export class RabbitMqService implements OnModuleInit, OnModuleDestroy {
  private readonly logger = new Logger(RabbitMqService.name);
  // In amqplib v2, connect() returns a ChannelModel (the connection + channel factory)
  private model: amqp.ChannelModel | null = null;
  private channel: amqp.Channel | null = null;

  constructor(private readonly config: ConfigService) {}

  async onModuleInit(): Promise<void> {
    const url = this.config.getOrThrow<string>('RABBITMQ_URL');
    await this.connect(url);
  }

  async onModuleDestroy(): Promise<void> {
    try {
      await this.channel?.close();
      await this.model?.close();
      this.logger.log('RabbitMQ connection closed.');
    } catch {
      // Ignore errors during shutdown
    }
  }

  async publish(reading: TelemetryReading): Promise<void> {
    if (!this.channel) {
      throw new Error('RabbitMQ channel not ready.');
    }

    const message = Buffer.from(JSON.stringify(reading));
    this.channel.publish(EXCHANGE, ROUTING_KEY, message, {
      contentType: 'application/json',
      persistent: true, // survive broker restart
    });
  }

  private async connect(url: string): Promise<void> {
    try {
      // amqplib v2: connect() returns ChannelModel
      this.model = await amqp.connect(url);
      this.channel = await this.model.createChannel();

      // Declare exchange and queue — idempotent, safe to call on every reconnect
      await this.channel.assertExchange(EXCHANGE, 'direct', { durable: true });
      // Declare the queue without DLX arguments so it matches the declaration
      // in TelemetryProcessor (Worker.cs) and OpcUaAgent (RabbitMqPublisher.ts).
      // All three services must declare with identical arguments or RabbitMQ
      // raises PRECONDITION_FAILED on the second assertQueue/QueueDeclare call.
      await this.channel.assertQueue(QUEUE, { durable: true });
      await this.channel.bindQueue(QUEUE, EXCHANGE, ROUTING_KEY);

      this.logger.log(
        `Connected to RabbitMQ — exchange: ${EXCHANGE}, queue: ${QUEUE}`,
      );

      // ChannelModel emits 'close' and 'error' events
      this.model.on('close', () => {
        this.logger.warn('RabbitMQ connection lost. Reconnecting in 5s...');
        this.model = null;
        this.channel = null;
        setTimeout(() => this.connect(url), 5000);
      });

      this.model.on('error', (err: Error) => {
        this.logger.error(`RabbitMQ error: ${err.message}`);
      });
    } catch (err) {
      this.logger.error(`Failed to connect to RabbitMQ: ${err}. Retrying in 5s...`);
      setTimeout(() => this.connect(url), 5000);
    }
  }
}
