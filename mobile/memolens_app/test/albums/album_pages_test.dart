import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/features/albums/application/album_controllers.dart';
import 'package:memolens_app/features/albums/data/album_models.dart';
import 'package:memolens_app/features/albums/presentation/album_pages.dart';
import 'package:memolens_app/features/memories/data/memory_image_repository.dart';

import '../helpers/album_fakes.dart';

void main() {
  testWidgets('Album list renders loading, empty, error and retry states', (
    tester,
  ) async {
    final albums = FakeAlbumRepository()..listCompleter = _neverCompletes();
    await _pump(tester, albums: albums);
    await tester.pump();
    expect(find.text('Đang mở bộ sưu tập...'), findsOneWidget);

    albums.listCompleter = null;
    albums.result = const AlbumPage(items: []);
    await tester.pumpWidget(const SizedBox());
    await _pump(tester, albums: albums);
    await tester.pump();
    expect(find.textContaining('Chưa có Album nào'), findsOneWidget);

    albums.listError = StateError('offline');
    await tester.pumpWidget(const SizedBox());
    await _pump(tester, albums: albums);
    await tester.pump();
    expect(find.text('Không thể tải Album'), findsOneWidget);
    expect(find.text('Thử lại'), findsOneWidget);
  });

  testWidgets('Album list renders private Album data and safe placeholders', (
    tester,
  ) async {
    final withoutCover = AlbumListItem(
      id: 42,
      title: 'Khong co anh bia',
      memoryCount: 0,
      createdAt: DateTime.utc(2026, 7, 16),
    );
    final albums = FakeAlbumRepository()
      ..result = AlbumPage(items: [sampleAlbum, withoutCover]);
    final images = FakeMemoryImageRepository();
    await _pump(tester, albums: albums, images: images);
    await tester.pump();
    expect(find.text('Mua he rieng tu'), findsOneWidget);
    expect(find.text('Nhung ky niem chi cua minh.'), findsOneWidget);
    expect(find.textContaining('3'), findsOneWidget);
    expect(find.byIcon(Icons.photo_library_outlined), findsOneWidget);
    expect(images.requestedImageIds, [91]);
    expect(find.byIcon(Icons.delete_outline), findsNothing);
    expect(find.byIcon(Icons.search), findsNothing);
    expect(find.byIcon(Icons.sort), findsNothing);
    expect(find.byIcon(Icons.grid_view), findsNothing);
  });

  testWidgets(
    'failed private cover keeps the session-neutral Album list usable',
    (tester) async {
      final images = FakeMemoryImageRepository()
        ..loadError = StateError('image');
      await _pump(tester, albums: FakeAlbumRepository(), images: images);
      await tester.pump();
      await tester.pump();
      expect(find.byIcon(Icons.broken_image_outlined), findsOneWidget);
      expect(find.text('Mua he rieng tu'), findsOneWidget);
    },
  );

  testWidgets('deferred Album actions do not open unfinished workflows', (
    tester,
  ) async {
    await _pump(
      tester,
      albums: FakeAlbumRepository(),
      images: FakeMemoryImageRepository(),
    );
    await tester.pump();

    await tester.tap(find.byTooltip('Tạo Album'));
    await tester.pump();
    expect(find.textContaining('Checkpoint 2C'), findsOneWidget);
    expect(find.byType(TextFormField), findsNothing);

    await tester.tap(find.text('Mua he rieng tu'));
    await tester.pump();
    expect(find.textContaining('Checkpoint 2C'), findsOneWidget);
  });
}

Completer<AlbumPage> _neverCompletes() => Completer<AlbumPage>();

Future<void> _pump(
  WidgetTester tester, {
  required FakeAlbumRepository albums,
  FakeMemoryImageRepository? images,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        albumRepositoryProvider.overrideWithValue(albums),
        if (images != null)
          memoryImageRepositoryProvider.overrideWithValue(images),
      ],
      child: const MaterialApp(home: AlbumsPage()),
    ),
  );
  await tester.pump();
}
