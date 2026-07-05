using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Tenants.Queries;

public record GetTenantsQuery : IRequest<List<TenantDto>>;

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string ContactEmail,
    string Status,
    string? TemplateName,
    DateTime CreatedAt
);

public class GetTenantsQueryHandler
    : IRequestHandler<GetTenantsQuery, List<TenantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTenantsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<TenantDto>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
            throw new ForbiddenAccessException();

        return await _context.Tenants
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new TenantDto(
                x.Id,
                x.Name,
                x.Slug,
                x.ContactEmail,
                x.Status,
                x.TenantTemplate != null
                    ? x.TenantTemplate.Template!.Name
                    : null,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
