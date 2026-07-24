using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Branding;

public record BrandingDto(
    string ProductName,
    string? LogoUrl,
    string? AccentColor
);

// ── Get (any authenticated user — the shell needs it at load) ────────────────

public record GetBrandingQuery : IRequest<BrandingDto>;

public class GetBrandingQueryHandler : IRequestHandler<GetBrandingQuery, BrandingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBrandingQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<BrandingDto> Handle(
        GetBrandingQuery request, CancellationToken cancellationToken)
    {
        var branding = await _context.TenantBrandings
            .Where(b => b.TenantId == _currentUser.TenantId && !b.IsDeleted)
            .Select(b => new BrandingDto(b.ProductName, b.LogoUrl, b.AccentColor))
            .FirstOrDefaultAsync(cancellationToken);

        return branding ?? new BrandingDto("EdgePulse", null, null);
    }
}

// ── Upsert (admins) ──────────────────────────────────────────────────────────

public record UpdateBrandingCommand(
    string ProductName,
    string? LogoUrl,
    string? AccentColor
) : IRequest;

public class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    public UpdateBrandingCommandValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.LogoUrl).MaximumLength(500)
            .Must(u => u is null || Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("LogoUrl must be an absolute URL.");
        RuleFor(x => x.AccentColor)
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => x.AccentColor != null)
            .WithMessage("AccentColor must be a hex colour like #3b82f6.");
    }
}

public class UpdateBrandingCommandHandler : IRequestHandler<UpdateBrandingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateBrandingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsCustomerAdmin)
            throw new ForbiddenAccessException();

        var existing = await _context.TenantBrandings
            .FirstOrDefaultAsync(b =>
                b.TenantId == _currentUser.TenantId && !b.IsDeleted,
                cancellationToken);

        if (existing is null)
        {
            _context.Add(TenantBranding.Create(
                _currentUser.TenantId, request.ProductName,
                request.LogoUrl, request.AccentColor));
        }
        else
        {
            existing.Update(request.ProductName, request.LogoUrl, request.AccentColor);
            _context.Update(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
