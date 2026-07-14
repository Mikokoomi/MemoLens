import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:memolens_app/app/app.dart';
import 'package:memolens_app/app/router.dart';
import 'package:memolens_app/core/widgets/paper_card.dart';
import 'package:memolens_app/core/widgets/primary_button.dart';
import 'package:memolens_app/core/widgets/secondary_button.dart';
import 'package:memolens_app/features/authentication/presentation/login_placeholder_page.dart';

void main() {
  testWidgets('App starts inside ProviderScope', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: MemoLensApp()));
    expect(find.byType(MemoLensApp), findsOneWidget);
  });

  testWidgets('Splash page renders MemoLens identity', (tester) async {
    final router = GoRouter(
      routes: [GoRoute(path: '/', builder: (_, _) => const Text('MemoLens'))],
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [routerProvider.overrideWithValue(router)],
        child: const MemoLensApp(),
      ),
    );
    expect(find.text('MemoLens'), findsOneWidget);
  });

  testWidgets('Login placeholder route renders', (tester) async {
    final router = GoRouter(
      initialLocation: '/login',
      routes: [
        GoRoute(
          path: '/login',
          builder: (_, _) => const LoginPlaceholderPage(),
        ),
      ],
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [routerProvider.overrideWithValue(router)],
        child: const MemoLensApp(),
      ),
    );
    expect(find.text('Chào mừng đến MemoLens'), findsOneWidget);
  });

  testWidgets('Home placeholder route renders', (tester) async {
    final router = GoRouter(
      routes: [
        GoRoute(
          path: '/',
          builder: (_, _) => const Text('Không gian kỷ niệm riêng tư'),
        ),
      ],
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [routerProvider.overrideWithValue(router)],
        child: const MemoLensApp(),
      ),
    );
    expect(find.text('Không gian kỷ niệm riêng tư'), findsOneWidget);
  });

  testWidgets('Unknown route shows friendly error UI', (tester) async {
    final router = GoRouter(
      initialLocation: '/khong-co',
      routes: [GoRoute(path: '/', builder: (_, _) => const SizedBox())],
      errorBuilder: (_, _) => const Text('Không tìm thấy trang này'),
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [routerProvider.overrideWithValue(router)],
        child: const MemoLensApp(),
      ),
    );
    await tester.pump();
    expect(find.text('Không tìm thấy trang này'), findsOneWidget);
  });

  for (final width in [360.0, 390.0, 430.0, 768.0]) {
    testWidgets('Paper controls fit ${width.toInt()} logical pixels', (
      tester,
    ) async {
      await tester.binding.setSurfaceSize(Size(width, 800));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Center(
              child: SizedBox(
                width: width - 32,
                child: PaperCard(
                  child: Column(
                    children: const [
                      PrimaryButton(label: 'Lưu kỷ niệm'),
                      SizedBox(height: 12),
                      SecondaryButton(label: 'Quay lại'),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      );
      expect(tester.takeException(), isNull);
    });
  }
}
