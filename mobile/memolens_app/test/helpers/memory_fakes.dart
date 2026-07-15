import 'package:memolens_app/features/memories/data/memory_repository.dart';
import 'package:memolens_app/features/memories/data/models/memory_models.dart';

final sampleMemory = MemoryDetails(
  id: 7,
  title: 'Buổi chiều bình yên',
  story: 'Một câu chuyện riêng tư.',
  feeling: 'Bình yên',
  memoryDate: DateTime(2026, 7, 1),
  location: 'Đà Lạt',
  tags: const ['du lịch'],
  images: const [],
  createdAt: DateTime.utc(2026, 7, 1),
  updatedAt: DateTime.utc(2026, 7, 1),
);

class FakeMemoryRepository implements MemoryRepository {
  final List<MemoryListItem> items = [MemoryListItem.fromDetails(sampleMemory)];
  MemoryDetails details = sampleMemory;
  int listCalls = 0;
  MemoryQuery? lastQuery;
  bool fail = false;

  @override
  Future<MemoryPage> getMemories(MemoryQuery query) async {
    listCalls++;
    lastQuery = query;
    if (fail) throw const MemoryRequestExceptionPlaceholder();
    return MemoryPage(
      items: List.of(items),
      page: query.page,
      pageSize: query.pageSize,
      totalItems: items.length,
      totalPages: 1,
      hasNextPage: false,
    );
  }

  @override
  Future<MemoryDetails> getMemory(int id) async {
    if (fail || id != details.id) {
      throw const MemoryRequestExceptionPlaceholder();
    }
    return details;
  }

  @override
  Future<MemoryDetails> createMemory(MemoryDraft draft) async {
    details = MemoryDetails(
      id: 8,
      title: draft.title.trim(),
      story: draft.story,
      feeling: draft.feeling,
      memoryDate: draft.memoryDate,
      location: draft.location,
      tags: draft.tags,
      images: const [],
      createdAt: DateTime.utc(2026, 7, 2),
      updatedAt: DateTime.utc(2026, 7, 2),
    );
    items.insert(0, MemoryListItem.fromDetails(details));
    return details;
  }

  @override
  Future<MemoryDetails> updateMemory(int id, MemoryDraft draft) async {
    if (id != details.id) throw const MemoryRequestExceptionPlaceholder();
    details = MemoryDetails(
      id: id,
      title: draft.title.trim(),
      story: draft.story,
      feeling: draft.feeling,
      memoryDate: draft.memoryDate,
      location: draft.location,
      tags: draft.tags,
      images: details.images,
      createdAt: details.createdAt,
      updatedAt: DateTime.utc(2026, 7, 3),
    );
    items[0] = MemoryListItem.fromDetails(details);
    return details;
  }

  @override
  Future<void> deleteMemory(int id) async {
    items.removeWhere((item) => item.id == id);
  }

  @override
  Future<MemoryDetails> restoreMemory(int id) async => details;
}

class MemoryRequestExceptionPlaceholder implements Exception {
  const MemoryRequestExceptionPlaceholder();
}
