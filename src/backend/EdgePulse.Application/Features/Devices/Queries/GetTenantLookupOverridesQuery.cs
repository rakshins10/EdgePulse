using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetTenantLookupOverridesQuery(
    string? LookupType = null
) : IRequest<List<TenantLookupOverrideDto>>;

public record TenantLookupOverrideDto(
    Guid Id,
    string LookupType,
    Guid LookupId,
    string? DisplayName,
    bool IsActive,
    string UpdatedBy,
    DateTime UpdatedAt
);

public class GetTenantLookupOverridesQueryHandler
    : IRequestHandler<GetTenantLookupOverridesQuery,
        List<TenantLookupOverrideDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTenantLookupOverridesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<TenantLookupOverrideDto>> Handle(
        GetTenantLookupOverridesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.TenantLookupOverrides
            .Where(x => x.TenantId == _currentUser.TenantId);

        if (!string.IsNullOrEmpty(request.LookupType))
            query = query.Where(x =>
                x.LookupType == request.LookupType);

        return await query
            .OrderBy(x => x.LookupType)
            .ThenBy(x => x.LookupId)
            .Select(x => new TenantLookupOverrideDto(
                x.Id,
                x.LookupType,
                x.LookupId,
                x.DisplayName,
                x.IsActive,
                x.UpdatedBy,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
