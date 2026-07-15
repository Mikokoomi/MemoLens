# Flutter Private Memory Images

## Scope

Phase 19D uses the existing JWT API only. It adds gallery selection, local preview, private byte display and individual image deletion for a user's own Memory. There are no public image URLs, disk cache, Albums, editing tools or backend changes.

## API used

- `POST /api/v1/memories/{memoryId}/images` sends multipart field `files`.
- `GET /api/v1/images/{imageId}/content` is loaded through the authenticated Dio client with `ResponseType.bytes` and rendered from memory.
- `DELETE /api/v1/memories/{memoryId}/images/{imageId}` accepts the normal success-only envelope.

The client validates JPG/JPEG/PNG/WEBP, 5 MB per file and 10 images per Memory before upload. The server remains authoritative.

## Create and edit flow

Memory text is saved first. Selected images are uploaded only after the server returns the Memory ID. If upload fails, the Memory remains saved and the selected files remain on the current form for a manual retry. A retry reuses the already-created Memory ID and does not create a duplicate.

Edit saves text first, then uploads selected additions. Existing uploaded images are not changed unless the user confirms deletion of one image.

## Privacy and lifecycle

Selected file paths are not displayed or persisted. Full image bytes are held only by `autoDispose` providers while a page is active. The byte provider watches the authenticated user ID, so logout/account changes invalidate the prior session's private image state. No token is placed in an image URL.

Android uses the operating-system image picker and does not request broad storage permission. iOS has a neutral photo-library usage description; iOS builds remain unverified on Windows.

## Limits

There is no image crop, edit, reorder, manual cover, permanent disk cache, offline sync or full-screen editor. Upload failures are retried manually to avoid duplicate multipart requests.
