using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Attachments.Commands;

public record DeleteAttachmentCommand(Guid Id) : IRequest;

public class DeleteAttachmentCommandHandler
    : IRequestHandler<DeleteAttachmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _storage;

    public DeleteAttachmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileStorage storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task Handle(
        DeleteAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a =>
                a.Id == request.Id &&
                a.TenantId == _currentUser.TenantId &&
                !a.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), request.Id);

        attachment.SoftDelete();
        _context.Update(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        // Remove the physical file after the DB row is soft-deleted.
        // Best effort — a failed disk delete must not undo the operation.
        try
        {
            await _storage.DeleteAsync(attachment.StoragePath, cancellationToken);
        }
        catch
        {
            // The row is gone from every listing; orphaned files can be
            // cleaned by an ops job. Never fail the request over this.
        }
    }
}
