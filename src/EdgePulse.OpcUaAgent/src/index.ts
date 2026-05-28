import { loadSimulatorConfig } from "./config";
import { runAgent } from "./agent";
import { OpcUaSimulator } from "./simulator/OpcUaSimulator";

const isSimulateMode = process.argv.includes("--simulate");

async function main(): Promise<void> {
  if (isSimulateMode) {
    console.log("[EdgePulse] Starting OPC-UA Simulator (demo mode)...");
    const cfg = loadSimulatorConfig();
    const simulator = new OpcUaSimulator(cfg);

    const shutdown = async (signal: string) => {
      console.log(`\n[Simulator] ${signal} received — shutting down.`);
      await simulator.stopAsync();
      process.exit(0);
    };

    process.on("SIGINT",  () => void shutdown("SIGINT"));
    process.on("SIGTERM", () => void shutdown("SIGTERM"));

    await simulator.startAsync();
  } else {
    console.log("[EdgePulse] Starting OPC-UA Agent...");
    await runAgent();
  }
}

main().catch((err) => {
  console.error("[EdgePulse] Fatal error:", err);
  process.exit(1);
});
