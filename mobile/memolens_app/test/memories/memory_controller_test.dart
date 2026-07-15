import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/features/memories/application/memory_controllers.dart';
import 'package:memolens_app/features/memories/data/models/memory_models.dart';
import '../helpers/memory_fakes.dart';

void main() {
  test(
    'timeline loads, filters, and updates optimistically after create',
    () async {
      final fake = FakeMemoryRepository();
      final container = ProviderContainer(
        overrides: [memoryRepositoryProvider.overrideWithValue(fake)],
      );
      addTearDown(container.dispose);
      final controller = container.read(timelineControllerProvider.notifier);
      await controller.loadInitial();
      expect(container.read(timelineControllerProvider).items.single.id, 7);
      await controller.loadInitial(query: const MemoryQuery(search: 'bình'));
      expect(fake.lastQuery!.search, 'bình');
      controller.upsert(sampleMemory);
      expect(container.read(timelineControllerProvider).items, isNotEmpty);
      controller.remove(7);
      expect(container.read(timelineControllerProvider).items, isEmpty);
    },
  );
  test('form controller creates and updates a memory', () async {
    final fake = FakeMemoryRepository();
    final container = ProviderContainer(
      overrides: [memoryRepositoryProvider.overrideWithValue(fake)],
    );
    addTearDown(container.dispose);
    final controller = container.read(memoryFormControllerProvider.notifier);
    final created = await controller.create(
      MemoryDraft(
        title: 'Mới',
        story: null,
        feeling: 'Vui vẻ',
        memoryDate: DateTime(2026, 7, 2),
        location: null,
        tags: const [],
      ),
    );
    expect(created!.id, 8);
    await controller.loadForEdit(8);
    final updated = await controller.update(
      8,
      MemoryDraft(
        title: 'Đã sửa',
        story: null,
        feeling: 'Buồn',
        memoryDate: DateTime(2026, 7, 3),
        location: null,
        tags: const [],
      ),
    );
    expect(updated!.title, 'Đã sửa');
  });
}
