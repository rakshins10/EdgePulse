using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Attachments.Queries;

public record GetAttachmentsQuery(
    string EntityType,
    Guid EntityId
) : IRequest<List<AttachmentDto>>;

public record AttachmentDto(
    Guid Id,
    string FileName,
    long FileSize,
    string ContentType,
    string FileCategory,
    string UploadedBy,
    DateTime UploadedAt
);

public class GetAttachmentsQueryHandler
    : IRequestHandler<GetAttachmentsQuery, List<AttachmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAttachmentsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<AttachmentDto>> Handle(
        GetAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Attachments
            .Where(a =>
                a.TenantId == _currentUser.TenantId &&
                a.EntityType == request.EntityType &&
                a.EntityId == request.EntityId &&
                !a.IsDeleted)
            .OrderBy(a => a.DisplayOrder)
            .ThenByDescending(a => a.UploadedAt)
            .Select(a => new AttachmentDto(
                a.Id, a.FileName, a.FileSize, a.ContentType,
                a.FileCategory, a.UploadedBy, a.UploadedAt))
            .ToListAsync(cancellationToken);
    }
}
