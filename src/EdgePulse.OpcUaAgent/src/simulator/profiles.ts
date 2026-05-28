/**
 * Simulation profiles for each device-metric combination.
 *
 * Values are derived from the DemoSeedService alert thresholds:
 *   - normalBase: midpoint of the healthy operating range
 *   - normalNoise: std-dev of Gaussian noise applied to normalBase
 *   - spikeValue: value that clearly exceeds the alert threshold
 *   - spikeIntervals: how many publish intervals the spike lasts
 *     (must be > consecutiveCount of the threshold — typically 3)
 *   - spikeProbabilityPerMin: average spike frequency
 *
 * All metric keys match those used in DemoSeedService.SeedAlertThresholdsAsync.
 */

export interface MetricProfile {
  key: string;
  unit: string;
  normalBase: number;
  normalNoise: number;
  spikeValue: number;
  spikeIntervals: number;   // intervals the spike persists (1 interval = publishIntervalMs)
  spikeProbabilityPerMin: number; // expected spikes per minute
  minClamp?: number;        // optional physical floor
  maxClamp?: number;        // optional physical ceiling
}

export interface DeviceProfile {
  browseName: string;       // OPC-UA object name
  deviceId: string;
  tenantId: string;
  millId: string;
  areaId: string;
  metrics: MetricProfile[];
}

const TENANT = "10000001-0000-0000-0000-000000000001";
const LAKEWOOD_MILL = "20000001-0000-0000-0000-000000000001";
const RIVERSIDE_MILL = "20000001-0000-0000-0000-000000000002";

// ── Lakewood Area IDs ─────────────────────────────────────────────────────────
const LW_FIBERLINE     = "30000001-0000-0000-0000-000000000001";
const LW_BLEACHING     = "30000001-0000-0000-0000-000000000002";
const LW_PAPER_MACHINE = "30000001-0000-0000-0000-000000000003";
const LW_RECOVERY      = "30000001-0000-0000-0000-000000000004";

// ── Riverside Area IDs ────────────────────────────────────────────────────────
const RV_FIBERLINE     = "30000002-0000-0000-0000-000000000001";
const RV_CHEM_RECOVERY = "30000002-0000-0000-0000-000000000002";
const RV_PAPER_MACHINE = "30000002-0000-0000-0000-000000000003";
const RV_UTILITIES     = "30000002-0000-0000-0000-000000000004";

export const DEVICE_PROFILES: DeviceProfile[] = [

  // ═══════════════════════════════ LAKEWOOD MILL ════════════════════════════

  {
    browseName: "LW_FeedWaterPump",
    deviceId: "40000001-0000-0000-0000-000000000001",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_FIBERLINE,
    metrics: [
      // bearing_temp  threshold: HIGH >75°C (x3), CRITICAL >85°C (x2)
      { key: "bearing_temp", unit: "°C",
        normalBase: 62, normalNoise: 3,
        spikeValue: 88, spikeIntervals: 4, spikeProbabilityPerMin: 0.15 },
      // vibration  threshold: HIGH >8 mm/s (x3)
      { key: "vibration", unit: "mm/s",
        normalBase: 3.5, normalNoise: 0.8,
        spikeValue: 9.5, spikeIntervals: 4, spikeProbabilityPerMin: 0.10,
        minClamp: 0 },
      // flow_rate  threshold: CRITICAL <20 m³/h (x3)
      { key: "flow_rate", unit: "m³/h",
        normalBase: 32, normalNoise: 2,
        spikeValue: 15, spikeIntervals: 4, spikeProbabilityPerMin: 0.08,
        minClamp: 0 },
    ],
  },

  {
    browseName: "LW_WhiteLiquorPump",
    deviceId: "40000001-0000-0000-0000-000000000002",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_FIBERLINE,
    metrics: [
      { key: "bearing_temp", unit: "°C",   normalBase: 58, normalNoise: 3,  spikeValue: 82, spikeIntervals: 4, spikeProbabilityPerMin: 0.08 },
      { key: "flow_rate",    unit: "m³/h", normalBase: 28, normalNoise: 2,  spikeValue: 12, spikeIntervals: 4, spikeProbabilityPerMin: 0.06, minClamp: 0 },
      { key: "discharge_pressure", unit: "bar", normalBase: 4.5, normalNoise: 0.3, spikeValue: 6.2, spikeIntervals: 3, spikeProbabilityPerMin: 0.05 },
    ],
  },

  {
    browseName: "LW_ChipFeederMotor",
    deviceId: "40000001-0000-0000-0000-000000000003",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_FIBERLINE,
    metrics: [
      // winding_temp  threshold: CRITICAL >105°C (x2)
      { key: "winding_temp", unit: "°C",
        normalBase: 78, normalNoise: 4,
        spikeValue: 110, spikeIntervals: 3, spikeProbabilityPerMin: 0.10 },
      // vibration  threshold: HIGH >10 mm/s (x3)
      { key: "vibration", unit: "mm/s",
        normalBase: 2.8, normalNoise: 0.6,
        spikeValue: 11.5, spikeIntervals: 4, spikeProbabilityPerMin: 0.08,
        minClamp: 0 },
      { key: "motor_current", unit: "A", normalBase: 42, normalNoise: 3, spikeValue: 55, spikeIntervals: 3, spikeProbabilityPerMin: 0.06 },
    ],
  },

  {
    browseName: "LW_ContinuousDigester",
    deviceId: "40000001-0000-0000-0000-000000000004",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_FIBERLINE,
    metrics: [
      // pressure  threshold: HIGH >7.5 bar (x3), CRITICAL >8.0 bar (x2)
      { key: "pressure", unit: "bar",
        normalBase: 6.5, normalNoise: 0.3,
        spikeValue: 8.4, spikeIntervals: 3, spikeProbabilityPerMin: 0.12 },
      // temperature  threshold: HIGH >180°C (x2)
      { key: "temperature", unit: "°C",
        normalBase: 162, normalNoise: 4,
        spikeValue: 184, spikeIntervals: 3, spikeProbabilityPerMin: 0.08 },
      { key: "kappa_number", unit: "", normalBase: 28, normalNoise: 2.5, spikeValue: 35, spikeIntervals: 3, spikeProbabilityPerMin: 0.05, minClamp: 0 },
    ],
  },

  {
    browseName: "LW_BleachPump",
    deviceId: "40000001-0000-0000-0000-000000000005",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_BLEACHING,
    metrics: [
      { key: "flow_rate",    unit: "m³/h", normalBase: 18, normalNoise: 1.5, spikeValue: 8,  spikeIntervals: 3, spikeProbabilityPerMin: 0.06, minClamp: 0 },
      { key: "bearing_temp", unit: "°C",   normalBase: 55, normalNoise: 3,   spikeValue: 78, spikeIntervals: 4, spikeProbabilityPerMin: 0.06 },
    ],
  },

  {
    browseName: "LW_PrimaryRefiner",
    deviceId: "40000001-0000-0000-0000-000000000006",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_BLEACHING,
    metrics: [
      // plate_gap  threshold: CRITICAL <0.02 mm (x3)
      { key: "plate_gap", unit: "mm",
        normalBase: 0.07, normalNoise: 0.008,
        spikeValue: 0.015, spikeIntervals: 4, spikeProbabilityPerMin: 0.10,
        minClamp: 0 },
      // motor_temp  threshold: HIGH >95°C (x3)
      { key: "motor_temp", unit: "°C",
        normalBase: 76, normalNoise: 4,
        spikeValue: 98, spikeIntervals: 4, spikeProbabilityPerMin: 0.10 },
      { key: "power_consumption", unit: "kW", normalBase: 1850, normalNoise: 80, spikeValue: 2400, spikeIntervals: 3, spikeProbabilityPerMin: 0.05 },
    ],
  },

  {
    browseName: "LW_PM1HeadBoxPump",
    deviceId: "40000001-0000-0000-0000-000000000007",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_PAPER_MACHINE,
    metrics: [
      { key: "flow_rate",    unit: "m³/h", normalBase: 95, normalNoise: 5, spikeValue: 50, spikeIntervals: 3, spikeProbabilityPerMin: 0.06, minClamp: 0 },
      { key: "discharge_pressure", unit: "bar", normalBase: 3.8, normalNoise: 0.2, spikeValue: 5.5, spikeIntervals: 3, spikeProbabilityPerMin: 0.05 },
      { key: "bearing_temp", unit: "°C", normalBase: 60, normalNoise: 3, spikeValue: 82, spikeIntervals: 4, spikeProbabilityPerMin: 0.06 },
    ],
  },

  {
    browseName: "LW_PM1DriveMotor",
    deviceId: "40000001-0000-0000-0000-000000000008",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_PAPER_MACHINE,
    metrics: [
      // winding_temp  threshold: HIGH >100°C (x3)
      { key: "winding_temp", unit: "°C",
        normalBase: 80, normalNoise: 5,
        spikeValue: 104, spikeIntervals: 4, spikeProbabilityPerMin: 0.10 },
      // vibration  threshold: CRITICAL >12 mm/s (x2)
      { key: "vibration", unit: "mm/s",
        normalBase: 2.5, normalNoise: 0.7,
        spikeValue: 13.5, spikeIntervals: 3, spikeProbabilityPerMin: 0.08,
        minClamp: 0 },
      { key: "speed_rpm",    unit: "rpm", normalBase: 860, normalNoise: 5, spikeValue: 920, spikeIntervals: 2, spikeProbabilityPerMin: 0.05, minClamp: 0 },
    ],
  },

  {
    browseName: "LW_RecoveryBoilerFeedPump",
    deviceId: "40000001-0000-0000-0000-000000000009",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_RECOVERY,
    metrics: [
      { key: "flow_rate",    unit: "m³/h", normalBase: 22, normalNoise: 1.5, spikeValue: 10, spikeIntervals: 3, spikeProbabilityPerMin: 0.06, minClamp: 0 },
      { key: "bearing_temp", unit: "°C",   normalBase: 62, normalNoise: 3,   spikeValue: 82, spikeIntervals: 4, spikeProbabilityPerMin: 0.06 },
      { key: "discharge_pressure", unit: "bar", normalBase: 12, normalNoise: 0.5, spikeValue: 16, spikeIntervals: 3, spikeProbabilityPerMin: 0.05 },
    ],
  },

  {
    browseName: "LW_RecoveryBoilerFanMotor",
    deviceId: "40000001-0000-0000-0000-000000000010",
    tenantId: TENANT, millId: LAKEWOOD_MILL, areaId: LW_RECOVERY,
    metrics: [
      { key: "winding_temp", unit: "°C",   normalBase: 72, normalNoise: 4, spikeValue: 102, spikeIntervals: 4, spikeProbabilityPerMin: 0.08 },
      { key: "vibration",    unit: "mm/s", normalBase: 2.0, normalNoise: 0.5, spikeValue: 9.5, spikeIntervals: 4, spikeProbabilityPerMin: 0.08, minClamp: 0 },
      { key: "airflow",      unit: "m³/s", normalBase: 18, normalNoise: 1,   spikeValue: 8,  spikeIntervals: 3, spikeProbabilityPerMin: 0.05, minClamp: 0 },
    ],
  },

  // ═══════════════════════════════ RIVERSIDE MILL ═══════════════════════════

  {
    browseName: "RV_FeedWaterPump",
    deviceId: "40000002-0000-0000-0000-000000000001",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_FIBERLINE,
    metrics: [
      // bearing_temp  threshold: HIGH >80°C (x3)
      { key: "bearing_temp", unit: "°C",
        normalBase: 65, normalNoise: 3,
        spikeValue: 84, spikeIntervals: 4, spikeProbabilityPerMin: 0.12 },
      // vibration  threshold: HIGH >8 mm/s (x3)
      { key: "vibration", unit: "mm/s",
        normalBase: 3.2, normalNoise: 0.8,
        spikeValue: 9.2, spikeIntervals: 4, spikeProbabilityPerMin: 0.10,
        minClamp: 0 },
      { key: "flow_rate", unit: "m³/h", normalBase: 30, normalNoise: 2, spikeValue: 14, spikeIntervals: 3, spikeProbabilityPerMin: 0.06, minClamp: 0 },
    ],
  },

  {
    browseName: "RV_ChipFeederMotor",
    deviceId: "40000002-0000-0000-0000-000000000002",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_FIBERLINE,
    metrics: [
      { key: "winding_temp", unit: "°C",   normalBase: 74, normalNoise: 4, spikeValue: 108, spikeIntervals: 3, spikeProbabilityPerMin: 0.08 },
      { key: "vibration",    unit: "mm/s", normalBase: 2.5, normalNoise: 0.6, spikeValue: 10.5, spikeIntervals: 4, spikeProbabilityPerMin: 0.08, minClamp: 0 },
    ],
  },

  {
    browseName: "RV_BatchDigester",
    deviceId: "40000002-0000-0000-0000-000000000003",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_FIBERLINE,
    metrics: [
      // pressure  threshold: CRITICAL >8.0 bar (x2)
      { key: "pressure", unit: "bar",
        normalBase: 6.2, normalNoise: 0.4,
        spikeValue: 8.5, spikeIntervals: 3, spikeProbabilityPerMin: 0.12 },
      // temperature  threshold: HIGH >178°C (x2)
      { key: "temperature", unit: "°C",
        normalBase: 155, normalNoise: 5,
        spikeValue: 182, spikeIntervals: 3, spikeProbabilityPerMin: 0.10 },
    ],
  },

  {
    browseName: "RV_BlackLiquorPump",
    deviceId: "40000002-0000-0000-0000-000000000004",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_CHEM_RECOVERY,
    metrics: [
      // pump_temp  threshold: HIGH >90°C (x3)
      { key: "pump_temp", unit: "°C",
        normalBase: 72, normalNoise: 4,
        spikeValue: 93, spikeIntervals: 4, spikeProbabilityPerMin: 0.12 },
      // flow_rate  threshold: HIGH <15 m³/h (x3)
      { key: "flow_rate", unit: "m³/h",
        normalBase: 22, normalNoise: 2,
        spikeValue: 11, spikeIntervals: 4, spikeProbabilityPerMin: 0.10,
        minClamp: 0 },
    ],
  },

  {
    browseName: "RV_GreenLiquorPump",
    deviceId: "40000002-0000-0000-0000-000000000005",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_CHEM_RECOVERY,
    metrics: [
      { key: "flow_rate",    unit: "m³/h", normalBase: 16, normalNoise: 1.5, spikeValue: 7,  spikeIntervals: 3, spikeProbabilityPerMin: 0.06, minClamp: 0 },
      { key: "bearing_temp", unit: "°C",   normalBase: 58, normalNoise: 3,   spikeValue: 80, spikeIntervals: 4, spikeProbabilityPerMin: 0.06 },
    ],
  },

  {
    browseName: "RV_RecoveryFanMotor",
    deviceId: "40000002-0000-0000-0000-000000000006",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_CHEM_RECOVERY,
    metrics: [
      { key: "winding_temp", unit: "°C",   normalBase: 70, normalNoise: 4, spikeValue: 100, spikeIntervals: 4, spikeProbabilityPerMin: 0.08 },
      { key: "vibration",    unit: "mm/s", normalBase: 2.2, normalNoise: 0.5, spikeValue: 9.0, spikeIntervals: 4, spikeProbabilityPerMin: 0.08, minClamp: 0 },
    ],
  },

  {
    browseName: "RV_PrimaryRefiner",
    deviceId: "40000002-0000-0000-0000-000000000007",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_PAPER_MACHINE,
    metrics: [
      { key: "motor_temp", unit: "°C",  normalBase: 75, normalNoise: 4, spikeValue: 98, spikeIntervals: 4, spikeProbabilityPerMin: 0.10 },
      { key: "plate_gap",  unit: "mm",  normalBase: 0.07, normalNoise: 0.008, spikeValue: 0.015, spikeIntervals: 4, spikeProbabilityPerMin: 0.08, minClamp: 0 },
      { key: "power_consumption", unit: "kW", normalBase: 1650, normalNoise: 70, spikeValue: 2200, spikeIntervals: 3, spikeProbabilityPerMin: 0.05 },
    ],
  },

  {
    browseName: "RV_PM1WhiteWaterPump",
    deviceId: "40000002-0000-0000-0000-000000000008",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_PAPER_MACHINE,
    metrics: [
      { key: "flow_rate",    unit: "m³/h", normalBase: 55, normalNoise: 4, spikeValue: 25, spikeIntervals: 3, spikeProbabilityPerMin: 0.06, minClamp: 0 },
      { key: "bearing_temp", unit: "°C",   normalBase: 60, normalNoise: 3, spikeValue: 82, spikeIntervals: 4, spikeProbabilityPerMin: 0.06 },
    ],
  },

  {
    browseName: "RV_CoolingWaterPump",
    deviceId: "40000002-0000-0000-0000-000000000009",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_UTILITIES,
    metrics: [
      { key: "flow_rate",    unit: "m³/h", normalBase: 72, normalNoise: 4, spikeValue: 30, spikeIntervals: 3, spikeProbabilityPerMin: 0.05, minClamp: 0 },
      { key: "outlet_temp",  unit: "°C",   normalBase: 28, normalNoise: 2, spikeValue: 42, spikeIntervals: 3, spikeProbabilityPerMin: 0.05 },
    ],
  },

  {
    browseName: "RV_MainDriveMotor",
    deviceId: "40000002-0000-0000-0000-000000000010",
    tenantId: TENANT, millId: RIVERSIDE_MILL, areaId: RV_UTILITIES,
    metrics: [
      // winding_temp  threshold: CRITICAL >110°C (x2)
      { key: "winding_temp", unit: "°C",
        normalBase: 82, normalNoise: 5,
        spikeValue: 114, spikeIntervals: 3, spikeProbabilityPerMin: 0.10 },
      // vibration  threshold: HIGH >9 mm/s (x3)
      { key: "vibration", unit: "mm/s",
        normalBase: 3.0, normalNoise: 0.8,
        spikeValue: 10.5, spikeIntervals: 4, spikeProbabilityPerMin: 0.10,
        minClamp: 0 },
      { key: "speed_rpm", unit: "rpm", normalBase: 1470, normalNoise: 8, spikeValue: 1560, spikeIntervals: 2, spikeProbabilityPerMin: 0.04, minClamp: 0 },
    ],
  },
];
