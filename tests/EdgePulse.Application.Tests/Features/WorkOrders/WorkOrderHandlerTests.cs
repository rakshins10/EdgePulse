using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.WorkOrders;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.WorkOrders;

public class WorkOrderHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (InMemoryApplicationDbContext ctx, TestCurrentUserService user) Setup()
        => (TestDbContextFactory.Create(), TestCurrentUserService.AsCustomerAdmin(_tenantId));

    private static Device SeedDevice(InMemoryApplicationDbContext ctx, Guid? millId = null)
    {
        var device = Device.Create(
            _tenantId, millId ?? Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "Pump", $"P{Guid.NewGuid().ToString()[..4]}");
        ctx.Add(device);
        ctx.SaveChanges();
        return device;
    }

    [Fact]
    public async Task Create_PersistsOpenWorkOrder()
    {
        var (ctx, user) = Setup();
        var device = SeedDevice(ctx);

        var handler = new CreateWorkOrderCommandHandler(ctx, user);
        var id = await handler.Handle(
            new CreateWorkOrderCommand(device.Id, "Grease bearings", null, "LOW"),
            CancellationToken.None);

        var saved = ctx.WorkOrderSet.Find(id)!;
        saved.Status.Should().Be(WorkOrder.StatusOpen);
        saved.MillId.Should().Be(device.MillId);
        saved.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task Create_Executive_Forbidden()
    {
        var (ctx, _) = Setup();
        var device = SeedDevice(ctx);
        var handler = new CreateWorkOrderCommandHandler(
            ctx, TestCurrentUserService.AsExecutive(_tenantId));

        var act = async () => await handler.Handle(
            new CreateWorkOrderCommand(device.Id, "X", null), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Create_OtherTenantsDevice_NotFound()
    {
        var (ctx, user) = Setup();
        var foreign = Device.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "F", "F1");
        ctx.Add(foreign);
        ctx.SaveChanges();

        var handler = new CreateWorkOrderCommandHandler(ctx, user);
        var act = async () => await handler.Handle(
            new CreateWorkOrderCommand(foreign.Id, "X", null), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Transition_StartThenComplete_Works()
    {
        var (ctx, user) = Setup();
        var device = SeedDevice(ctx);
        var create = new CreateWorkOrderCommandHandler(ctx, user);
        var id = await create.Handle(
            new CreateWorkOrderCommand(device.Id, "Fix", null), CancellationToken.None);

        var handler = new TransitionWorkOrderCommandHandler(ctx, user);
        await handler.Handle(new TransitionWorkOrderCommand(id, "start"), CancellationToken.None);
        await handler.Handle(
            new TransitionWorkOrderCommand(id, "complete", "done", "belt x1"),
            CancellationToken.None);

        var saved = ctx.WorkOrderSet.Find(id)!;
        saved.Status.Should().Be(WorkOrder.StatusCompleted);
        saved.CompletionNotes.Should().Be("done");
        saved.PartsUsed.Should().Be("belt x1");
    }

    [Fact]
    public async Task Transition_IllegalMove_Throws409Conflict()
    {
        var (ctx, user) = Setup();
        var device = SeedDevice(ctx);
        var create = new CreateWorkOrderCommandHandler(ctx, user);
        var id = await create.Handle(
            new CreateWorkOrderCommand(device.Id, "Fix", null), CancellationToken.None);

        var handler = new TransitionWorkOrderCommandHandler(ctx, user);
        var act = async () => await handler.Handle(
            new TransitionWorkOrderCommand(id, "complete"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetWorkOrders_MillManager_SeesOnlyTheirMill()
    {
        var (ctx, user) = Setup();
        var millA = Guid.NewGuid();
        var deviceA = SeedDevice(ctx, millA);
        var deviceB = SeedDevice(ctx, Guid.NewGuid());

        var create = new CreateWorkOrderCommandHandler(ctx, user);
        await create.Handle(new CreateWorkOrderCommand(deviceA.Id, "A", null), CancellationToken.None);
        await create.Handle(new CreateWorkOrderCommand(deviceB.Id, "B", null), CancellationToken.None);

        var manager = new TestCurrentUserService
        {
            Role = EdgePulse.Domain.Enums.UserRole.MillManager,
            TenantId = _tenantId,
            MillId = millA,
        };

        var handler = new GetWorkOrdersQueryHandler(ctx, manager);
        var result = await handler.Handle(new GetWorkOrdersQuery(), CancellationToken.None);

        result.Should().ContainSingle(w => w.Title == "A");
    }

    [Fact]
    public async Task Assign_SetsAssignee()
    {
        var (ctx, user) = Setup();
        var device = SeedDevice(ctx);
        var create = new CreateWorkOrderCommandHandler(ctx, user);
        var id = await create.Handle(
            new CreateWorkOrderCommand(device.Id, "Fix", null), CancellationToken.None);

        var handler = new AssignWorkOrderCommandHandler(ctx, user);
        await handler.Handle(
            new AssignWorkOrderCommand(id, "tech@nordpulp.example"), CancellationToken.None);

        ctx.WorkOrderSet.Find(id)!.AssignedTo.Should().Be("tech@nordpulp.example");
    }
}
