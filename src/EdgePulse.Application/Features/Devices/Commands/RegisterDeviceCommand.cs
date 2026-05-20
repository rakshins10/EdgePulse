using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EdgePulse.Application.Features.Devices.Commands;

public record RegisterDeviceCommand(
    Guid AreaId,
    Guid TypeId,
    Guid StatusId,
    string Name,
    string Code,
    Guid? ManufacturerId,
    Guid? ModelId,
    string? SerialNumber,
    DateOnly? InstallDate,
    string? Description
) : IRequest<RegisterDeviceResult>;

public record RegisterDeviceResult(
    Guid DeviceId,
    string Code,
    string ApiKey
);

public class RegisterDeviceCommandValidator
    : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.AreaId)
            .NotEmpty().WithMessage("Area is required.");
        RuleFor(x => x.TypeId)
            .NotEmpty().WithMessage("Device type is required.");
        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("Device status is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50)
            .Matches("^[A-Z0-9-]+$").WithMessage(
                "Code must be uppercase letters, numbers and hyphens only.");
        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).When(x => x.SerialNumber != null);
        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);
    }
}

public class RegisterDeviceCommandHandler
    : IRequestHandler<RegisterDeviceCommand, RegisterDeviceResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RegisterDeviceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<RegisterDeviceResult> Handle(
        RegisterDeviceCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Verify area belongs to tenant
        var area = await _context.Areas
            .FirstOrDefaultAsync(x =>
                x.Id == request.AreaId &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Area), request.AreaId);

        // MillManager can only register in their mill
        if (_currentUser.IsMillManager &&
            _currentUser.MillId != area.MillId)
            throw new ForbiddenAccessException();

        // Check device code unique within tenant
        var codeExists = await _context.Devices
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted,
                cancellationToken);

        if (codeExists)
            throw new ConflictException(
                $"Device with code '{request.Code}' already exists.");

        // Create device
        var device = Device.Create(
            tenantId: _currentUser.TenantId,
            millId: area.MillId,
            areaId: request.AreaId,
            typeId: request.TypeId,
            statusId: request.StatusId,
            name: request.Name,
            code: request.Code,
            serialNumber: request.SerialNumber,
            installDate: request.InstallDate,
            manufacturerId: request.ManufacturerId,
            modelId: request.ModelId,
            description: request.Description);

        _context.Add(device);

        // Generate API key -- plain text shown ONCE, hash stored
        var plainTextKey = GenerateApiKey();
        var keyHash = HashApiKey(plainTextKey);
        var keyPrefix = plainTextKey.Substring(0, 8);

        var apiKey = DeviceApiKey.Create(
            tenantId: _currentUser.TenantId,
            deviceId: device.Id,
            keyHash: keyHash,
            keyPrefix: keyPrefix);

        _context.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return new RegisterDeviceResult(
            device.Id,
            device.Code,
            plainTextKey);
    }

    private static string GenerateApiKey()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(24);
        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "a")
            .Replace("/", "b")
            .Replace("=", "c")
            .Substring(0, 32);
        return $"dev_{randomPart}";
    }

    private static string HashApiKey(string plainTextKey)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plainTextKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
