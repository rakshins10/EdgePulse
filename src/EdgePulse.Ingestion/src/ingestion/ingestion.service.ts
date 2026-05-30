import {
  Injectable,
  Logger,
  UnauthorizedException,
  UnprocessableEntityException,
} from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { RabbitMqService } from '../messaging/rabbitmq.service';
import { IngestDto, TelemetryReading } from './dto/ingest.dto';

interface DeviceKeyContext {
  deviceId: string;
  tenantId: string;
  millId: string;
  areaId: string;
}

@Injectable()
export class IngestionService {
  private readonly logger = new Logger(IngestionService.name);
  private readonly apiUrl: string;
  private readonly serviceKey: string;

  constructor(
    private readonly config: ConfigService,
    private readonly rabbitmq: RabbitMqService,
  ) {
    this.apiUrl = this.config.getOrThrow<string>('EDGEPULSE_API_URL');
    this.serviceKey = this.config.getOrThrow<string>('SERVICE_KEY');
  }

  async ingest(deviceKey: string, dto: IngestDto): Promise<void> {
    // 1. Validate the device key against EdgePulse.API
    const context = await this.validateDeviceKey(deviceKey);

    // 2. Validate payload
    if (!dto.metrics || dto.metrics.length === 0) {
      throw new UnprocessableEntityException('metrics array must not be empty.');
    }

    // 3. Build enriched telemetry reading
    const reading: TelemetryReading = {
      deviceId: context.deviceId,
      tenantId: context.tenantId,
      millId: context.millId,
      areaId: context.areaId,
      timestamp: dto.timestamp ?? new Date().toISOString(),
      metrics: dto.metrics,
    };

    // 4. Publish to RabbitMQ
    await this.rabbitmq.publish(reading);

    this.logger.log(
      `Reading published — device ${context.deviceId} — ${reading.metrics.length} metric(s)`,
    );
  }

  private async validateDeviceKey(rawKey: string): Promise<DeviceKeyContext> {
    const url = `${this.apiUrl}/api/devices/validate-key`;

    let response: Response;
    try {
      response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Service-Key': this.serviceKey,
        },
        body: JSON.stringify({ apiKey: rawKey }),
      });
    } catch (err) {
      this.logger.error(`Failed to reach EdgePulse API: ${err}`);
      throw new UnauthorizedException('Unable to validate device key.');
    }

    if (response.status === 401) {
      throw new UnauthorizedException('Invalid or expired device API key.');
    }

    if (!response.ok) {
      this.logger.error(`Validation endpoint returned ${response.status}`);
      throw new UnauthorizedException('Unable to validate device key.');
    }

    return response.json() as Promise<DeviceKeyContext>;
  }
}
