import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/app/providers.dart';
import 'package:memolens_app/features/albums/application/album_controllers.dart';
import 'package:memolens_app/features/albums/presentation/album_pages.dart';
import 'package:memolens_app/features/authentication/application/auth_controller.dart';
import 'package:memolens_app/features/authentication/data/auth_repository.dart';
import 'package:memolens_app/features/home/presentation/authenticated_shell.dart';
import 'package:memolens_app/features/memories/presentation/memory_pages.dart';
import 'package:memolens_app/features/memories/presentation/timeline_page.dart';
import 'package:memolens_app/features/memories/application/memory_controllers.dart';
import 'package:memolens_app/features/memories/data/memory_image_repository.dart';

import '../helpers/album_fakes.dart';
import '../helpers/auth_fakes.dart';
import '../helpers/memory_fakes.dart';

void main() {
  testWidgets('shell exposes Timeline, Album and Settings destinations', (
    tester,
  ) async {
    final harness = _ShellHarness();
    addTearDown(harness.dispose);
    await _pump(tester, harness);

    expect(find.byType(TimelinePage), findsOneWidget);
    expect(find.byType(NavigationDestination), findsNWidgets(3));

    await _selectTab(tester, 1);
    expect(find.byType(AlbumsPage), findsOneWidget);

    await _selectTab(tester, 2);
    expect(find.textContaining('Phase 19G'), findsOneWidget);
    expect(find.text('Thùng rác'), findsNothing);
    expect(find.byType(Switch), findsNothing);
  });

  testWidgets(
    'central create action opens Create Memory from every destination',
    (tester) async {
      final harness = _ShellHarness();
      addTearDown(harness.dispose);
      await _pump(tester, harness);

      for (var index = 0; index < 3; index++) {
        await _selectTab(tester, index);
        tester
            .widget<FloatingActionButton>(find.byType(FloatingActionButton))
            .onPressed!();
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 300));
        expect(find.byType(CreateMemoryPage), findsOneWidget);
        await tester.pageBack();
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 300));
        expect(find.byType(AuthenticatedShell), findsOneWidget);
      }
    },
  );

  testWidgets('normal tab switching preserves one shell instance', (
    tester,
  ) async {
    final harness = _ShellHarness();
    addTearDown(harness.dispose);
    await _pump(tester, harness);

    await _selectTab(tester, 1);
    await _selectTab(tester, 0);

    expect(find.byType(AuthenticatedShell), findsOneWidget);
    expect(find.byType(TimelinePage), findsOneWidget);
  });
}

Future<void> _selectTab(WidgetTester tester, int index) async {
  tester
      .widget<NavigationBar>(find.byType(NavigationBar))
      .onDestinationSelected!(index);
  await tester.pump();
}

Future<void> _pump(WidgetTester tester, _ShellHarness harness) async {
  await harness.container
      .read(authControllerProvider.notifier)
      .login(email: 'user@example.test', password: 'MemoLens1');
  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: harness.container,
      child: MaterialApp(home: const AuthenticatedShell()),
    ),
  );
  await tester.pump();
}

class _ShellHarness {
  _ShellHarness() {
    authRepository = AuthRepository(api: api, storage: storage);
    container = ProviderContainer(
      overrides: [
        authRepositoryProvider.overrideWithValue(authRepository),
        memoryRepositoryProvider.overrideWithValue(FakeMemoryRepository()),
        memoryImageRepositoryProvider.overrideWithValue(
          FakeMemoryImageRepository(),
        ),
        albumRepositoryProvider.overrideWithValue(FakeAlbumRepository()),
      ],
    );
  }

  final FakeAuthApi api = FakeAuthApi();
  final FakeTokenStorage storage = FakeTokenStorage();
  late final AuthRepository authRepository;
  late final ProviderContainer container;

  Future<void> dispose() async {
    container.dispose();
    await authRepository.dispose();
  }
}
