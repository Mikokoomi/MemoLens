import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/features/memories/data/models/memory_models.dart';

void main() {
  test(
    'formatMemoryDate preserves the calendar day',
    () => expect(formatMemoryDate(DateTime(2026, 7, 1, 23, 50)), '2026-07-01'),
  );
  test('parseMemoryDate returns date-only value', () {
    final value = parseMemoryDate('2026-07-01T23:30:00Z');
    expect(value, DateTime(2026, 7, 1));
  });
  test('memory query omits empty filters', () {
    final query = const MemoryQuery();
    expect(query.toQueryParameters(), {
      'page': 1,
      'pageSize': 20,
      'sort': 'newest',
    });
  });
  test('memory query serializes supported filters', () {
    final query = MemoryQuery(
      search: ' biển ',
      feeling: 'Bình yên',
      tag: 'du lịch',
      from: DateTime(2026, 7, 1),
      to: DateTime(2026, 7, 3),
      sort: MemorySort.oldest,
    );
    expect(query.toQueryParameters()['from'], '2026-07-01');
    expect(query.toQueryParameters()['sort'], 'oldest');
    expect(query.hasFilters, isTrue);
  });
  test('draft removes blank tag and normalizes optional strings', () {
    final draft = MemoryDraft(
      title: '  Hè  ',
      story: ' ',
      feeling: 'Vui vẻ',
      memoryDate: DateTime(2026, 7, 2),
      location: ' ',
      tags: const [' biển ', '', 'biển'],
    );
    final json = draft.toJson();
    expect(json['title'], 'Hè');
    expect(json['story'], isNull);
    expect(json['location'], isNull);
    expect(json['tags'], ['biển', 'biển']);
  });
  test('list item parses image metadata without content bytes', () {
    final item = MemoryListItem.fromJson({
      'id': 1,
      'title': 'A',
      'feeling': 'Bình yên',
      'memoryDate': '2026-07-01T00:00:00',
      'tags': [],
      'imageCount': 2,
      'createdAt': '2026-07-01T00:00:00Z',
      'updatedAt': '2026-07-01T00:00:00Z',
    });
    expect(item.imageCount, 2);
    expect(item.coverImageId, isNull);
  });
}
