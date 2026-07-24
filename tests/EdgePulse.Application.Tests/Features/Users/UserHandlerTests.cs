using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Users;
using EdgePulse.Application.Tests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace EdgePulse.Application.Tests.Features.Users;

public class UserHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static IdentityUser User(
        string id, string role, Guid? tenantId = null, string username = "u") =>
        new(id, username, $"{username}@x.io", "F", "L", true, role,
            tenantId ?? _tenantId, null, []);

    // ── GetUsers ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_CustomerAdmin_SeesOnlyOwnTenant()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        identity.GetUsersAsync(Arg.Any<CancellationToken>()).Returns(
        [
            User("1", "Operator"),
            User("2", "Operator", Guid.NewGuid(), "foreign"),
        ]);

        var handler = new GetUsersQueryHandler(
            identity, TestCurrentUserService.AsCustomerAdmin(_tenantId));
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().ContainSingle(u => u.Id == "1");
    }

    [Fact]
    public async Task GetUsers_SuperAdmin_SeesEveryone()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        identity.GetUsersAsync(Arg.Any<CancellationToken>()).Returns(
        [
            User("1", "Operator"),
            User("2", "Operator", Guid.NewGuid(), "other"),
        ]);

        var handler = new GetUsersQueryHandler(
            identity, TestCurrentUserService.AsSuperAdmin());
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsers_Operator_Forbidden()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        var handler = new GetUsersQueryHandler(
            identity, TestCurrentUserService.AsOperator(_tenantId));

        var act = async () => await handler.Handle(new GetUsersQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ── CreateUser ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_CustomerAdmin_CreatesInOwnTenant()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        identity.CreateUserAsync(Arg.Any<CreateIdentityUser>(), Arg.Any<CancellationToken>())
            .Returns("new-id");

        var handler = new CreateUserCommandHandler(
            identity, TestCurrentUserService.AsCustomerAdmin(_tenantId));
        var id = await handler.Handle(
            new CreateUserCommand("a@x.io", "A", "B", "Operator", null, [], "Passw0rd!"),
            CancellationToken.None);

        id.Should().Be("new-id");
        await identity.Received(1).CreateUserAsync(
            Arg.Is<CreateIdentityUser>(u => u.TenantId == _tenantId && u.Role == "Operator"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUser_CustomerAdmin_CannotMintSuperAdmin()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        var handler = new CreateUserCommandHandler(
            identity, TestCurrentUserService.AsCustomerAdmin(_tenantId));

        var act = async () => await handler.Handle(
            new CreateUserCommand("a@x.io", "A", "B", "SuperAdmin", null, [], "Passw0rd!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task CreateUser_UnknownRole_ThrowsValidation()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        var handler = new CreateUserCommandHandler(
            identity, TestCurrentUserService.AsCustomerAdmin(_tenantId));

        var act = async () => await handler.Handle(
            new CreateUserCommand("a@x.io", "A", "B", "Wizard", null, [], "Passw0rd!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── UpdateRole ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRole_CustomerAdmin_CannotTouchOtherTenant()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        identity.GetUserAsync("x", Arg.Any<CancellationToken>())
            .Returns(User("x", "Operator", Guid.NewGuid()));

        var handler = new UpdateUserRoleCommandHandler(
            identity, TestCurrentUserService.AsCustomerAdmin(_tenantId));

        var act = async () => await handler.Handle(
            new UpdateUserRoleCommand("x", "MillManager", Guid.NewGuid(), []),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task UpdateRole_UnknownUser_ThrowsNotFound()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        identity.GetUserAsync("nope", Arg.Any<CancellationToken>())
            .Returns((IdentityUser?)null);

        var handler = new UpdateUserRoleCommandHandler(
            identity, TestCurrentUserService.AsCustomerAdmin(_tenantId));

        var act = async () => await handler.Handle(
            new UpdateUserRoleCommand("nope", "Operator", null, []),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Enable / disable ─────────────────────────────────────────────────────

    [Fact]
    public async Task SetEnabled_CannotDisableSelf()
    {
        var actor = TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var identity = Substitute.For<IIdentityAdminService>();
        identity.GetUserAsync(actor.UserId, Arg.Any<CancellationToken>())
            .Returns(User(actor.UserId, "CustomerAdmin"));

        var handler = new SetUserEnabledCommandHandler(identity, actor);
        var act = async () => await handler.Handle(
            new SetUserEnabledCommand(actor.UserId, false), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SetEnabled_DisablesOtherUser()
    {
        var identity = Substitute.For<IIdentityAdminService>();
        identity.GetUserAsync("victim", Arg.Any<CancellationToken>())
            .Returns(User("victim", "Operator"));

        var handler = new SetUserEnabledCommandHandler(
            identity, TestCurrentUserService.AsCustomerAdmin(_tenantId));
        await handler.Handle(new SetUserEnabledCommand("victim", false), CancellationToken.None);

        await identity.Received(1).SetUserEnabledAsync(
            "victim", false, Arg.Any<CancellationToken>());
    }
}
