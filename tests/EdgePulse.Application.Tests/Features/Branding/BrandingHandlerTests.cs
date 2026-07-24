using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Branding;
using EdgePulse.Application.Tests.Helpers;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Branding;

public class BrandingHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Get_NoRow_ReturnsDefaults()
    {
        var ctx = TestDbContextFactory.Create();
        var handler = new GetBrandingQueryHandler(
            ctx, TestCurrentUserService.AsOperator(_tenantId));
        var result = await handler.Handle(new GetBrandingQuery(), CancellationToken.None);

        result.ProductName.Should().Be("EdgePulse");
        result.AccentColor.Should().BeNull();
    }

    [Fact]
    public async Task Update_CreatesThenUpdates_SingleRow()
    {
        var ctx = TestDbContextFactory.Create();
        var admin = TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var update = new UpdateBrandingCommandHandler(ctx, admin);

        await update.Handle(
            new UpdateBrandingCommand("NordPulp Monitor", null, "#0ea5e9"),
            CancellationToken.None);
        await update.Handle(
            new UpdateBrandingCommand("NordPulp Ops", "https://x/logo.png", "#22c55e"),
            CancellationToken.None);

        ctx.BrandingSet.Should().HaveCount(1);

        var get = new GetBrandingQueryHandler(ctx, admin);
        var result = await get.Handle(new GetBrandingQuery(), CancellationToken.None);
        result.ProductName.Should().Be("NordPulp Ops");
        result.AccentColor.Should().Be("#22c55e");
        result.LogoUrl.Should().Be("https://x/logo.png");
    }

    [Fact]
    public async Task Update_Operator_Forbidden()
    {
        var ctx = TestDbContextFactory.Create();
        var handler = new UpdateBrandingCommandHandler(
            ctx, TestCurrentUserService.AsOperator(_tenantId));

        var act = async () => await handler.Handle(
            new UpdateBrandingCommand("X", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
