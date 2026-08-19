using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Ai;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace EdgePulse.Application.Tests.Features.Ai;

/// <summary>
/// The summary handler is tested with a FAKE IAiAssistant — no model, no
/// network. That is the point of the abstraction: all the logic around the
/// model (caching, disabled path, failure path, prompt contents) is
/// deterministic and unit-testable; only the provider classes talk to a server.
/// </summary>
public class AlertSummaryTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (InMemoryApplicationDbContext ctx, TestCurrentUserService user, Alert alert) Seed()
    {
        var ctx = TestDbContextFactory.Create();
        var user = TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var type = DeviceType.CreateCustomValue(_tenantId, "Centrifugal Pump", "PUMP");
        ctx.Add(type);
        var device = Device.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), type.Id, Guid.NewGuid(), "Feed Water Pump", "PUMP-LW-001");
        ctx.Add(device);
        var alert = Alert.Create(_tenantId, device.Id, device.MillId, device.AreaId, Guid.NewGuid(),
            "bearing_temp", 92.5, 75, "HIGH", "C",
            readingsJson: "[{\"Timestamp\":\"2026-01-01T00:00:00Z\",\"Value\":80},{\"Timestamp\":\"2026-01-01T00:00:05Z\",\"Value\":86},{\"Timestamp\":\"2026-01-01T00:00:10Z\",\"Value\":92.5}]");
        ctx.Add(alert);
        ctx.SaveChanges();
        return (ctx, user, alert);
    }

    private static IAiAssistant Ai(bool enabled, string? reply)
    {
        var ai = Substitute.For<IAiAssistant>();
        ai.IsEnabled.Returns(enabled);
        ai.Description.Returns(enabled ? "ollama/llama3.2" : "disabled");
        ai.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
          .Returns(Task.FromResult(reply));
        return ai;
    }

    [Fact]
    public async Task GeneratesSummary_CachesOnAlert_AndReportsProvider()
    {
        var (ctx, user, alert) = Seed();
        var ai = Ai(true, "WHAT HAPPENED: bearing temperature exceeded 75 C.");
        var handler = new GetAlertSummaryQueryHandler(ctx, user, ai);

        var result = await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);

        result.Available.Should().BeTrue();
        result.FromCache.Should().BeFalse();
        result.Provider.Should().Be("ollama/llama3.2");
        result.Summary.Should().Contain("WHAT HAPPENED");
        ctx.AlertSet.Find(alert.Id)!.AiSummary.Should().Contain("WHAT HAPPENED"); // persisted
    }

    [Fact]
    public async Task SecondCall_ServesFromCache_WithoutCallingModel()
    {
        var (ctx, user, alert) = Seed();
        var ai = Ai(true, "first answer");
        var handler = new GetAlertSummaryQueryHandler(ctx, user, ai);

        await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);
        var second = await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);

        second.FromCache.Should().BeTrue();
        second.Summary.Should().Be("first answer");
        await ai.Received(1).CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Regenerate_BypassesCache_AndCallsModelAgain()
    {
        var (ctx, user, alert) = Seed();
        var ai = Ai(true, "answer");
        var handler = new GetAlertSummaryQueryHandler(ctx, user, ai);

        await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);
        var again = await handler.Handle(new GetAlertSummaryQuery(alert.Id, Regenerate: true), CancellationToken.None);

        again.FromCache.Should().BeFalse();
        await ai.Received(2).CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AiDisabled_ReturnsUnavailable_WithoutError_AndDoesNotTouchAlert()
    {
        var (ctx, user, alert) = Seed();
        var handler = new GetAlertSummaryQueryHandler(ctx, user, Ai(false, null));

        var result = await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);

        result.Available.Should().BeFalse();
        result.Reason.Should().Contain("not enabled");
        ctx.AlertSet.Find(alert.Id)!.AiSummary.Should().BeNull();
    }

    [Fact]
    public async Task ModelReturnsNothing_ReportsUnavailable_NotException()
    {
        var (ctx, user, alert) = Seed();
        var handler = new GetAlertSummaryQueryHandler(ctx, user, Ai(true, null));

        var result = await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);

        result.Available.Should().BeFalse();
        result.Reason.Should().Contain("did not return");
    }

    [Fact]
    public async Task PromptContainsDeviceFactsAndTrend()
    {
        var (ctx, user, alert) = Seed();
        string? captured = null;
        var ai = Ai(true, "ok");
        ai.CompleteAsync(Arg.Any<string>(), Arg.Do<string>(p => captured = p), Arg.Any<CancellationToken>())
          .Returns(Task.FromResult<string?>("ok"));
        var handler = new GetAlertSummaryQueryHandler(ctx, user, ai);

        await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);

        captured.Should().Contain("Feed Water Pump (PUMP-LW-001)")
                .And.Contain("Centrifugal Pump")
                .And.Contain("bearing_temp")
                .And.Contain("92.5 C")
                .And.Contain("75 C")
                .And.Contain("80 C, 86 C, 92.5 C"); // trend from ReadingsJson
    }

    [Fact]
    public async Task OtherTenantsAlert_NotFound()
    {
        var (ctx, _, alert) = Seed();
        var stranger = TestCurrentUserService.AsCustomerAdmin(Guid.NewGuid());
        var handler = new GetAlertSummaryQueryHandler(ctx, stranger, Ai(true, "x"));

        var act = async () => await handler.Handle(new GetAlertSummaryQuery(alert.Id), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
