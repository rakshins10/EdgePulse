import {
  OPCUAClient,
  MessageSecurityMode,
  SecurityPolicy,
  NodeClass,
  BrowseDirection,
  ClientSession,
  ReferenceDescription,
} from "node-opcua";

/**
 * OPC-UA auto-discovery (Sprint 25).
 *
 * Connects to a server, walks the address space from ObjectsFolder and
 * prints every discovered Object that carries Variable children as a
 * ready-to-paste `devices[]` snippet for the agent config — the missing
 * link between "a factory already has OPC-UA" and "EdgePulse is publishing
 * its telemetry".
 *
 * Usage:
 *   npm run discover                          # opc.tcp://localhost:4840
 *   npm run discover -- opc.tcp://plc:4840    # explicit endpoint
 */

interface DiscoveredVariable {
  nodeId: string;
  browseName: string;
  dataType: string;
}

interface DiscoveredObject {
  browseName: string;
  path: string;
  variables: DiscoveredVariable[];
}

const MAX_DEPTH = 4;
const MAX_NODES = 500;

function snakeCase(name: string): string {
  return name
    .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
    .replace(/[\s\-]+/g, "_")
    .toLowerCase();
}

async function browseChildren(
  session: ClientSession,
  nodeId: string
): Promise<ReferenceDescription[]> {
  const result = await session.browse({
    nodeId,
    browseDirection: BrowseDirection.Forward,
    includeSubtypes: true,
    nodeClassMask: NodeClass.Object | NodeClass.Variable,
    resultMask: 63,
  });
  return result.references ?? [];
}

async function walk(
  session: ClientSession,
  nodeId: string,
  path: string,
  depth: number,
  found: DiscoveredObject[],
  counter: { nodes: number }
): Promise<void> {
  if (depth > MAX_DEPTH || counter.nodes > MAX_NODES) return;

  const children = await browseChildren(session, nodeId);
  const variables: DiscoveredVariable[] = [];

  for (const ref of children) {
    counter.nodes++;
    const name = ref.browseName.name ?? "";
    // Skip the server's own diagnostics tree
    if (name === "Server" || name.startsWith("_")) continue;

    if (ref.nodeClass === NodeClass.Variable) {
      variables.push({
        nodeId: ref.nodeId.toString(),
        browseName: name,
        dataType: ref.typeDefinition?.toString() ?? "",
      });
    } else if (ref.nodeClass === NodeClass.Object) {
      await walk(
        session,
        ref.nodeId.toString(),
        path ? `${path}/${name}` : name,
        depth + 1,
        found,
        counter
      );
    }
  }

  // Only surface application nodes — variables that live entirely in the
  // server namespace (ns=0, e.g. Aliases/LastChange) are OPC-UA plumbing.
  const applicationVariables = variables.filter(
    (v) => !v.nodeId.startsWith("ns=0;")
  );
  if (applicationVariables.length > 0) {
    const objectName = path.split("/").pop() ?? path;
    found.push({ browseName: objectName, path, variables: applicationVariables });
  }
}

export async function runDiscovery(endpoint: string): Promise<void> {
  console.log(`[Discovery] Connecting to ${endpoint} ...`);

  const client = OPCUAClient.create({
    applicationName: "EdgePulse Discovery",
    securityMode: MessageSecurityMode.None,
    securityPolicy: SecurityPolicy.None,
    endpointMustExist: false,
    connectionStrategy: { maxRetry: 2, initialDelay: 1000 },
  });

  await client.connect(endpoint);
  const session = await client.createSession();
  console.log("[Discovery] Connected. Browsing address space ...\n");

  const found: DiscoveredObject[] = [];
  const counter = { nodes: 0 };
  // ObjectsFolder = ns=0;i=85
  await walk(session, "ns=0;i=85", "", 0, found, counter);

  await session.close();
  await client.disconnect();

  if (found.length === 0) {
    console.log("[Discovery] No objects with variables found.");
    return;
  }

  console.log(`[Discovery] Found ${found.length} device candidate(s), ` +
    `${found.reduce((n, o) => n + o.variables.length, 0)} variable(s):\n`);

  for (const obj of found) {
    console.log(`  ▪ ${obj.path}`);
    for (const v of obj.variables) {
      console.log(`      ${v.browseName.padEnd(24)} ${v.nodeId}`);
    }
  }

  // Ready-to-paste agent config snippet. deviceId/tenant ids must be filled
  // in by the operator (they come from EdgePulse device registration).
  const snippet = found.map((obj) => ({
    deviceId: "<EDGEPULSE-DEVICE-ID>",
    tenantId: "<TENANT-ID>",
    millId: "<MILL-ID>",
    areaId: "<AREA-ID>",
    name: obj.browseName,
    metrics: obj.variables.map((v) => ({
      nodeId: v.nodeId,
      key: snakeCase(v.browseName),
    })),
  }));

  console.log("\n[Discovery] Paste into the agent config `devices` array");
  console.log("(register each device in EdgePulse first, then fill the ids):\n");
  console.log(JSON.stringify(snippet, null, 2));
}
