import '../data/memory_image_repository.dart';
import '../data/models/memory_models.dart';

enum MemoryImageSaveStage {
  idle,
  savingText,
  uploadingImages,
  partialSuccess,
  retryingImages,
  complete,
  failure,
}

class MemoryImageSaveResult {
  const MemoryImageSaveResult._({
    required this.stage,
    this.details,
    this.message,
  });

  const MemoryImageSaveResult.complete(MemoryDetails details)
    : this._(stage: MemoryImageSaveStage.complete, details: details);

  const MemoryImageSaveResult.partialSuccess(MemoryDetails details)
    : this._(
        stage: MemoryImageSaveStage.partialSuccess,
        details: details,
        message:
            'Kỷ niệm đã được lưu, nhưng ảnh chưa tải lên. Bạn có thể thử lại hoặc tiếp tục không có ảnh.',
      );

  const MemoryImageSaveResult.failure()
    : this._(stage: MemoryImageSaveStage.failure);

  final MemoryImageSaveStage stage;
  final MemoryDetails? details;
  final String? message;

  bool get isComplete => stage == MemoryImageSaveStage.complete;
  bool get isPartialSuccess => stage == MemoryImageSaveStage.partialSuccess;
}

/// Keeps the text-save result while a selected image batch is retried.
///
/// This object is owned by one Create or Edit page. It is deliberately not a
/// provider or queue: leaving the page, logging out, or changing accounts
/// discards the local selection and the retained Memory ID.
class MemoryImageSaveFlow {
  MemoryImageSaveStage _stage = MemoryImageSaveStage.idle;
  MemoryDetails? _savedDetails;
  String? _userId;
  bool _isRunning = false;

  MemoryImageSaveStage get stage => _stage;
  MemoryDetails? get savedDetails => _savedDetails;
  bool get isRunning => _isRunning;

  Future<MemoryImageSaveResult> saveAndUpload({
    required String? userId,
    required List<SelectedMemoryImage> images,
    required Future<MemoryDetails?> Function() saveText,
    required Future<MemoryDetails> Function(
      int memoryId,
      List<SelectedMemoryImage> images,
    )
    uploadImages,
  }) async {
    resetForUser(userId);
    if (_isRunning) return const MemoryImageSaveResult.failure();
    _isRunning = true;
    try {
      if (_savedDetails == null) {
        _stage = MemoryImageSaveStage.savingText;
        final details = await saveText();
        if (details == null) {
          _stage = MemoryImageSaveStage.failure;
          return const MemoryImageSaveResult.failure();
        }
        _savedDetails = details;
      }

      if (images.isEmpty) {
        _stage = MemoryImageSaveStage.complete;
        return MemoryImageSaveResult.complete(_savedDetails!);
      }

      _stage = _stage == MemoryImageSaveStage.partialSuccess
          ? MemoryImageSaveStage.retryingImages
          : MemoryImageSaveStage.uploadingImages;
      try {
        final details = await uploadImages(_savedDetails!.id, images);
        _savedDetails = details;
        _stage = MemoryImageSaveStage.complete;
        return MemoryImageSaveResult.complete(details);
      } catch (_) {
        _stage = MemoryImageSaveStage.partialSuccess;
        return MemoryImageSaveResult.partialSuccess(_savedDetails!);
      }
    } finally {
      _isRunning = false;
    }
  }

  MemoryDetails? continueWithoutImages() {
    final details = _savedDetails;
    if (details != null) _stage = MemoryImageSaveStage.complete;
    return details;
  }

  void resetForUser(String? userId) {
    if (_userId != userId) {
      _userId = userId;
      _savedDetails = null;
      _stage = MemoryImageSaveStage.idle;
      _isRunning = false;
    }
  }

  void clear() {
    _userId = null;
    _savedDetails = null;
    _stage = MemoryImageSaveStage.idle;
    _isRunning = false;
  }
}
