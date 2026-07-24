using EdgePulse.TelemetryProcessor;
using EdgePulse.TelemetryProcessor.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// MongoDB.Driver 3.x breaking change: GuidRepresentation is Unspecified by default.
// Register globally so POCO Guid properties serialize as BSON strings.
BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        var sqlConnection = config.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:SqlServer is not configured.");

        var refreshSeconds = config.GetValue<int>(
            "AlertEngine:ThresholdCacheRefreshSeconds", 60);

        // Threshold cache — singleton, in-memory, refreshes from SQL
        services.AddSingleton(sp =>
            new ThresholdCacheService(
                sqlConnection,
                refreshSeconds,
                sp.GetRequiredService<ILogger<ThresholdCacheService>>()));

        // Notification fan-out (in-app rows + SMTP email) when alerts fire
        var smtpOptions = config.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
        var workOrderOptions = config.GetSection("WorkOrders").Get<WorkOrderOptions>() ?? new WorkOrderOptions();
        services.AddSingleton(sp =>
            new AlertNotifier(
                sqlConnection,
                smtpOptions,
                workOrderOptions,
                sp.GetRequiredService<ILogger<AlertNotifier>>()));

        // Alert engine — singleton (holds in-memory breach counters)
        services.AddSingleton(sp =>
            new AlertEngineService(
                sp.GetRequiredService<ThresholdCacheService>(),
                sqlConnection,
                sp.GetRequiredService<AlertNotifier>(),
                sp.GetRequiredService<ILogger<AlertEngineService>>()));

        // The RabbitMQ consumer worker
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
