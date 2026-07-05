using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class UpdateMaintenanceTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (UpdateMaintenanceTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new UpdateMaintenanceTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    private static MaintenanceType SeedCustom(InMemoryApplicationDbContext ctx, Guid tenantId)
    {
        var mt = MaintenanceType.CreateCustomValue(tenantId, "Preventive", "PREV", "#3b82f6");
        ctx.Add(mt);
        ctx.SaveChanges();
        return mt;
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameDescriptionAndColor()
    {
        var (handler, ctx) = Setup();
        var mt = SeedCustom(ctx, _tenantId);

        await handler.Handle(
            new UpdateMaintenanceTypeCommand(mt.Id, "Planned", "Scheduled work", "#ef4444", 2),
            CancellationToken.None);

        var saved = ctx.MaintenanceTypeSet.Find(mt.Id)!;
        saved.Name.Should().Be("Planned");
        saved.Description.Should().Be("Scheduled work");
        saved.Color.Should().Be("#ef4444");
    }

    [Fact]
    public async Task Handle_ColorToNull_ClearsColor()
    {
        var (handler, ctx) = Setup();
        var mt = SeedCustom(ctx, _tenantId);

        await handler.Handle(
            new UpdateMaintenanceTypeCommand(mt.Id, "Preventive", null, null, 0),
            CancellationToken.None);

        ctx.MaintenanceTypeSet.Find(mt.Id)!.Color.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new UpdateMaintenanceTypeCommand(Guid.NewGuid(), "X", null, null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OtherTenantsType_ThrowsNotFoundException()
    {
        var (handler, ctx) = Setup();
        var mt = SeedCustom(ctx, Guid.NewGuid()); // belongs to a different tenant

        var act = async () => await handler.Handle(
            new UpdateMaintenanceTypeCommand(mt.Id, "X", null, null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Operator_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup(TestCurrentUserService.AsOperator(_tenantId));
        var mt = SeedCustom(ctx, _tenantId);

        var act = async () => await handler.Handle(
            new UpdateMaintenanceTypeCommand(mt.Id, "X", null, null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_Executive_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup(TestCurrentUserService.AsExecutive(_tenantId));
        var mt = SeedCustom(ctx, _tenantId);

        var act = async () => await handler.Handle(
            new UpdateMaintenanceTypeCommand(mt.Id, "X", null, null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
