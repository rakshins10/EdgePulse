using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Configuration;

public class CreateMaintenanceTypeCommandTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (CreateMaintenanceTypeCommandHandler handler,
                    InMemoryApplicationDbContext context)
        Setup(TestCurrentUserService? user = null)
    {
        var ctx     = TestDbContextFactory.Create();
        var svc     = user ?? TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var handler = new CreateMaintenanceTypeCommandHandler(ctx, svc);
        return (handler, ctx);
    }

    // ── Success paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_CreatesMaintenanceType_AndReturnsId()
    {
        var (handler, ctx) = Setup();
        var command = new CreateMaintenanceTypeCommand("Preventive", "PREV", null, "#3b82f6", 1);

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        var saved = ctx.MaintenanceTypeSet.Find(id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Preventive");
        saved.Code.Should().Be("PREV");
        saved.Color.Should().Be("#3b82f6");
        saved.SortOrder.Should().Be(1);
        saved.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidCommand_CodeIsUppercased()
    {
        var (handler, ctx) = Setup();
        var command = new CreateMaintenanceTypeCommand("Corrective", "corr", null, null);

        var id = await handler.Handle(command, CancellationToken.None);

        var saved = ctx.MaintenanceTypeSet.Find(id);
        saved!.Code.Should().Be("CORR");
    }

    // ── Role-based access ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Operator_ThrowsForbiddenAccessException()
    {
        var (handler, _) = Setup(TestCurrentUserService.AsOperator(_tenantId));
        var command = new CreateMaintenanceTypeCommand("Emergency", "EMER", null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_Executive_ThrowsForbiddenAccessException()
    {
        var (handler, _) = Setup(TestCurrentUserService.AsExecutive(_tenantId));
        var command = new CreateMaintenanceTypeCommand("Emergency", "EMER", null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_CustomerAdmin_Succeeds()
    {
        var (handler, _) = Setup(TestCurrentUserService.AsCustomerAdmin(_tenantId));
        var command = new CreateMaintenanceTypeCommand("Planned", "PLAN", null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ── Duplicate code conflict ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateCode_SameTenant_ThrowsConflictException()
    {
        var (handler, ctx) = Setup();

        // Pre-seed an existing maintenance type
        ctx.Add(MaintenanceType.CreateCustomValue(_tenantId, "Preventive", "PREV"));
        await ctx.SaveChangesAsync(CancellationToken.None);

        var command = new CreateMaintenanceTypeCommand("Preventive 2", "PREV", null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_DuplicateCode_DifferentTenant_Succeeds()
    {
        var otherTenantId = Guid.NewGuid();
        var (handler, ctx) = Setup();

        // Pre-seed a type belonging to a different tenant
        ctx.Add(MaintenanceType.CreateCustomValue(otherTenantId, "Preventive", "PREV"));
        await ctx.SaveChangesAsync(CancellationToken.None);

        // Same code but our tenant — should not conflict
        var command = new CreateMaintenanceTypeCommand("Preventive", "PREV", null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
