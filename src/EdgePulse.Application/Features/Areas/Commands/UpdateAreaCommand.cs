using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Areas.Commands;

public record UpdateAreaCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid? LocationTypeId
) : IRequest;

public class UpdateAreaCommandValidator : AbstractValidator<UpdateAreaCommand>
{
    public UpdateAreaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}

public class UpdateAreaCommandHandler : IRequestHandler<UpdateAreaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateAreaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var area = await _context.Areas
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Area), request.Id);

        // MillManager can only edit areas in their assigned mill
        if (_currentUser.IsMillManager &&
            _currentUser.MillId.HasValue &&
            _currentUser.MillId.Value != area.MillId)
            throw new ForbiddenAccessException();

        area.UpdateDetails(request.Name, request.Description, request.LocationTypeId);
        _context.Update(area);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
