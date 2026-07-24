# Sprint 18 — File Attachments (Epic #10, US-018 #28)

**Date:** July 2026
**Goal:** Let users attach files (manuals, datasheets, photos, CAD drawings)
to devices — the `Attachment` entity existed since Sprint 1 but had no
endpoints, no storage and no UI.

---

## What was delivered

### Backend
- **`IFileStorage`** abstraction (Application) + **`LocalFileStorage`**
  (Infrastructure) — writes under `Storage:AttachmentsRoot`
  (default `data/attachments`) as
  `{tenantId}/{entityType}/{entityId}/{guid}.{ext}`. A cloud implementation
  (Azure Blob) can replace it via DI without touching handlers.
- Handlers (`Features/Attachments/`):
  - `GetAttachmentsQuery(entityType, entityId)` — tenant-scoped listing
  - `UploadAttachmentCommand` — validates entity exists in-tenant, role
    (Operator/Executive are read-only per US-018), extension allowlist
    (PDF, images, Office, CSV, TXT/MD, DWG/DXF, ZIP) and 25 MB limit;
    stores file then persists metadata
  - `DownloadAttachmentQuery` — tenant-checked stream + original filename
  - `DeleteAttachmentCommand` — soft-deletes the row, best-effort deletes
    the physical file
- **`AttachmentsController`**:
  - `GET    /api/attachments?entityType&entityId`
  - `POST   /api/attachments` (multipart/form-data, 25 MB request limit)
  - `GET    /api/attachments/{id}/download`
  - `DELETE /api/attachments/{id}`
- No migration needed — the `Attachments` table shipped with Sprint 1.

### Dashboard
- **`AttachmentsCard`** component on the Device detail page: file table
  (name → download, category chip, size, uploader, date), upload button with
  accept-filter, delete with confirm dialog + toasts. Upload/delete hidden for
  Operator/Executive. Works for Mill/Area too (`entityType` prop) — currently
  mounted on Device detail.
- en/fi/sv strings.

## Verified end-to-end (live)
1. ✅ Upload `pump-manual.pdf` → 201 with metadata
2. ✅ List returns it
3. ✅ Download → HTTP 200, **byte-identical** to the original
4. ✅ File on disk at `data/attachments/{tenant}/{Device}/{deviceId}/{guid}.pdf`
5. ✅ Delete → 204; list empty; physical file removed
6. ✅ 79 unit tests green (9 new attachment tests incl. validator edge cases)

## Notes
- Attachment storage root is gitignored (`src/backend/EdgePulse.API/data/`).
- For Docker deployment, mount a volume at the storage root so files persist.
- `uploadedBy` falls back FullName → Email → UserId.

## Follow-ups
- Attachments card on Mill / Area pages (component already supports it)
- Image thumbnail preview
