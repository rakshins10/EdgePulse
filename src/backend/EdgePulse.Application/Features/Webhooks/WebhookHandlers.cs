using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Webhooks;

// Webhook administration is SuperAdmin/CustomerAdmin only.

public static class WebhookEvents
{
    public static readonly string[] All = ["alert.created", "workorder.created"];
}

/// <summary>Sends a signed webhook request. Implemented in Infrastructure.</summary>
public interface IWebhookSender
{
    /// <returns>Delivery status, e.g. "200" or "error: timeout".</returns>
    Task<string> SendAsync(
        WebhookSubscription subscription,
        string eventKey,
        object data,
        CancellationToken cancellationToken);
}

public record WebhookDto(
    Guid Id,
    string Name,
    string Url,
    List<string> Events,
    string Format,
    bool IsActive,
    string? LastStatus,
    DateTime? LastTriggeredAt
);

internal static class WebhookGuard
{
    public static void RequireAdmin(ICurrentUserService user)
    {
        if (!user.IsSuperAdmin && !user.IsCustomerAdmin)
            throw new ForbiddenAccessException();
    }
}

// ── List ─────────────────────────────────────────────────────────────────────

public record GetWebhooksQuery : IRequest<List<WebhookDto>>;

public class GetWebhooksQueryHandler : IRequestHandler<GetWebhooksQuery, List<WebhookDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetWebhooksQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<WebhookDto>> Handle(
        GetWebhooksQuery request, CancellationToken cancellationToken)
    {
        WebhookGuard.RequireAdmin(_currentUser);
        return await _context.WebhookSubscriptions
            .Where(w => w.TenantId == _currentUser.TenantId && !w.IsDeleted)
            .OrderBy(w => w.Name)
            .Select(w => new WebhookDto(
                w.Id, w.Name, w.Url,
                w.Events.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                w.Format, w.IsActive, w.LastStatus, w.LastTriggeredAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Create ───────────────────────────────────────────────────────────────────

public record CreateWebhookCommand(
    string Name,
    string Url,
    string Secret,
    List<string> Events,
    string Format = WebhookSubscription.FormatJson
) : IRequest<Guid>;

public class CreateWebhookCommandValidator : AbstractValidator<CreateWebhookCommand>
{
    public CreateWebhookCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                       (uri.Scheme == "http" || uri.Scheme == "https"))
            .WithMessage("Url must be an absolute http(s) URL.");
        RuleFor(x => x.Secret).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.Events).NotEmpty()
            .Must(events => events.All(e => WebhookEvents.All.Contains(e.ToLowerInvariant())))
            .WithMessage($"Events must be from: {string.Join(", ", WebhookEvents.All)}");
        RuleFor(x => x.Format)
            .Must(f => f is WebhookSubscription.FormatJson or WebhookSubscription.FormatSlack)
            .WithMessage("Format must be 'json' or 'slack'.");
    }
}

public class CreateWebhookCommandHandler : IRequestHandler<CreateWebhookCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateWebhookCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateWebhookCommand request, CancellationToken cancellationToken)
    {
        WebhookGuard.RequireAdmin(_currentUser);

        var webhook = WebhookSubscription.Create(
            _currentUser.TenantId, request.Name, request.Url,
            request.Secret, request.Events, request.Format);

        _context.Add(webhook);
        await _context.SaveChangesAsync(cancellationToken);
        return webhook.Id;
    }
}

// ── Update ───────────────────────────────────────────────────────────────────

public record UpdateWebhookCommand(
    Guid Id,
    string Name,
    string Url,
    string? Secret,       // null/empty = keep existing
    List<string> Events,
    string Format,
    bool IsActive
) : IRequest;

public class UpdateWebhookCommandHandler : IRequestHandler<UpdateWebhookCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateWebhookCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateWebhookCommand request, CancellationToken cancellationToken)
    {
        WebhookGuard.RequireAdmin(_currentUser);

        var webhook = await _context.WebhookSubscriptions
            .FirstOrDefaultAsync(w =>
                w.Id == request.Id &&
                w.TenantId == _currentUser.TenantId && !w.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(WebhookSubscription), request.Id);

        webhook.Update(
            request.Name, request.Url, request.Secret,
            request.Events, request.Format, request.IsActive);
        _context.Update(webhook);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// ── Delete ───────────────────────────────────────────────────────────────────

public record DeleteWebhookCommand(Guid Id) : IRequest;

public class DeleteWebhookCommandHandler : IRequestHandler<DeleteWebhookCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteWebhookCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteWebhookCommand request, CancellationToken cancellationToken)
    {
        WebhookGuard.RequireAdmin(_currentUser);

        var webhook = await _context.WebhookSubscriptions
            .FirstOrDefaultAsync(w =>
                w.Id == request.Id &&
                w.TenantId == _currentUser.TenantId && !w.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(WebhookSubscription), request.Id);

        webhook.MarkAsDeleted();
        _context.Update(webhook);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// ── Test-fire ────────────────────────────────────────────────────────────────

public record TestWebhookCommand(Guid Id) : IRequest<string>;

public class TestWebhookCommandHandler : IRequestHandler<TestWebhookCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebhookSender _sender;

    public TestWebhookCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IWebhookSender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<string> Handle(
        TestWebhookCommand request, CancellationToken cancellationToken)
    {
        WebhookGuard.RequireAdmin(_currentUser);

        var webhook = await _context.WebhookSubscriptions
            .FirstOrDefaultAsync(w =>
                w.Id == request.Id &&
                w.TenantId == _currentUser.TenantId && !w.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(WebhookSubscription), request.Id);

        var status = await _sender.SendAsync(
            webhook, "test",
            new
            {
                message = "EdgePulse webhook test",
                subscription = webhook.Name,
                requestedBy = _currentUser.Email,
            },
            cancellationToken);

        webhook.RecordDelivery(status);
        _context.Update(webhook);
        await _context.SaveChangesAsync(cancellationToken);
        return status;
    }
}
