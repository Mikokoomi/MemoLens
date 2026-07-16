import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/app/providers.dart';
import 'package:memolens_app/features/albums/application/album_controllers.dart';
import 'package:memolens_app/features/albums/data/album_models.dart';
import 'package:memolens_app/features/authentication/application/auth_controller.dart';
import 'package:memolens_app/features/authentication/data/auth_repository.dart';

import '../helpers/album_fakes.dart';
import '../helpers/auth_fakes.dart';

void main() {
  test('Album list exposes loading and successful states', () async {
    final harness = _Harness();
    addTearDown(harness.dispose);
    final completer = Completer<AlbumPage>();
    harness.albums.listCompleter = completer;

    final load = harness.controller.load();
    expect(harness.state.loading, isTrue);
    expect(harness.state.loaded, isTrue);

    completer.complete(harness.albums.result);
    await load;
    expect(harness.state.loading, isFalse);
    expect(harness.state.items, [sampleAlbum]);
  });

  test(
    'Album list supports empty, safe error, retry and joined fetches',
    () async {
      final harness = _Harness();
      addTearDown(harness.dispose);
      harness.albums.result = const AlbumPage(items: []);

      await harness.controller.load();
      expect(harness.state.items, isEmpty);

      harness.albums
        ..listError = StateError('offline')
        ..result = AlbumPage(items: [sampleAlbum]);
      await harness.controller.load(force: true);
      expect(harness.state.error, isNotNull);

      harness.albums.listError = null;
      await harness.controller.load(force: true);
      expect(harness.state.items, [sampleAlbum]);
      expect(harness.albums.listCalls, 3);

      final pending = Completer<AlbumPage>();
      harness.albums.listCompleter = pending;
      final first = harness.controller.load(force: true);
      final second = harness.controller.load(force: true);
      expect(harness.albums.listCalls, 4);
      pending.complete(harness.albums.result);
      await Future.wait([first, second]);
    },
  );

  test('logout and account switch clear Album state', () async {
    final harness = _Harness();
    addTearDown(harness.dispose);

    await harness.login('a@example.test');
    await harness.controller.load();
    expect(harness.state.items, isNotEmpty);

    await harness.container.read(authControllerProvider.notifier).logout();
    expect(harness.state.items, isEmpty);
    expect(harness.state.loaded, isFalse);

    await harness.login('b@example.test');
    expect(harness.state.items, isEmpty);
    expect(harness.state.loaded, isFalse);
  });

  test('stale User A response cannot overwrite User B Album state', () async {
    final harness = _Harness();
    addTearDown(harness.dispose);
    final userARequest = Completer<AlbumPage>();
    harness.albums.listCompleter = userARequest;

    await harness.login('a@example.test');
    final userALoad = harness.controller.load();
    await harness.login('b@example.test');
    harness.albums
      ..listCompleter = null
      ..result = const AlbumPage(items: []);
    await harness.controller.load();

    userARequest.complete(AlbumPage(items: [sampleAlbum]));
    await userALoad;
    expect(harness.state.items, isEmpty);
  });
}

class _Harness {
  _Harness() {
    authRepository = AuthRepository(api: api, storage: storage);
    container = ProviderContainer(
      overrides: [
        authRepositoryProvider.overrideWithValue(authRepository),
        albumRepositoryProvider.overrideWithValue(albums),
      ],
    );
  }

  final FakeAuthApi api = FakeAuthApi();
  final FakeTokenStorage storage = FakeTokenStorage();
  final FakeAlbumRepository albums = FakeAlbumRepository();
  late final AuthRepository authRepository;
  late final ProviderContainer container;

  AlbumListController get controller =>
      container.read(albumListControllerProvider.notifier);
  AlbumListState get state => container.read(albumListControllerProvider);

  Future<void> login(String email) async {
    api.loginResult = testTokensFor(email);
    await container
        .read(authControllerProvider.notifier)
        .login(email: email, password: 'MemoLens1');
  }

  Future<void> dispose() async {
    container.dispose();
    await authRepository.dispose();
  }
}
