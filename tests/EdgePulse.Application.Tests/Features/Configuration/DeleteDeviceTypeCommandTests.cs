using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class DeleteDeviceTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (DeleteDeviceTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new DeleteDeviceTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    [Fact]
    public async Task Handle_UnusedCustomValue_DeactivatesIt()
    {
        var (handler, ctx) = Setup();
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "PUMP");
        ctx.Add(dt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        await handler.Handle(new DeleteDeviceTypeCommand(dt.Id), CancellationToken.None);

        ctx.DeviceTypeSet.Find(dt.Id)!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TypeInUseByDevice_ThrowsConflictException()
    {
        var (handler, ctx) = Setup();
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "PUMP");
        ctx.Add(dt);
        ctx.Add(Device.Create(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(),
            typeId: dt.Id, statusId: Guid.NewGuid(), name: "Feed Pump", code: "FP1"));
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteDeviceTypeCommand(dt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_SystemValue_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup();
        var dt = DeviceType.CreateSystemValue(Guid.NewGuid(), Guid.NewGuid(), "Fan", "FAN");
        ctx.Add(dt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteDeviceTypeCommand(dt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new DeleteDeviceTypeCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Operator_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup(TestCurrentUserService.AsOperator(_tenantId));
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "PUMP");
        ctx.Add(dt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteDeviceTypeCommand(dt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
