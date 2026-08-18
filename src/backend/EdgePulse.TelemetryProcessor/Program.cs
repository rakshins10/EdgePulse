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

        // Fail fast if any connection string is still the committed placeholder.
        // Real values come from user-secrets (Development) or environment
        // variables (ConnectionStrings__SqlServer etc.) — never from git.
        foreach (var name in new[] { "SqlServer", "RabbitMQ", "MongoDB" })
        {
            var value = config.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(value) || value.Contains("<SET-VIA-"))
                throw new InvalidOperationException(
                    $"ConnectionStrings:{name} is not configured. Set it via " +
                    $"'dotnet user-secrets' (Development) or the ConnectionStrings__{name} " +
                    "environment variable. See docs/guides/02-configuration-guide.md.");
        }

        var sqlConnection = config.GetConnectionString("SqlServer")!;

        var refreshSeconds = config.GetValue<int>(
            "AlertEngine:ThresholdCacheRefreshSeconds", 60);

        // Threshold cache — singleton, in-memory, refreshes from SQL
        services.AddSingleton(sp =>
            new ThresholdCacheService(
                sqlConnection,
                refreshSeconds,
                sp.GetRequiredService<ILogger<ThresholdCacheService>>()));

        // Notification fan-out (in-app rows + SMTP email + webhooks) on alerts
        var smtpOptions = config.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
        var workOrderOptions = config.GetSection("WorkOrders").Get<WorkOrderOptions>() ?? new WorkOrderOptions();
        services.AddSingleton(sp =>
            new WebhookDispatcher(
                sqlConnection,
                sp.GetRequiredService<ILogger<WebhookDispatcher>>()));
        services.AddSingleton(sp =>
            new AlertNotifier(
                sqlConnection,
                smtpOptions,
                workOrderOptions,
                sp.GetRequiredService<WebhookDispatcher>(),
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
