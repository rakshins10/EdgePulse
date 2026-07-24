using EdgePulse.Application.Common;
using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Webhooks;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace EdgePulse.Application.Tests.Features.Webhooks;

public class WebhookHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (InMemoryApplicationDbContext ctx, TestCurrentUserService user) Setup()
        => (TestDbContextFactory.Create(), TestCurrentUserService.AsCustomerAdmin(_tenantId));

    [Fact]
    public void Signer_ProducesStableHexHmac()
    {
        var sig = WebhookSigner.Sign("secret123", "{\"a\":1}");
        sig.Should().MatchRegex("^[0-9a-f]{64}$");
        WebhookSigner.Sign("secret123", "{\"a\":1}").Should().Be(sig);   // deterministic
        WebhookSigner.Sign("other", "{\"a\":1}").Should().NotBe(sig);    // keyed
    }

    [Fact]
    public void SubscribesTo_MatchesCaseInsensitive()
    {
        var wh = WebhookSubscription.Create(
            _tenantId, "n", "https://x", "secret12", ["alert.created"]);
        wh.SubscribesTo("ALERT.CREATED").Should().BeTrue();
        wh.SubscribesTo("workorder.created").Should().BeFalse();
    }

    [Fact]
    public async Task Create_List_RoundTrip()
    {
        var (ctx, user) = Setup();
        var create = new CreateWebhookCommandHandler(ctx, user);
        var id = await create.Handle(
            new CreateWebhookCommand("Ops Slack", "https://hooks.example/x",
                "secret123", ["alert.created"], "slack"),
            CancellationToken.None);

        var list = new GetWebhooksQueryHandler(ctx, user);
        var result = await list.Handle(new GetWebhooksQuery(), CancellationToken.None);

        result.Should().ContainSingle(w =>
            w.Id == id && w.Format == "slack" && w.Events.Contains("alert.created"));
    }

    [Fact]
    public async Task List_Operator_Forbidden()
    {
        var (ctx, _) = Setup();
        var handler = new GetWebhooksQueryHandler(
            ctx, TestCurrentUserService.AsOperator(_tenantId));

        var act = async () => await handler.Handle(new GetWebhooksQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Update_EmptySecret_KeepsExisting()
    {
        var (ctx, user) = Setup();
        var create = new CreateWebhookCommandHandler(ctx, user);
        var id = await create.Handle(
            new CreateWebhookCommand("A", "https://x", "originalsecret", ["alert.created"]),
            CancellationToken.None);

        var update = new UpdateWebhookCommandHandler(ctx, user);
        await update.Handle(
            new UpdateWebhookCommand(id, "A2", "https://y", null,
                ["workorder.created"], "json", false),
            CancellationToken.None);

        var saved = ctx.WebhookSet.Find(id)!;
        saved.Secret.Should().Be("originalsecret");
        saved.Url.Should().Be("https://y");
        saved.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Test_FiresSenderAndRecordsStatus()
    {
        var (ctx, user) = Setup();
        var create = new CreateWebhookCommandHandler(ctx, user);
        var id = await create.Handle(
            new CreateWebhookCommand("T", "https://x", "secret123", ["alert.created"]),
            CancellationToken.None);

        var sender = Substitute.For<IWebhookSender>();
        sender.SendAsync(
                Arg.Any<WebhookSubscription>(), "test", Arg.Any<object>(),
                Arg.Any<CancellationToken>())
            .Returns("200");

        var handler = new TestWebhookCommandHandler(ctx, user, sender);
        var status = await handler.Handle(new TestWebhookCommand(id), CancellationToken.None);

        status.Should().Be("200");
        ctx.WebhookSet.Find(id)!.LastStatus.Should().Be("200");
    }

    [Fact]
    public async Task Delete_OtherTenant_NotFound()
    {
        var (ctx, user) = Setup();
        var foreign = WebhookSubscription.Create(
            Guid.NewGuid(), "F", "https://x", "secret12", ["alert.created"]);
        ctx.Add(foreign);
        ctx.SaveChanges();

        var handler = new DeleteWebhookCommandHandler(ctx, user);
        var act = async () => await handler.Handle(
            new DeleteWebhookCommand(foreign.Id), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
