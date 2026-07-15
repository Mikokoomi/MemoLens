import 'dart:typed_data';

import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/app.dart';
import '../core/network/api_exception.dart';
import '../features/memories/data/memory_image_repository.dart';
import '../features/memories/data/memory_repository.dart';

/// QA-only entrypoint. Run with `flutter run -t lib/qa/partial_upload_retry_qa.dart`.
/// It is never imported by main.dart and is not used for normal Release builds.
void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(
    ProviderScope(
      overrides: [
        memoryImageRepositoryProvider.overrideWith(
          (ref) => _FailFirstUploadRepository(
            ApiMemoryImageRepository(ref.watch(authenticatedDioProvider)),
          ),
        ),
      ],
      child: const MemoLensApp(),
    ),
  );
}

class _FailFirstUploadRepository implements MemoryImageRepository {
  _FailFirstUploadRepository(this._delegate);
  final MemoryImageRepository _delegate;
  bool _shouldFail = true;

  @override
  Future<MemoryImageUploadResult> uploadImages(
    int memoryId,
    List<SelectedMemoryImage> images, {
    void Function(int sent, int total)? onSendProgress,
  }) {
    if (_shouldFail) {
      _shouldFail = false;
      return Future<MemoryImageUploadResult>.error(
        const MemoryRequestException(
          ApiException(
            ApiErrorType.unavailable,
            'Máy chủ ảnh tạm thời không khả dụng.',
          ),
        ),
      );
    }
    return _delegate.uploadImages(
      memoryId,
      images,
      onSendProgress: onSendProgress,
    );
  }

  @override
  Future<void> deleteImage(int memoryId, int imageId) =>
      _delegate.deleteImage(memoryId, imageId);

  @override
  Future<Uint8List> loadImageBytes(int imageId) =>
      _delegate.loadImageBytes(imageId);
}
