using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Areas.Commands;

public record CreateAreaCommand(
    Guid MillId,
    string Name,
    string Code,
    Guid? LocationTypeId,
    string? Description
) : IRequest<Guid>;

public class CreateAreaCommandValidator
    : AbstractValidator<CreateAreaCommand>
{
    public CreateAreaCommandValidator()
    {
        RuleFor(x => x.MillId)
            .NotEmpty().WithMessage("Mill is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(20)
            .Matches("^[A-Z0-9]+$").WithMessage(
                "Code must be uppercase letters and numbers only.");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);
    }
}

public class CreateAreaCommandHandler
    : IRequestHandler<CreateAreaCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateAreaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateAreaCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Verify mill belongs to tenant
        var millExists = await _context.Mills
            .AnyAsync(x =>
                x.Id == request.MillId &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken);

        if (!millExists)
            throw new NotFoundException(nameof(Mill), request.MillId);

        // MillManager can only add areas to their assigned mill
        if (_currentUser.IsMillManager &&
            _currentUser.MillId != request.MillId)
            throw new ForbiddenAccessException();

        // Check code unique within mill
        var codeExists = await _context.Areas
            .AnyAsync(x =>
                x.MillId == request.MillId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted,
                cancellationToken);

        if (codeExists)
            throw new ConflictException(
                $"Area with code '{request.Code}' already exists " +
                $"in this mill.");

        var area = Area.Create(
            tenantId: _currentUser.TenantId,
            millId: request.MillId,
            name: request.Name,
            code: request.Code,
            locationTypeId: request.LocationTypeId,
            description: request.Description);

        _context.Add(area);
        await _context.SaveChangesAsync(cancellationToken);
        return area.Id;
    }
}
