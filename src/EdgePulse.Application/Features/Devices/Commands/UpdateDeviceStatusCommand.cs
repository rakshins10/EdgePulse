using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record UpdateDeviceStatusCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Color,
    int SortOrder
) : IRequest;

public class UpdateDeviceStatusCommandValidator
    : AbstractValidator<UpdateDeviceStatusCommand>
{
    public UpdateDeviceStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300)
            .When(x => x.Description != null);
        RuleFor(x => x.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => x.Color != null)
            .WithMessage("Color must be a valid hex code e.g. #22c55e");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateDeviceStatusCommandHandler
    : IRequestHandler<UpdateDeviceStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateDeviceStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateDeviceStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var status = await _context.DeviceStatuses
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(DeviceStatus), request.Id);

        if (status.IsSystem)
            throw new ForbiddenAccessException();

        status.UpdateDetails(request.Name, request.Description);
        _context.Update(status);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
