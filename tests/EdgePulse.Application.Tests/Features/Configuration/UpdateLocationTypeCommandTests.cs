using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class UpdateLocationTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (UpdateLocationTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new UpdateLocationTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    private static LocationType SeedCustom(InMemoryApplicationDbContext ctx, Guid tenantId)
    {
        var lt = LocationType.CreateCustomValue(tenantId, "Floor", "FLOOR", "Old desc");
        ctx.Add(lt);
        ctx.SaveChanges();
        return lt;
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameAndDescription()
    {
        var (handler, ctx) = Setup();
        var lt = SeedCustom(ctx, _tenantId);

        await handler.Handle(
            new UpdateLocationTypeCommand(lt.Id, "Production Line", "Main line", 3),
            CancellationToken.None);

        var saved = ctx.LocationTypeSet.Find(lt.Id)!;
        saved.Name.Should().Be("Production Line");
        saved.Description.Should().Be("Main line");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        var (handler, _) = Setup();

        var act = async () => await handler.Handle(
            new UpdateLocationTypeCommand(Guid.NewGuid(), "X", null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Operator_ThrowsForbiddenAccessException()
    {
        var (handler, ctx) = Setup(TestCurrentUserService.AsOperator(_tenantId));
        var lt = SeedCustom(ctx, _tenantId);

        var act = async () => await handler.Handle(
            new UpdateLocationTypeCommand(lt.Id, "X", null, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
