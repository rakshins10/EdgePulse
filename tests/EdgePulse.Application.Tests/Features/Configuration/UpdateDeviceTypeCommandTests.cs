using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class UpdateDeviceTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (UpdateDeviceTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new UpdateDeviceTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    private static DeviceType SeedCustom(InMemoryApplicationDbContext ctx, Guid tenantId)
    {
        var dt = DeviceType.CreateCustomValue(tenantId, "Pump", "PUMP", "Old desc");
        ctx.Add(dt);
        ctx.SaveChanges();
        return dt;
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameAndDescription()
    {
        var (handler, ctx) = Setup();
        var dt = SeedCustom(ctx, _tenantId);

        await handler.Handle(
            new UpdateDeviceTypeCommand(dt.Id, "Motor", "New desc", null, 3),
            CancellationToken.None);

        var saved = ctx.DeviceTypeSet.Find(dt.Id)!;
        saved.Name.Should().Be("Motor");
        saved.Description.Should().Be("New desc");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new UpdateDeviceTypeCommand(Guid.NewGuid(), "X", null, null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Operator_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup(TestCurrentUserService.AsOperator(_tenantId));
        var dt = SeedCustom(ctx, _tenantId);

        var act = async () => await handler.Handle(
            new UpdateDeviceTypeCommand(dt.Id, "X", null, null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
