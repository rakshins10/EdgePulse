// EdgePulse MongoDB Initialization Script
// Creates telemetry collection with proper indexes
// Runs automatically on first container start

db = db.getSiblingDB('edgepulse_telemetry');

// Create telemetry readings collection
db.createCollection('readings');

// Index 1: deviceId + timestamp (most common query)
// "give me all readings for PUMP-LW-001 in last 24 hours"
db.readings.createIndex(
    { deviceId: 1, timestamp: -1 },
    { name: "idx_device_timestamp" }
);

// Index 2: tenantId + timestamp (tenant-level queries)
db.readings.createIndex(
    { tenantId: 1, timestamp: -1 },
    { name: "idx_tenant_timestamp" }
);

// Index 3: millId + timestamp (mill dashboard queries)
db.readings.createIndex(
    { millId: 1, timestamp: -1 },
    { name: "idx_mill_timestamp" }
);

// TTL Index: auto-expire documents after 12 months
// Mirrors Azure Cosmos DB TTL behaviour
db.readings.createIndex(
    { timestamp: 1 },
    {
        name: "idx_ttl_expire",
        expireAfterSeconds: 31536000
    }
);

print('EdgePulse MongoDB initialized successfully');
print('Collection: readings');
print('Indexes created: 4');
print('TTL: 12 months (31536000 seconds)');
