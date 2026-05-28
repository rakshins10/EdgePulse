import * as fs from "fs";
import * as path from "path";
import type { AgentConfig, SimulatorConfig } from "./types";

const DEFAULT_CONFIG_PATH = path.resolve(__dirname, "../config/config.nordpulp.json");

/**
 * Loads and validates the agent configuration from a JSON file.
 * Path is resolved from:
 *   1. CONFIG_PATH environment variable
 *   2. Default: config/config.nordpulp.json (relative to the project root)
 */
export function loadAgentConfig(): AgentConfig {
  const configPath = process.env["CONFIG_PATH"] ?? DEFAULT_CONFIG_PATH;

  if (!fs.existsSync(configPath)) {
    throw new Error(
      `Configuration file not found: ${configPath}\n` +
      `Set the CONFIG_PATH environment variable or place config.nordpulp.json in the config/ directory.`
    );
  }

  const raw = fs.readFileSync(configPath, "utf-8");
  let cfg: AgentConfig;

  try {
    cfg = JSON.parse(raw) as AgentConfig;
  } catch (err) {
    throw new Error(`Failed to parse configuration file ${configPath}: ${String(err)}`);
  }

  validateAgentConfig(cfg, configPath);
  return cfg;
}

/**
 * Loads simulator configuration.
 * Port is read from OPCUA_PORT env var, default 4840.
 */
export function loadSimulatorConfig(): SimulatorConfig {
  const port = parseInt(process.env["OPCUA_PORT"] ?? "4840", 10);
  const updateIntervalMs = parseInt(process.env["SIMULATOR_UPDATE_MS"] ?? "2000", 10);
  return { port, updateIntervalMs };
}

function validateAgentConfig(cfg: AgentConfig, path: string): void {
  const errors: string[] = [];

  if (!cfg.opcua?.serverUrl) errors.push("opcua.serverUrl is required");
  if (!cfg.rabbitmq?.url) errors.push("rabbitmq.url is required");
  if (!cfg.rabbitmq?.queue) errors.push("rabbitmq.queue is required");
  if (!Array.isArray(cfg.devices) || cfg.devices.length === 0) {
    errors.push("devices array must have at least one entry");
  }

  cfg.devices?.forEach((d, i) => {
    if (!d.deviceId) errors.push(`devices[${i}].deviceId is required`);
    if (!d.tenantId) errors.push(`devices[${i}].tenantId is required`);
    if (!Array.isArray(d.metrics) || d.metrics.length === 0) {
      errors.push(`devices[${i}].metrics must have at least one entry`);
    }
    d.metrics?.forEach((m, j) => {
      if (!m.nodeId) errors.push(`devices[${i}].metrics[${j}].nodeId is required`);
      if (!m.key) errors.push(`devices[${i}].metrics[${j}].key is required`);
    });
  });

  if (errors.length > 0) {
    throw new Error(
      `Configuration validation failed for ${path}:\n  - ${errors.join("\n  - ")}`
    );
  }

  // Apply defaults
  cfg.name ??= "edgepulse-opcua-agent";
  cfg.publishIntervalMs ??= 5000;
  cfg.opcua.reconnectDelayMs ??= 5000;
  cfg.opcua.samplingIntervalMs ??= 1000;
  cfg.rabbitmq.durable ??= true;
  cfg.rabbitmq.reconnectDelayMs ??= 5000;
}
