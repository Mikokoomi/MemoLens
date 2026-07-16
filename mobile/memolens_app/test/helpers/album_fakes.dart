import 'dart:async';
import 'dart:typed_data';

import 'package:memolens_app/features/albums/data/album_models.dart';
import 'package:memolens_app/features/albums/data/album_repository.dart';
import 'package:memolens_app/features/memories/data/memory_image_repository.dart';

final sampleAlbum = AlbumListItem(
  id: 41,
  title: 'Mua he rieng tu',
  description: 'Nhung ky niem chi cua minh.',
  memoryCount: 3,
  effectiveCoverImageId: 91,
  createdAt: DateTime.utc(2026, 7, 16),
);

class FakeAlbumRepository implements AlbumRepository {
  AlbumPage result = AlbumPage(items: [sampleAlbum]);
  Object? listError;
  Completer<AlbumPage>? listCompleter;
  int listCalls = 0;
  AlbumDraft? createDraft;
  AlbumDraft? updateDraft;
  Object? saveError;
  Object? detailsError;
  int detailsCalls = 0;
  AlbumDetails detailsResult = AlbumDetails(
    id: 41,
    title: 'Mua he rieng tu',
    description: 'Nhung ky niem chi cua minh.',
    memoryCount: 3,
    effectiveCoverImageId: 91,
    createdAt: DateTime.utc(2026, 7, 16),
    updatedAt: DateTime.utc(2026, 7, 16),
    memories: const [],
  );

  @override
  Future<AlbumPage> list() async {
    listCalls++;
    if (listError case final error?) throw error;
    if (listCompleter case final completer?) return completer.future;
    return result;
  }

  @override
  Future<AlbumDetails> details(int id) async {
    detailsCalls++;
    if (detailsError case final error?) throw error;
    return detailsResult;
  }

  @override
  Future<AlbumDetails> create(AlbumDraft draft) async {
    createDraft = draft;
    if (saveError case final error?) throw error;
    return detailsResult;
  }

  @override
  Future<AlbumDetails> update(int id, AlbumDraft draft) async {
    updateDraft = draft;
    if (saveError case final error?) throw error;
    return AlbumDetails(
      id: id,
      title: draft.title.trim(),
      description: draft.description,
      memoryCount: detailsResult.memoryCount,
      effectiveCoverImageId: detailsResult.effectiveCoverImageId,
      createdAt: detailsResult.createdAt,
      updatedAt: DateTime.utc(2026, 7, 16, 1),
      memories: detailsResult.memories,
    );
  }

  @override
  Future<void> delete(int id) => throw UnimplementedError();
  @override
  Future<AlbumDetails> restore(int id) => throw UnimplementedError();
  @override
  Future<AlbumDetails> addMemories(int id, List<int> memoryIds) =>
      throw UnimplementedError();
  @override
  Future<void> removeMemory(int id, int memoryId) => throw UnimplementedError();
  @override
  Future<void> addMemoryToAlbums(int memoryId, List<int> albumIds) =>
      throw UnimplementedError();
}

class FakeMemoryImageRepository implements MemoryImageRepository {
  Object? loadError;
  final List<int> requestedImageIds = [];

  @override
  Future<Uint8List> loadImageBytes(int imageId) async {
    requestedImageIds.add(imageId);
    if (loadError case final error?) throw error;
    return Uint8List.fromList(const [
      137,
      80,
      78,
      71,
      13,
      10,
      26,
      10,
      0,
      0,
      0,
      13,
      73,
      72,
      68,
      82,
      0,
      0,
      0,
      1,
      0,
      0,
      0,
      1,
      8,
      6,
      0,
      0,
      0,
      31,
      21,
      196,
      137,
      0,
      0,
      0,
      13,
      73,
      68,
      65,
      84,
      8,
      215,
      99,
      248,
      207,
      192,
      240,
      31,
      0,
      5,
      0,
      1,
      255,
      137,
      153,
      61,
      29,
      0,
      0,
      0,
      0,
      73,
      69,
      78,
      68,
      174,
      66,
      96,
      130,
    ]);
  }

  @override
  Future<MemoryImageUploadResult> uploadImages(
    int memoryId,
    List<SelectedMemoryImage> images, {
    void Function(int sent, int total)? onSendProgress,
  }) => throw UnimplementedError();

  @override
  Future<void> deleteImage(int memoryId, int imageId) =>
      throw UnimplementedError();
}
