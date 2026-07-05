using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
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
    private readonly ILookupTranslator _translator;

    public GetMetricTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILookupTranslator translator)
    {
        _context = context;
        _currentUser = currentUser;
        _translator = translator;
    }

    public async Task<List<MetricTypeDto>> Handle(
        GetMetricTypesQuery request,
        CancellationToken cancellationToken)
    {
        var metricTypes = await _context.MetricTypes
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new MetricTypeDto(
                x.Id, x.Name, x.Code, x.DefaultUnit,
                x.Description, x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);

        var translations = await _translator.GetMapAsync(
            LookupTypes.MetricType, cancellationToken);
        if (translations.Count == 0)
            return metricTypes;

        return metricTypes
            .Select(m => translations.TryGetValue(m.Id, out var tr)
                ? m with { Name = tr.Name, Description = tr.Description ?? m.Description }
                : m)
            .ToList();
    }
}
