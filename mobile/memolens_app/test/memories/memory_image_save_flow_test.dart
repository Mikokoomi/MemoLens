import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker/image_picker.dart';
import 'package:memolens_app/features/memories/application/memory_image_save_flow.dart';
import 'package:memolens_app/features/memories/data/memory_image_repository.dart';
import 'package:memolens_app/features/memories/data/models/memory_models.dart';

void main() {
  final draftImage = SelectedMemoryImage(
    file: XFile('qa-photo.jpg'),
    displayName: 'qa-photo.jpg',
    extension: 'jpg',
    byteLength: 128,
  );

  test(
    'create saves once and retries the same Memory ID after upload failure',
    () async {
      final flow = MemoryImageSaveFlow();
      var createCalls = 0;
      var uploadCalls = 0;
      final uploadIds = <int>[];

      Future<MemoryDetails?> create() async {
        createCalls++;
        return _details(41);
      }

      Future<MemoryDetails> upload(
        int memoryId,
        List<SelectedMemoryImage> images,
      ) async {
        uploadCalls++;
        uploadIds.add(memoryId);
        if (uploadCalls == 1) throw StateError('offline');
        return _details(memoryId, images: [_imageMetadata]);
      }

      final first = await flow.saveAndUpload(
        userId: 'user-a',
        images: [draftImage],
        saveText: create,
        uploadImages: upload,
      );
      expect(first.isPartialSuccess, isTrue);
      expect(first.message, contains('Kỷ niệm đã được lưu'));
      expect(createCalls, 1);
      expect(uploadCalls, 1);
      expect(uploadIds, [41]);

      final retry = await flow.saveAndUpload(
        userId: 'user-a',
        images: [draftImage],
        saveText: create,
        uploadImages: upload,
      );
      expect(retry.isComplete, isTrue);
      expect(createCalls, 1);
      expect(uploadCalls, 2);
      expect(uploadIds, [41, 41]);
      expect(retry.details!.images, hasLength(1));
    },
  );

  test(
    'continue without images retains one created Memory and does not retry upload',
    () async {
      final flow = MemoryImageSaveFlow();
      var createCalls = 0;
      var uploadCalls = 0;
      final first = await flow.saveAndUpload(
        userId: 'user-a',
        images: [draftImage],
        saveText: () async {
          createCalls++;
          return _details(42);
        },
        uploadImages: (_, _) async {
          uploadCalls++;
          throw StateError('offline');
        },
      );

      expect(first.isPartialSuccess, isTrue);
      expect(flow.continueWithoutImages()!.id, 42);
      expect(createCalls, 1);
      expect(uploadCalls, 1);
      expect(flow.stage, MemoryImageSaveStage.complete);
    },
  );

  test('edit retry does not repeat the successful update request', () async {
    final flow = MemoryImageSaveFlow();
    var updateCalls = 0;
    var uploadCalls = 0;
    final uploadIds = <int>[];

    Future<MemoryDetails?> update() async {
      updateCalls++;
      return _details(77, title: 'Đã chỉnh sửa');
    }

    Future<MemoryDetails> upload(
      int memoryId,
      List<SelectedMemoryImage> images,
    ) async {
      uploadCalls++;
      uploadIds.add(memoryId);
      if (uploadCalls == 1) throw StateError('offline');
      return _details(
        memoryId,
        title: 'Đã chỉnh sửa',
        images: [_imageMetadata],
      );
    }

    final first = await flow.saveAndUpload(
      userId: 'user-a',
      images: [draftImage],
      saveText: update,
      uploadImages: upload,
    );
    final retry = await flow.saveAndUpload(
      userId: 'user-a',
      images: [draftImage],
      saveText: update,
      uploadImages: upload,
    );

    expect(first.isPartialSuccess, isTrue);
    expect(retry.isComplete, isTrue);
    expect(updateCalls, 1);
    expect(uploadCalls, 2);
    expect(uploadIds, [77, 77]);
    expect(retry.details!.title, 'Đã chỉnh sửa');
  });

  test(
    'ordinary text save failure never becomes image partial-success',
    () async {
      final flow = MemoryImageSaveFlow();
      var uploadCalls = 0;
      final result = await flow.saveAndUpload(
        userId: 'user-a',
        images: [draftImage],
        saveText: () async => null,
        uploadImages: (_, _) async {
          uploadCalls++;
          return _details(99);
        },
      );

      expect(result.stage, MemoryImageSaveStage.failure);
      expect(result.isPartialSuccess, isFalse);
      expect(uploadCalls, 0);
    },
  );

  test('a duplicate retry while uploading is ignored', () async {
    final flow = MemoryImageSaveFlow();
    final uploadCompleter = Completer<MemoryDetails>();
    var createCalls = 0;
    var uploadCalls = 0;

    final first = flow.saveAndUpload(
      userId: 'user-a',
      images: [draftImage],
      saveText: () async {
        createCalls++;
        return _details(88);
      },
      uploadImages: (_, _) {
        uploadCalls++;
        return uploadCompleter.future;
      },
    );
    final duplicate = await flow.saveAndUpload(
      userId: 'user-a',
      images: [draftImage],
      saveText: () async {
        createCalls++;
        return _details(89);
      },
      uploadImages: (_, _) async => _details(89),
    );
    uploadCompleter.complete(_details(88, images: [_imageMetadata]));

    expect(duplicate.stage, MemoryImageSaveStage.failure);
    expect((await first).isComplete, isTrue);
    expect(createCalls, 1);
    expect(uploadCalls, 1);
  });

  test('account change clears retained Memory ID before a new save', () async {
    final flow = MemoryImageSaveFlow();
    await flow.saveAndUpload(
      userId: 'user-a',
      images: [draftImage],
      saveText: () async => _details(55),
      uploadImages: (_, _) async => throw StateError('offline'),
    );
    expect(flow.savedDetails!.id, 55);

    var createCalls = 0;
    final result = await flow.saveAndUpload(
      userId: 'user-b',
      images: const [],
      saveText: () async {
        createCalls++;
        return _details(56);
      },
      uploadImages: (_, _) async => _details(56),
    );

    expect(result.isComplete, isTrue);
    expect(result.details!.id, 56);
    expect(createCalls, 1);
  });
}

final _imageMetadata = MemoryImageMetadata(
  id: 701,
  originalFileName: 'qa-photo.jpg',
  uploadedAt: DateTime.utc(2026, 7, 16),
  contentUrl: '/api/v1/images/701/content',
);

MemoryDetails _details(
  int id, {
  String title = 'Kỷ niệm QA',
  List<MemoryImageMetadata> images = const [],
}) => MemoryDetails(
  id: id,
  title: title,
  story: 'Nội dung đã lưu.',
  feeling: 'Bình yên',
  memoryDate: DateTime.utc(2026, 7, 16),
  location: 'Đà Lạt',
  tags: const ['qa'],
  images: images,
  createdAt: DateTime.utc(2026, 7, 16),
  updatedAt: DateTime.utc(2026, 7, 16),
);
