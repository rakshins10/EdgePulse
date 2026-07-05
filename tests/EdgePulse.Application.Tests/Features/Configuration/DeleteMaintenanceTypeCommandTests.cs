using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class DeleteMaintenanceTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (DeleteMaintenanceTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new DeleteMaintenanceTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    [Fact]
    public async Task Handle_CustomValue_DeactivatesIt()
    {
        var (handler, ctx) = Setup();
        var mt = MaintenanceType.CreateCustomValue(_tenantId, "Preventive", "PREV");
        ctx.Add(mt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        await handler.Handle(new DeleteMaintenanceTypeCommand(mt.Id), CancellationToken.None);

        ctx.MaintenanceTypeSet.Find(mt.Id)!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new DeleteMaintenanceTypeCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SystemValue_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup();
        var mt = MaintenanceType.CreateSystemValue(
            Guid.NewGuid(), Guid.NewGuid(), "Routine", "ROUTINE");
        ctx.Add(mt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteMaintenanceTypeCommand(mt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_OtherTenantsType_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup();
        var mt = MaintenanceType.CreateCustomValue(Guid.NewGuid(), "Preventive", "PREV");
        ctx.Add(mt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteMaintenanceTypeCommand(mt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_Operator_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup(TestCurrentUserService.AsOperator(_tenantId));
        var mt = MaintenanceType.CreateCustomValue(_tenantId, "Preventive", "PREV");
        ctx.Add(mt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteMaintenanceTypeCommand(mt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
