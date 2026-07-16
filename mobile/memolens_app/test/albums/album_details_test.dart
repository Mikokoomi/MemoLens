import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/features/albums/application/album_controllers.dart';
import 'package:memolens_app/features/albums/data/album_models.dart';
import 'package:memolens_app/features/albums/presentation/album_pages.dart';
import 'package:memolens_app/features/memories/data/memory_image_repository.dart';

import '../helpers/album_fakes.dart';

void main() {
  testWidgets('Album Details renders empty Album and has Edit only', (
    tester,
  ) async {
    final albums = FakeAlbumRepository();
    await _pump(tester, albums);
    await tester.pumpAndSettle();

    expect(find.text('Mua he rieng tu'), findsOneWidget);
    expect(find.textContaining('3'), findsOneWidget);
    expect(find.textContaining('chưa có kỷ niệm'), findsOneWidget);
    await tester.tap(find.byType(PopupMenuButton<String>));
    await tester.pumpAndSettle();
    expect(find.text('Chỉnh sửa Album'), findsOneWidget);
    expect(find.textContaining('Xóa Album'), findsNothing);
    expect(find.textContaining('Thêm kỷ niệm'), findsNothing);
  });

  testWidgets('Album Details renders active Memory data and private cover', (
    tester,
  ) async {
    final albums = FakeAlbumRepository()
      ..detailsResult = AlbumDetails(
        id: 41,
        title: 'Mua he rieng tu',
        memoryCount: 1,
        effectiveCoverImageId: null,
        createdAt: DateTime.utc(2026, 7, 16),
        updatedAt: DateTime.utc(2026, 7, 16),
        memories: [
          AlbumMemoryItem(
            id: 7,
            title: 'Buoi chieu',
            memoryDate: DateTime(2026, 7, 1),
            feeling: 'Binh yen',
            imageCount: 1,
            coverImageId: 91,
          ),
        ],
      );
    final images = FakeMemoryImageRepository();
    await _pump(tester, albums, images: images);
    await tester.pumpAndSettle();

    expect(find.text('Buoi chieu'), findsOneWidget);
    expect(images.requestedImageIds, [91]);
  });

  testWidgets('Album Details error can Retry', (tester) async {
    final albums = FakeAlbumRepository()..detailsError = StateError('offline');
    await _pump(tester, albums);
    await tester.pumpAndSettle();
    expect(find.text('Không thể tải Album'), findsOneWidget);
    albums.detailsError = null;
    await tester.tap(find.text('Thử lại'));
    await tester.pumpAndSettle();
    expect(find.text('Mua he rieng tu'), findsOneWidget);
    expect(albums.detailsCalls, 2);
  });
}

Future<void> _pump(
  WidgetTester tester,
  FakeAlbumRepository albums, {
  FakeMemoryImageRepository? images,
}) => tester.pumpWidget(
  ProviderScope(
    overrides: [
      albumRepositoryProvider.overrideWithValue(albums),
      if (images != null)
        memoryImageRepositoryProvider.overrideWithValue(images),
    ],
    child: const MaterialApp(home: AlbumDetailsPage(id: 41)),
  ),
);
