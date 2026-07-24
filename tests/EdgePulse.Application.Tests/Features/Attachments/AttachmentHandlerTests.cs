using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Attachments.Commands;
using EdgePulse.Application.Features.Attachments.Queries;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace EdgePulse.Application.Tests.Features.Attachments;

public class AttachmentHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static (InMemoryApplicationDbContext ctx,
                    TestCurrentUserService user,
                    IFileStorage storage) Setup()
    {
        var ctx = TestDbContextFactory.Create();
        var user = TestCurrentUserService.AsCustomerAdmin(_tenantId);
        var storage = Substitute.For<IFileStorage>();
        storage.SaveAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(),
                Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(
                $"{ci.ArgAt<Guid>(0)}/{ci.ArgAt<string>(1)}/{ci.ArgAt<Guid>(2)}/{ci.ArgAt<string>(3)}"));
        return (ctx, user, storage);
    }

    private static Device SeedDevice(InMemoryApplicationDbContext ctx, Guid tenantId)
    {
        var device = Device.Create(
            tenantId, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "Pump", "P1");
        ctx.Add(device);
        ctx.SaveChanges();
        return device;
    }

    private static UploadAttachmentCommand UploadCmd(Guid deviceId, string name = "manual.pdf") =>
        new("Device", deviceId, name, "application/pdf", 1234, new MemoryStream([1, 2, 3]));

    // ── Upload ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_ValidFile_PersistsRecordAndStoresFile()
    {
        var (ctx, user, storage) = Setup();
        var device = SeedDevice(ctx, _tenantId);

        var handler = new UploadAttachmentCommandHandler(ctx, user, storage);
        var dto = await handler.Handle(UploadCmd(device.Id), CancellationToken.None);

        dto.FileName.Should().Be("manual.pdf");
        ctx.AttachmentSet.Should().ContainSingle(a => a.Id == dto.Id);
        await storage.Received(1).SaveAsync(
            _tenantId, "Device", device.Id,
            Arg.Is<string>(s => s.EndsWith(".pdf")),
            Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_UnknownDevice_ThrowsNotFound()
    {
        var (ctx, user, storage) = Setup();

        var handler = new UploadAttachmentCommandHandler(ctx, user, storage);
        var act = async () => await handler.Handle(
            UploadCmd(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Upload_OtherTenantsDevice_ThrowsNotFound()
    {
        var (ctx, user, storage) = Setup();
        var foreignDevice = SeedDevice(ctx, Guid.NewGuid());

        var handler = new UploadAttachmentCommandHandler(ctx, user, storage);
        var act = async () => await handler.Handle(
            UploadCmd(foreignDevice.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Upload_Operator_ThrowsForbidden()
    {
        var (ctx, _, storage) = Setup();
        var device = SeedDevice(ctx, _tenantId);
        var operatorUser = TestCurrentUserService.AsOperator(_tenantId);

        var handler = new UploadAttachmentCommandHandler(ctx, operatorUser, storage);
        var act = async () => await handler.Handle(
            UploadCmd(device.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public void UploadValidator_RejectsExecutableExtension()
    {
        var validator = new UploadAttachmentCommandValidator();
        var result = validator.Validate(
            new UploadAttachmentCommand(
                "Device", Guid.NewGuid(), "virus.exe",
                "application/octet-stream", 100, Stream.Null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UploadValidator_RejectsOversizedFile()
    {
        var validator = new UploadAttachmentCommandValidator();
        var result = validator.Validate(
            new UploadAttachmentCommand(
                "Device", Guid.NewGuid(), "big.pdf",
                "application/pdf",
                UploadAttachmentCommandValidator.MaxFileSizeBytes + 1,
                Stream.Null));

        result.IsValid.Should().BeFalse();
    }

    // ── List ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAttachments_ReturnsOnlyOwnTenantsEntityFiles()
    {
        var (ctx, user, storage) = Setup();
        var device = SeedDevice(ctx, _tenantId);

        var uploadHandler = new UploadAttachmentCommandHandler(ctx, user, storage);
        await uploadHandler.Handle(UploadCmd(device.Id, "a.pdf"), CancellationToken.None);
        await uploadHandler.Handle(UploadCmd(device.Id, "b.png"), CancellationToken.None);

        // foreign attachment on some other tenant
        ctx.Add(Attachment.Create(
            Guid.NewGuid(), "Device", device.Id, "foreign.pdf", "x.pdf",
            10, "application/pdf", "General", "p", "someone"));
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAttachmentsQueryHandler(ctx, user);
        var result = await handler.Handle(
            new GetAttachmentsQuery("Device", device.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(a => a.FileName).Should().BeEquivalentTo("a.pdf", "b.png");
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletesRow_AndRemovesFile()
    {
        var (ctx, user, storage) = Setup();
        var device = SeedDevice(ctx, _tenantId);
        var uploadHandler = new UploadAttachmentCommandHandler(ctx, user, storage);
        var dto = await uploadHandler.Handle(UploadCmd(device.Id), CancellationToken.None);

        var handler = new DeleteAttachmentCommandHandler(ctx, user, storage);
        await handler.Handle(new DeleteAttachmentCommand(dto.Id), CancellationToken.None);

        ctx.AttachmentSet.Find(dto.Id)!.IsDeleted.Should().BeTrue();
        await storage.Received(1).DeleteAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Operator_ThrowsForbidden()
    {
        var (ctx, user, storage) = Setup();
        var device = SeedDevice(ctx, _tenantId);
        var uploadHandler = new UploadAttachmentCommandHandler(ctx, user, storage);
        var dto = await uploadHandler.Handle(UploadCmd(device.Id), CancellationToken.None);

        var handler = new DeleteAttachmentCommandHandler(
            ctx, TestCurrentUserService.AsOperator(_tenantId), storage);
        var act = async () => await handler.Handle(
            new DeleteAttachmentCommand(dto.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
