using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetMetricTypesQuery : IRequest<List<MetricTypeDto>>;

public record MetricTypeDto(
    Guid Id,
    string Name,
    string Code,
    string DefaultUnit,
    string? Description,
    bool IsSystem,
    int SortOrder
);

public class GetMetricTypesQueryHandler
    : IRequestHandler<GetMetricTypesQuery, List<MetricTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMetricTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<MetricTypeDto>> Handle(
        GetMetricTypesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.MetricTypes
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new MetricTypeDto(
                x.Id, x.Name, x.Code, x.DefaultUnit,
                x.Description, x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
