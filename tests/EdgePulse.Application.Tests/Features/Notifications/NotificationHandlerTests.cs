using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Notifications.Commands;
using EdgePulse.Application.Features.Notifications.Queries;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Notifications;

public class NotificationHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (InMemoryApplicationDbContext ctx, TestCurrentUserService user) Setup()
    {
        var ctx  = TestDbContextFactory.Create();
        var user = TestCurrentUserService.AsCustomerAdmin(_tenantId);
        return (ctx, user);
    }

    private static Notification Seed(
        InMemoryApplicationDbContext ctx, Guid tenantId, string title = "T",
        bool read = false)
    {
        var n = Notification.Create(
            tenantId, "ALERT", title, "message", "HIGH", "Alert", Guid.NewGuid());
        if (read) n.MarkRead();
        ctx.Add(n);
        ctx.SaveChanges();
        return n;
    }

    // ── GetNotificationsQuery ─────────────────────────────────────────────────

    [Fact]
    public async Task GetNotifications_ReturnsOnlyOwnTenant_NewestFirst()
    {
        var (ctx, user) = Setup();
        Seed(ctx, _tenantId, "mine-1");
        Seed(ctx, _tenantId, "mine-2");
        Seed(ctx, Guid.NewGuid(), "other-tenant");

        var handler = new GetNotificationsQueryHandler(ctx, user);
        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(n => n.Title).Should().NotContain("other-tenant");
    }

    [Fact]
    public async Task GetNotifications_UnreadOnly_ExcludesRead()
    {
        var (ctx, user) = Setup();
        Seed(ctx, _tenantId, "unread");
        Seed(ctx, _tenantId, "read", read: true);

        var handler = new GetNotificationsQueryHandler(ctx, user);
        var result = await handler.Handle(
            new GetNotificationsQuery(UnreadOnly: true), CancellationToken.None);

        result.Should().ContainSingle(n => n.Title == "unread");
    }

    // ── GetUnreadNotificationCountQuery ──────────────────────────────────────

    [Fact]
    public async Task UnreadCount_CountsOnlyOwnUnread()
    {
        var (ctx, user) = Setup();
        Seed(ctx, _tenantId);
        Seed(ctx, _tenantId);
        Seed(ctx, _tenantId, read: true);
        Seed(ctx, Guid.NewGuid());

        var handler = new GetUnreadNotificationCountQueryHandler(ctx, user);
        var count = await handler.Handle(
            new GetUnreadNotificationCountQuery(), CancellationToken.None);

        count.Should().Be(2);
    }

    // ── MarkNotificationReadCommand ──────────────────────────────────────────

    [Fact]
    public async Task MarkRead_SetsIsReadAndReadAt()
    {
        var (ctx, user) = Setup();
        var n = Seed(ctx, _tenantId);

        var handler = new MarkNotificationReadCommandHandler(ctx, user);
        await handler.Handle(new MarkNotificationReadCommand(n.Id), CancellationToken.None);

        var saved = ctx.NotificationSet.Find(n.Id)!;
        saved.IsRead.Should().BeTrue();
        saved.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkRead_OtherTenants_ThrowsNotFound()
    {
        var (ctx, user) = Setup();
        var n = Seed(ctx, Guid.NewGuid());

        var handler = new MarkNotificationReadCommandHandler(ctx, user);
        var act = async () => await handler.Handle(
            new MarkNotificationReadCommand(n.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── MarkAllNotificationsReadCommand ──────────────────────────────────────

    [Fact]
    public async Task MarkAllRead_MarksOnlyOwnTenant_ReturnsCount()
    {
        var (ctx, user) = Setup();
        Seed(ctx, _tenantId);
        Seed(ctx, _tenantId);
        var foreign = Seed(ctx, Guid.NewGuid());

        var handler = new MarkAllNotificationsReadCommandHandler(ctx, user);
        var count = await handler.Handle(
            new MarkAllNotificationsReadCommand(), CancellationToken.None);

        count.Should().Be(2);
        ctx.NotificationSet.Find(foreign.Id)!.IsRead.Should().BeFalse();
    }
}
