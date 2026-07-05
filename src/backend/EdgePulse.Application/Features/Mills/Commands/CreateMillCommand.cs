using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using EdgePulse.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Mills.Commands;

public record CreateMillCommand(
    string Name,
    string Code,
    string Location,
    string Timezone,
    bool HasInternet = true,
    DeploymentMode DeploymentMode = DeploymentMode.Cloud
) : IRequest<Guid>;

public class CreateMillCommandValidator
    : AbstractValidator<CreateMillCommand>
{
    public CreateMillCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(20)
            .Matches("^[A-Z0-9]+$").WithMessage(
                "Code must be uppercase letters and numbers only.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(300);

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .MaximumLength(100);
    }
}

public class CreateMillCommandHandler
    : IRequestHandler<CreateMillCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateMillCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateMillCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Check code unique within tenant
        var codeExists = await _context.Mills
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted,
                cancellationToken);

        if (codeExists)
            throw new ConflictException(
                $"Mill with code '{request.Code}' already exists.");

        var mill = Mill.Create(
            tenantId: _currentUser.TenantId,
            name: request.Name,
            code: request.Code,
            location: request.Location,
            timezone: request.Timezone,
            hasInternet: request.HasInternet,
            deploymentMode: request.DeploymentMode);

        _context.Add(mill);
        await _context.SaveChangesAsync(cancellationToken);
        return mill.Id;
    }
}
