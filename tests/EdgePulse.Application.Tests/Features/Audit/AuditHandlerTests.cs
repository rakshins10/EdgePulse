using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Features.Audit;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Audit;

public class AuditHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static InMemoryApplicationDbContext Seed()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Add(AuditLog.Create(_tenantId, "alice", "CREATED", "Device", Guid.NewGuid(), "Pump", null));
        ctx.Add(AuditLog.Create(_tenantId, "bob", "UPDATED", "Mill", Guid.NewGuid(), "Lakewood",
            "{\"Name\":{\"old\":\"A\",\"new\":\"B\"}}"));
        ctx.Add(AuditLog.Create(Guid.NewGuid(), "eve", "DELETED", "Device", Guid.NewGuid(), "Foreign", null));
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task GetAuditLogs_TenantScoped_NewestFirst()
    {
        var ctx = Seed();
        var handler = new GetAuditLogsQueryHandler(
            ctx, TestCurrentUserService.AsCustomerAdmin(_tenantId));

        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(r => r.UserName).Should().NotContain("eve");
    }

    [Fact]
    public async Task GetAuditLogs_FilterByEntityTypeAndAction()
    {
        var ctx = Seed();
        var handler = new GetAuditLogsQueryHandler(
            ctx, TestCurrentUserService.AsCustomerAdmin(_tenantId));

        var result = await handler.Handle(
            new GetAuditLogsQuery(EntityType: "Mill", Action: "UPDATED"),
            CancellationToken.None);

        result.Should().ContainSingle(r => r.UserName == "bob");
    }

    [Fact]
    public async Task GetAuditLogs_NonAdmin_Forbidden()
    {
        var ctx = Seed();
        var handler = new GetAuditLogsQueryHandler(
            ctx, TestCurrentUserService.AsOperator(_tenantId));

        var act = async () => await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
