using EdgePulse.TelemetryProcessor.Models;
using EdgePulse.TelemetryProcessor.Services;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EdgePulse.TelemetryProcessor;

/// <summary>
/// Background worker that:
///   1. Connects to RabbitMQ and subscribes to "telemetry.readings"
///   2. Deserializes each message to TelemetryReading
///   3. Stores the reading to MongoDB (time-series store)
///   4. Passes to AlertEngineService for threshold evaluation
/// </summary>
public class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly AlertEngineService _alertEngine;
    private readonly ThresholdCacheService _thresholdCache;
    private readonly ILogger<Worker> _logger;

    private IConnection? _rabbitConnection;
    private IChannel? _rabbitChannel;
    private IMongoCollection<TelemetryReading>? _mongoCollection;

    private const string QueueName = "telemetry.readings";

    public Worker(
        IConfiguration config,
        AlertEngineService alertEngine,
        ThresholdCacheService thresholdCache,
        ILogger<Worker> logger)
    {
        _config = config;
        _alertEngine = alertEngine;
        _thresholdCache = thresholdCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TelemetryProcessor starting...");

        // Pre-load threshold cache so the first messages get evaluated
        await _thresholdCache.RefreshAsync(stoppingToken);

        // MongoDB setup
        _mongoCollection = BuildMongoCollection();

        // RabbitMQ setup
        await ConnectRabbitMqAsync(stoppingToken);

        _logger.LogInformation(
            "TelemetryProcessor ready. Listening on queue '{Queue}'.", QueueName);

        // Wait until the host requests cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken)
            .ContinueWith(_ => Task.CompletedTask, CancellationToken.None);
    }

    private async Task ConnectRabbitMqAsync(CancellationToken ct)
    {
        var rabbitUri = _config.GetConnectionString("RabbitMQ")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:RabbitMQ is not configured.");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitUri)
        };

        _rabbitConnection = await factory.CreateConnectionAsync(ct);
        _rabbitChannel = await _rabbitConnection.CreateChannelAsync(
            cancellationToken: ct);

        // Durable queue — survives broker restart
        await _rabbitChannel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        // Process one message at a time — ensures ordering per device
        await _rabbitChannel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_rabbitChannel);
        consumer.ReceivedAsync += HandleMessageAsync;

        await _rabbitChannel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);
    }

    private async Task HandleMessageAsync(
        object sender,
        BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;

        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            var reading = JsonSerializer.Deserialize<TelemetryReading>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (reading is null)
            {
                _logger.LogWarning(
                    "Received null or undeserializable message. Nacking.");
                await _rabbitChannel!.BasicNackAsync(
                    deliveryTag, multiple: false, requeue: false);
                return;
            }

            // 1 — Store to MongoDB (time-series)
            reading.ReceivedAt = DateTime.UtcNow;
            await _mongoCollection!.InsertOneAsync(reading);

            // 2 — Evaluate alert thresholds
            await _alertEngine.EvaluateAsync(reading);

            // 3 — Acknowledge the message
            await _rabbitChannel!.BasicAckAsync(
                deliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing telemetry message {DeliveryTag}.", deliveryTag);

            // Nack without requeue — avoid poison message loop
            // Message goes to dead-letter queue if configured
            try
            {
                await _rabbitChannel!.BasicNackAsync(
                    deliveryTag, multiple: false, requeue: false);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx,
                    "Failed to nack message {DeliveryTag}.", deliveryTag);
            }
        }
    }

    private IMongoCollection<TelemetryReading> BuildMongoCollection()
    {
        var mongoUri = _config.GetConnectionString("MongoDB")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:MongoDB is not configured.");

        var database = _config["MongoDB:Database"] ?? "edgepulse_telemetry";

        var client = new MongoClient(mongoUri);
        var db = client.GetDatabase(database);
        return db.GetCollection<TelemetryReading>("telemetry_readings");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TelemetryProcessor stopping...");

        if (_rabbitChannel is not null)
        {
            await _rabbitChannel.CloseAsync(cancellationToken);
            _rabbitChannel.Dispose();
        }

        if (_rabbitConnection is not null)
        {
            await _rabbitConnection.CloseAsync(cancellationToken);
            _rabbitConnection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
