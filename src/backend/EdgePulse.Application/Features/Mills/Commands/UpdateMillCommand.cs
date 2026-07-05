using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using EdgePulse.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Mills.Commands;

public record UpdateMillCommand(
    Guid Id,
    string Name,
    string Location,
    string Timezone,
    bool HasInternet,
    DeploymentMode DeploymentMode
) : IRequest;

public class UpdateMillCommandValidator : AbstractValidator<UpdateMillCommand>
{
    public UpdateMillCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
    }
}

public class UpdateMillCommandHandler : IRequestHandler<UpdateMillCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateMillCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateMillCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive || _currentUser.IsMillManager)
            throw new ForbiddenAccessException();

        // SuperAdmin can manage mills across all tenants (matches GetMillsQuery);
        // CustomerAdmin is restricted to their own tenant.
        var query = _context.Mills.Where(x => x.Id == request.Id && !x.IsDeleted);
        if (!_currentUser.IsSuperAdmin)
            query = query.Where(x => x.TenantId == _currentUser.TenantId);

        var mill = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Mill), request.Id);

        mill.UpdateDetails(
            request.Name,
            request.Location,
            request.Timezone,
            request.HasInternet,
            request.DeploymentMode);

        _context.Update(mill);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
