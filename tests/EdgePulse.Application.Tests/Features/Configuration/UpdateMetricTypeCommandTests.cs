using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class UpdateMetricTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (UpdateMetricTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new UpdateMetricTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    private static MetricType SeedCustom(InMemoryApplicationDbContext ctx, Guid tenantId)
    {
        var mt = MetricType.CreateCustomValue(tenantId, "Temperature", "TEMP", "C", "Old desc");
        ctx.Add(mt);
        ctx.SaveChanges();
        return mt;
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameDescriptionAndUnit()
    {
        var (handler, ctx) = Setup();
        var mt = SeedCustom(ctx, _tenantId);

        await handler.Handle(
            new UpdateMetricTypeCommand(mt.Id, "Temp (surface)", "F", "Fahrenheit", 2),
            CancellationToken.None);

        var saved = ctx.MetricTypeSet.Find(mt.Id)!;
        saved.Name.Should().Be("Temp (surface)");
        saved.Description.Should().Be("Fahrenheit");
        saved.DefaultUnit.Should().Be("F");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new UpdateMetricTypeCommand(Guid.NewGuid(), "X", "C", null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Operator_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup(TestCurrentUserService.AsOperator(_tenantId));
        var mt = SeedCustom(ctx, _tenantId);

        var act = async () => await handler.Handle(
            new UpdateMetricTypeCommand(mt.Id, "X", "C", null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
