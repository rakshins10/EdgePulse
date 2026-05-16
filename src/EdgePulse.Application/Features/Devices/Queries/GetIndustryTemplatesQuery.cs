using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetIndustryTemplatesQuery : IRequest<List<IndustryTemplateDto>>;

public record IndustryTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault
);

public class GetIndustryTemplatesQueryHandler
    : IRequestHandler<GetIndustryTemplatesQuery, List<IndustryTemplateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetIndustryTemplatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<IndustryTemplateDto>> Handle(
        GetIndustryTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        // Only SuperAdmin can see all templates
        if (!_currentUser.IsSuperAdmin)
            throw new Common.Exceptions.ForbiddenAccessException();

        return await _context.IndustryTemplates
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new IndustryTemplateDto(
                x.Id, x.Name, x.Description, x.IsDefault))
            .ToListAsync(cancellationToken);
    }
}
