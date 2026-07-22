using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class DeleteMetricTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (DeleteMetricTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new DeleteMetricTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    [Fact]
    public async Task Handle_CustomValue_DeactivatesIt()
    {
        var (handler, ctx) = Setup();
        var mt = MetricType.CreateCustomValue(_tenantId, "Temperature", "TEMP", "C");
        ctx.Add(mt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        await handler.Handle(new DeleteMetricTypeCommand(mt.Id), CancellationToken.None);

        ctx.MetricTypeSet.Find(mt.Id)!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SystemValue_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup();
        var mt = MetricType.CreateSystemValue(
            Guid.NewGuid(), Guid.NewGuid(), "Pressure", "PRESSURE", "bar");
        ctx.Add(mt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteMetricTypeCommand(mt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_OtherTenantsType_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup();
        var mt = MetricType.CreateCustomValue(Guid.NewGuid(), "Temperature", "TEMP", "C");
        ctx.Add(mt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteMetricTypeCommand(mt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new DeleteMetricTypeCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
