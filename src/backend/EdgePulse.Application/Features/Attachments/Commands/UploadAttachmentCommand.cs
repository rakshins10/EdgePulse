using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Attachments.Queries;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Attachments.Commands;

public record UploadAttachmentCommand(
    string EntityType,
    Guid EntityId,
    string FileName,
    string ContentType,
    long FileSize,
    Stream Content,
    string FileCategory = "General"
) : IRequest<AttachmentDto>;

public class UploadAttachmentCommandValidator
    : AbstractValidator<UploadAttachmentCommand>
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    // Document / image formats an industrial platform legitimately needs.
    // Executables and scripts are deliberately not accepted.
    public static readonly string[] AllowedExtensions =
    [
        ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".xlsx", ".xls", ".csv", ".docx", ".doc", ".txt", ".md",
        ".dwg", ".dxf", ".zip"
    ];

    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(t => t is "Device" or "Mill" or "Area")
            .WithMessage("EntityType must be Device, Mill or Area.");

        RuleFor(x => x.EntityId).NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty().MaximumLength(255)
            .Must(f => AllowedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()))
            .WithMessage(
                $"File type not allowed. Accepted: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File exceeds the 25 MB limit.");

        RuleFor(x => x.FileCategory).NotEmpty().MaximumLength(50);
    }
}

public class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _storage;

    public UploadAttachmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileStorage storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<AttachmentDto> Handle(
        UploadAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        // US-018: MillManager (and admins) upload; Operator/Executive read-only.
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // The target entity must exist and belong to the tenant.
        var entityExists = request.EntityType switch
        {
            "Device" => await _context.Devices.AnyAsync(d =>
                d.Id == request.EntityId &&
                d.TenantId == _currentUser.TenantId && !d.IsDeleted,
                cancellationToken),
            "Mill" => await _context.Mills.AnyAsync(m =>
                m.Id == request.EntityId &&
                m.TenantId == _currentUser.TenantId && !m.IsDeleted,
                cancellationToken),
            "Area" => await _context.Areas.AnyAsync(a =>
                a.Id == request.EntityId &&
                a.TenantId == _currentUser.TenantId && !a.IsDeleted,
                cancellationToken),
            _ => false
        };

        if (!entityExists)
            throw new NotFoundException(request.EntityType, request.EntityId);

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        var storagePath = await _storage.SaveAsync(
            _currentUser.TenantId, request.EntityType, request.EntityId,
            storedFileName, request.Content, cancellationToken);

        var attachment = Attachment.Create(
            tenantId: _currentUser.TenantId,
            entityType: request.EntityType,
            entityId: request.EntityId,
            fileName: request.FileName,
            storedFileName: storedFileName,
            fileSize: request.FileSize,
            contentType: request.ContentType,
            fileCategory: request.FileCategory,
            storagePath: storagePath,
            uploadedBy: FirstNonEmpty(
                _currentUser.FullName, _currentUser.Email, _currentUser.UserId));

        _context.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        return new AttachmentDto(
            attachment.Id, attachment.FileName, attachment.FileSize,
            attachment.ContentType, attachment.FileCategory,
            attachment.UploadedBy, attachment.UploadedAt);
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "unknown";
}
