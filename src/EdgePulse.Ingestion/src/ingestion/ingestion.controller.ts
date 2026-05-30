import {
  Body,
  Controller,
  Get,
  Headers,
  HttpCode,
  HttpStatus,
  Post,
  UnauthorizedException,
  UnprocessableEntityException,
} from '@nestjs/common';
import { IngestionService } from './ingestion.service';
import { IngestDto } from './dto/ingest.dto';

@Controller()
export class IngestionController {
  constructor(private readonly ingestionService: IngestionService) {}

  /**
   * GET /health
   * Used by Docker health checks and HAProxy. No authentication required.
   */
  @Get('health')
  health(): { status: string } {
    return { status: 'healthy' };
  }

  /**
   * POST /ingest
   *
   * Accepts telemetry from a registered device.
   *
   * Headers:
   *   X-Device-Key: <raw API key returned at device registration>
   *
   * Body:
   *   {
   *     "metrics": [
   *       { "key": "temperature", "value": 75.5, "unit": "°C" },
   *       { "key": "vibration",   "value": 2.1,  "unit": "mm/s" }
   *     ],
   *     "timestamp": "2026-05-24T08:00:00Z"  // optional — defaults to now
   *   }
   *
   * Responses:
   *   202 Accepted  — reading accepted and queued
   *   401 Unauthorized — missing/invalid device key
   *   422 Unprocessable — empty metrics array or malformed body
   */
  @Post('ingest')
  @HttpCode(HttpStatus.ACCEPTED)
  async ingest(
    @Headers('x-device-key') deviceKey: string | undefined,
    @Body() body: IngestDto,
  ): Promise<{ status: string }> {
    if (!deviceKey) {
      throw new UnauthorizedException('X-Device-Key header is required.');
    }

    if (!body?.metrics || !Array.isArray(body.metrics)) {
      throw new UnprocessableEntityException('metrics must be an array.');
    }

    await this.ingestionService.ingest(deviceKey, body);
    return { status: 'accepted' };
  }
}
