/**
 * A single metric reading from a device sensor.
 * e.g. { key: "temperature", value: 75.5, unit: "°C" }
 */
export interface MetricReadingDto {
  key: string;
  value: number;
  unit?: string;
}

/**
 * Payload sent by a device to POST /ingest.
 * The device identity comes from the X-Device-Key header, not the body.
 */
export interface IngestDto {
  metrics: MetricReadingDto[];
  timestamp?: string; // ISO 8601 — defaults to server time if omitted
}

/**
 * The enriched reading published to RabbitMQ.
 * Combines the device payload with context resolved from the API key.
 */
export interface TelemetryReading {
  deviceId: string;
  tenantId: string;
  millId: string;
  areaId: string;
  timestamp: string;
  metrics: MetricReadingDto[];
}
