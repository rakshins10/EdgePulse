using EdgePulse.TelemetryProcessor;
using EdgePulse.TelemetryProcessor.Services;

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

        // Alert engine — singleton (holds in-memory breach counters)
        services.AddSingleton(sp =>
            new AlertEngineService(
                sp.GetRequiredService<ThresholdCacheService>(),
                sqlConnection,
                sp.GetRequiredService<ILogger<AlertEngineService>>()));

        // The RabbitMQ consumer worker
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
