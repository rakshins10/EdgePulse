using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record UpdateLocationTypeCommand(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder = 0
) : IRequest;

public class UpdateLocationTypeCommandValidator
    : AbstractValidator<UpdateLocationTypeCommand>
{
    public UpdateLocationTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300).When(x => x.Description != null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateLocationTypeCommandHandler
    : IRequestHandler<UpdateLocationTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateLocationTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateLocationTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var locationType = await _context.LocationTypes
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(LocationType), request.Id);

        // System values cannot be edited directly (tenants use a TenantLookupOverride).
        if (locationType.IsSystem)
            throw new ForbiddenAccessException();

        locationType.UpdateDetails(request.Name, request.Description);
        _context.Update(locationType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
