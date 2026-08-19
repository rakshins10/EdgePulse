using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Ai;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace EdgePulse.Application.Tests.Features.Ai;

/// <summary>
/// "Ask EdgePulse" is tested with a fake IAiAssistant. What we verify here is
/// the part that matters and that we control: WHICH DATA the model is given
/// (grounding), role scoping, device matching, validation and the graceful
/// unavailable paths. The model's wording is not under test.
/// </summary>
public class AskQuestionTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private record Seeded(
        InMemoryApplicationDbContext Ctx, TestCurrentUserService User,
        Device Pump, Device Motor, Guid AreaA, Guid AreaB, Guid MillId);

    private static Seeded Seed()
    {
        var ctx = TestDbContextFactory.Create();
        var user = TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var millId = Guid.NewGuid();
        var areaA = Guid.NewGuid();
        var areaB = Guid.NewGuid();

        var pumpType = DeviceType.CreateCustomValue(_tenantId, "Centrifugal Pump", "PUMP");
        var motorType = DeviceType.CreateCustomValue(_tenantId, "Electric Motor", "MOTOR");
        ctx.Add(pumpType); ctx.Add(motorType);

        var pump = Device.Create(_tenantId, millId, areaA, pumpType.Id, Guid.NewGuid(), "Feed Water Pump", "PUMP-LW-001");
        var motor = Device.Create(_tenantId, millId, areaB, motorType.Id, Guid.NewGuid(), "Refiner Motor", "MOT-RF-002");
        ctx.Add(pump); ctx.Add(motor);

        // pump: 2 alerts (one open HIGH, one resolved LOW); motor: 1 open CRITICAL
        ctx.Add(Alert.Create(_tenantId, pump.Id, millId, areaA, Guid.NewGuid(), "vibration", 11.4, 8, "HIGH", "mm/s"));
        var resolved = Alert.Create(_tenantId, pump.Id, millId, areaA, Guid.NewGuid(), "bearing_temp", 76, 75, "LOW", "C");
        resolved.Resolve("tech@plant");
        ctx.Add(resolved);
        ctx.Add(Alert.Create(_tenantId, motor.Id, millId, areaB, Guid.NewGuid(), "current", 420, 380, "CRITICAL", "A"));

        ctx.Add(WorkOrder.Create(_tenantId, pump.Id, millId, "Inspect pump bearings", "admin@plant", "HIGH"));

        ctx.SaveChanges();
        return new Seeded(ctx, user, pump, motor, areaA, areaB, millId);
    }

    private static (IAiAssistant ai, List<string> prompts) Ai(bool enabled, string? reply)
    {
        var prompts = new List<string>();
        var ai = Substitute.For<IAiAssistant>();
        ai.IsEnabled.Returns(enabled);
        ai.Description.Returns(enabled ? "ollama/llama3.2" : "disabled");
        ai.CompleteAsync(Arg.Any<string>(), Arg.Do<string>(prompts.Add), Arg.Any<CancellationToken>())
          .Returns(Task.FromResult(reply));
        return (ai, prompts);
    }

    [Fact]
    public async Task DeviceId_GroundsOnThatDevice_AlertsAndWorkOrders()
    {
        var s = Seed();
        var (ai, prompts) = Ai(true, "The pump has one open HIGH vibration alert.");
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        var r = await handler.Handle(new AskQuestionQuery("What is wrong with this pump?", s.Pump.Id), CancellationToken.None);

        r.Available.Should().BeTrue();
        r.Answer.Should().Contain("vibration");
        r.Grounding.Scope.Should().Be("device");
        r.Grounding.Devices.Should().ContainSingle().Which.Should().Be("Feed Water Pump (PUMP-LW-001)");
        r.Grounding.Alerts.Should().Be(2);
        r.Grounding.WorkOrders.Should().Be(1);

        var prompt = prompts.Single();
        prompt.Should().Contain("DEVICE: Feed Water Pump (PUMP-LW-001)");
        prompt.Should().Contain("type: Centrifugal Pump");
        prompt.Should().Contain("alerts (last 30 days + any still open): 2 total, 1 open (1 HIGH, 1 LOW)");
        prompt.Should().Contain("vibration 11.4mm/s (threshold 8mm/s)");
        prompt.Should().Contain("Inspect pump bearings");
        prompt.Should().Contain("QUESTION:\nWhat is wrong with this pump?".Replace("\n", Environment.NewLine));
        prompt.Should().NotContain("MOT-RF-002", "other devices must not leak into a device-scoped question");
    }

    [Fact]
    public async Task DeviceMentionedByCode_InQuestion_IsDetected()
    {
        var s = Seed();
        var (ai, prompts) = Ai(true, "ok");
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        var r = await handler.Handle(new AskQuestionQuery("Any open work on mot-rf-002?"), CancellationToken.None);

        r.Grounding.Scope.Should().Be("mentioned-devices");
        r.Grounding.Devices.Should().Equal("Refiner Motor (MOT-RF-002)");
        prompts.Single().Should().Contain("DEVICE: Refiner Motor (MOT-RF-002)").And.NotContain("PUMP-LW-001");
    }

    [Fact]
    public async Task DeviceMentionedByName_InQuestion_IsDetected()
    {
        var s = Seed();
        var (ai, prompts) = Ai(true, "ok");
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        await handler.Handle(new AskQuestionQuery("Has the feed water pump alerted recently?"), CancellationToken.None);

        prompts.Single().Should().Contain("DEVICE: Feed Water Pump (PUMP-LW-001)");
    }

    [Fact]
    public async Task NoDevice_GivesTenantSnapshot_WithOpenAlertsAndWorkOrders()
    {
        var s = Seed();
        var (ai, prompts) = Ai(true, "There are 2 open alerts.");
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        var r = await handler.Handle(new AskQuestionQuery("How is the plant doing today?"), CancellationToken.None);

        r.Grounding.Scope.Should().Be("tenant");
        r.Grounding.Devices.Should().BeEmpty();
        r.Grounding.Alerts.Should().Be(2, "only OPEN alerts are in the snapshot");
        var prompt = prompts.Single();
        prompt.Should().Contain("PLANT SNAPSHOT (devices visible to you: 2)");
        prompt.Should().Contain("Open alerts: 2 (1 CRITICAL, 1 HIGH)");
        prompt.Should().Contain("Most alerts in the last 7 days: Feed Water Pump (PUMP-LW-001) 2");
        prompt.Should().Contain("Open work orders: 1");
    }

    [Fact]
    public async Task Operator_OnlySeesTheirAreas()
    {
        var s = Seed();
        var op = TestCurrentUserService.AsOperator(_tenantId);
        op.AreaIds = new[] { s.AreaB };   // motor only
        var (ai, prompts) = Ai(true, "ok");
        var handler = new AskQuestionQueryHandler(s.Ctx, op, ai);

        var r = await handler.Handle(new AskQuestionQuery("Status of PUMP-LW-001?"), CancellationToken.None);

        // the pump is outside the operator's areas → not matched → tenant snapshot of 1 device
        r.Grounding.Scope.Should().Be("tenant");
        prompts.Single().Should().Contain("devices visible to you: 1").And.Contain("Refiner Motor (MOT-RF-002)").And.NotContain("Feed Water Pump");
    }

    [Fact]
    public async Task DeviceId_OutsideTenant_IsNotFound()
    {
        var s = Seed();
        var other = TestCurrentUserService.AsCustomerAdmin(Guid.NewGuid());
        var (ai, _) = Ai(true, "ok");
        var handler = new AskQuestionQueryHandler(s.Ctx, other, ai);

        var act = () => handler.Handle(new AskQuestionQuery("?", s.Pump.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyQuestion_IsRejected(string q)
    {
        var s = Seed();
        var (ai, _) = Ai(true, "ok");
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        var act = () => handler.Handle(new AskQuestionQuery(q), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task TooLongQuestion_IsRejected()
    {
        var s = Seed();
        var (ai, _) = Ai(true, "ok");
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        var act = () => handler.Handle(new AskQuestionQuery(new string('x', AskQuestionQueryHandler.MaxQuestionLength + 1)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AiDisabled_ReturnsUnavailable_WithoutCallingModel()
    {
        var s = Seed();
        var (ai, _) = Ai(false, null);
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        var r = await handler.Handle(new AskQuestionQuery("anything"), CancellationToken.None);

        r.Available.Should().BeFalse();
        r.Reason.Should().Contain("not enabled");
        r.Grounding.Should().NotBeNull();
        await ai.DidNotReceive().CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ModelReturnsNull_ReturnsUnavailable_WithReason()
    {
        var s = Seed();
        var (ai, _) = Ai(true, null);
        var handler = new AskQuestionQueryHandler(s.Ctx, s.User, ai);

        var r = await handler.Handle(new AskQuestionQuery("anything"), CancellationToken.None);

        r.Available.Should().BeFalse();
        r.Answer.Should().BeNull();
        r.Reason.Should().Contain("did not return");
    }

    [Fact]
    public void MatchDevices_PrefersCodes_CapsAtThree_AndIgnoresShortNames()
    {
        var rows = new List<AskQuestionQueryHandler.DeviceRow>();
        for (int i = 1; i <= 5; i++)
            rows.Add(new(Guid.NewGuid(), $"Pump {i}", $"P-00{i}", Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, null, null));
        rows.Add(new(Guid.NewGuid(), "Fan", "FAN-1", Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, null, null));

        var hits = AskQuestionQueryHandler.MatchDevicesInQuestion("compare p-001, P-002, p-003 and p-004 and the fan", rows);

        hits.Should().HaveCount(3);
        hits.Select(h => h.Code).Should().BeEquivalentTo(new[] { "P-001", "P-002", "P-003" });
    }
}
