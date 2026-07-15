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

## Phase 19D.1 - Android E2E and regression QA

- QA ran on Android API 36 against the Development backend through `http://10.0.2.2:5296`.
- The system Photo Picker opened without broad media-storage permission. Cancel, reopening, local-preview removal and multiple selection were exercised.
- Real WEBP and JPG selections uploaded through create-then-upload. Client tests cover JPG, JPEG, PNG and WEBP; an over-5-MB selection was rejected before upload and the ten-image boundary has a regression test.
- Details loaded bytes through authenticated Dio. The content endpoint returned an image MIME type with `Cache-Control: private, no-store`; no public URL, token, storage path or `ImagePath` was rendered.
- Image delete was exercised with cancel and confirm. The deleted image returned `404`, the remaining image returned `200`, and repeated delete returned a safe `404`.
- User A logout followed by User B login did not retain User A's Timeline item or private image access. A forged User B request for User A's image returned `404`.
- A confirmed gallery helper-text defect was fixed: it no longer says images will appear later when they are already visible.
- Final verification finished with 62 Flutter tests passing, analyzer clean and a rebuilt debug APK. Temporary QA accounts, tokens, Memory/image rows, local generated files and emulator media were removed after the run.

The deterministic offline boundary between a successful text write and a multipart upload was not force-stopped during this QA run. The create/edit flow keeps selected images on the active form and reuses the created/saved Memory ID, but that exact backend-offline handoff remains a required follow-up manual regression before declaring the image workflow fully frozen.

## Phase 19D.2 - Deterministic partial-success and retry

- `MemoryImageSaveFlow` has explicit idle, text-save, image-upload, partial-success, retry and complete states. A successful text request retains its returned Memory ID only for the active form/session.
- A failed image upload now shows a safe partial-success message with **Thử lại tải ảnh** and **Tiếp tục không có ảnh**. The selected local previews remain for Retry and are cleared only after upload succeeds or after the user leaves through Continue.
- Retry never repeats the successful `POST`/`PUT`: focused regression tests assert one text request, two upload attempts and the same Memory ID for both upload attempts. Continue makes no automatic background retry and creates no offline queue.
- `lib/qa/partial_upload_retry_qa.dart` is a separate QA target that replaces only the image repository and fails its first upload with a safe unavailable error. It is not imported by `main.dart`; normal and Release builds keep the real repository.
- The QA target built and booted on Android API 36. Its interactive Photo Picker retry pass still needs one supported manual-device run before Phase 19D can be declared fully frozen; no unverified Android result is recorded as passed.
