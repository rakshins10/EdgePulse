using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Attachments.Queries;

public record DownloadAttachmentQuery(Guid Id) : IRequest<AttachmentDownload>;

public record AttachmentDownload(
    Stream Content,
    string FileName,
    string ContentType
);

public class DownloadAttachmentQueryHandler
    : IRequestHandler<DownloadAttachmentQuery, AttachmentDownload>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _storage;

    public DownloadAttachmentQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileStorage storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<AttachmentDownload> Handle(
        DownloadAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a =>
                a.Id == request.Id &&
                a.TenantId == _currentUser.TenantId &&
                !a.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), request.Id);

        var stream = await _storage.OpenReadAsync(
            attachment.StoragePath, cancellationToken);

        return new AttachmentDownload(
            stream, attachment.FileName, attachment.ContentType);
    }
}
