using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class DeleteLocationTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (DeleteLocationTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new DeleteLocationTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    [Fact]
    public async Task Handle_UnusedCustomValue_DeactivatesIt()
    {
        var (handler, ctx) = Setup();
        var lt = LocationType.CreateCustomValue(_tenantId, "Floor", "FLOOR");
        ctx.Add(lt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        await handler.Handle(new DeleteLocationTypeCommand(lt.Id), CancellationToken.None);

        ctx.LocationTypeSet.Find(lt.Id)!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_InUseByArea_ThrowsConflictException()
    {
        var (handler, ctx) = Setup();
        var lt = LocationType.CreateCustomValue(_tenantId, "Floor", "FLOOR");
        ctx.Add(lt);
        ctx.Add(Area.Create(_tenantId, Guid.NewGuid(), "Area 1", "A1", locationTypeId: lt.Id));
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteLocationTypeCommand(lt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_SystemValue_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup();
        var lt = LocationType.CreateSystemValue(Guid.NewGuid(), Guid.NewGuid(), "Building", "BUILDING");
        ctx.Add(lt);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var act = async () => await handler.Handle(
            new DeleteLocationTypeCommand(lt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new DeleteLocationTypeCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
