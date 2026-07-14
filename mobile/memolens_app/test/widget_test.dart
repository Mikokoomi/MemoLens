import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/app/app.dart';
import 'package:memolens_app/app/router.dart';
import 'package:memolens_app/features/authentication/application/auth_controller.dart';
import 'package:memolens_app/features/authentication/data/auth_repository.dart';
import 'package:memolens_app/features/authentication/presentation/email_confirmation_page.dart';
import 'package:memolens_app/features/authentication/presentation/login_page.dart';
import 'package:memolens_app/features/authentication/presentation/register_page.dart';
import 'package:memolens_app/features/home/presentation/home_placeholder_page.dart';

import 'helpers/auth_fakes.dart';

void main() {
  testWidgets('App starts inside ProviderScope', (tester) async {
    final harness = _WidgetHarness();
    addTearDown(harness.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: harness.container,
        child: const MemoLensApp(),
      ),
    );
    expect(find.byType(MemoLensApp), findsOneWidget);
  });

  for (final width in [360.0, 390.0, 430.0]) {
    testWidgets('Login renders at ${width.toInt()} without overflow', (
      tester,
    ) async {
      await _setSize(tester, width);
      final harness = _WidgetHarness();
      addTearDown(harness.dispose);
      await _pumpPage(tester, harness, const LoginPage());

      expect(find.text('Chào mừng trở lại'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('Register renders without overflow and validates fields', (
    tester,
  ) async {
    await _setSize(tester, 390);
    final harness = _WidgetHarness();
    addTearDown(harness.dispose);
    await _pumpPage(tester, harness, const RegisterPage());

    await tester.tap(find.text('Tạo tài khoản').last);
    await tester.pump();

    expect(find.text('Vui lòng nhập email.'), findsOneWidget);
    expect(find.text('Mật khẩu cần ít nhất 8 ký tự.'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('Login validation and password visibility work', (tester) async {
    final harness = _WidgetHarness();
    addTearDown(harness.dispose);
    await _pumpPage(tester, harness, const LoginPage());

    await tester.tap(find.text('Đăng nhập').last);
    await tester.pump();
    expect(find.text('Vui lòng nhập email.'), findsOneWidget);
    expect(find.text('Vui lòng nhập mật khẩu.'), findsOneWidget);

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Mật khẩu'),
      'MemoLens1',
    );
    expect(
      tester.widget<TextField>(find.byType(TextField).last).obscureText,
      isTrue,
    );
    await tester.tap(find.byTooltip('Hiện mật khẩu'));
    await tester.pump();
    expect(
      tester.widget<TextField>(find.byType(TextField).last).obscureText,
      isFalse,
    );
  });

  testWidgets('duplicate login submits are prevented while loading', (
    tester,
  ) async {
    final harness = _WidgetHarness();
    harness.api.loginCompleter = Completer();
    addTearDown(harness.dispose);
    await _pumpPage(tester, harness, const LoginPage());

    await tester.enterText(
      find.byType(TextFormField).first,
      'user@example.test',
    );
    await tester.enterText(find.byType(TextFormField).last, 'MemoLens1');
    await tester.tap(find.text('Đăng nhập').last);
    await tester.pump();

    expect(harness.api.loginCalls, 1);
    expect(find.text('Đang đăng nhập...'), findsOneWidget);
    await tester.tap(find.text('Đang đăng nhập...'));
    await tester.pump();
    expect(harness.api.loginCalls, 1);
    harness.api.loginCompleter!.complete(testTokens);
    await tester.pump();
  });

  testWidgets('confirmation-required page renders safely', (tester) async {
    final harness = _WidgetHarness();
    addTearDown(harness.dispose);
    await _pumpPage(tester, harness, const EmailConfirmationPage());

    expect(find.text('Xác nhận email của bạn'), findsOneWidget);
    expect(find.text('Quay lại đăng nhập'), findsOneWidget);
  });

  testWidgets('authenticated Home displays safe identity', (tester) async {
    final harness = _WidgetHarness();
    addTearDown(harness.dispose);
    await harness.container
        .read(authControllerProvider.notifier)
        .login(email: 'user@example.test', password: 'MemoLens1');
    await _pumpPage(tester, harness, const HomePlaceholderPage());

    expect(find.textContaining('Người dùng thử'), findsOneWidget);
    expect(find.text('Đăng xuất'), findsOneWidget);
  });

  testWidgets('route guard blocks Home when unauthenticated', (tester) async {
    final harness = _WidgetHarness();
    addTearDown(harness.dispose);
    final router = harness.container.read(routerProvider);
    router.go('/home');

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: harness.container,
        child: const MemoLensApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsOneWidget);
  });

  testWidgets('authenticated user is redirected away from Login', (
    tester,
  ) async {
    final harness = _WidgetHarness()
      ..storage.accessToken = 'access'
      ..storage.refreshToken = 'refresh';
    addTearDown(harness.dispose);
    await harness.container
        .read(authControllerProvider.notifier)
        .initializeSession();
    final router = harness.container.read(routerProvider);
    router.go('/login');

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: harness.container,
        child: const MemoLensApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(HomePlaceholderPage), findsOneWidget);
  });

  testWidgets('logout clears session and returns to Login', (tester) async {
    final harness = _WidgetHarness();
    addTearDown(harness.dispose);
    await harness.container
        .read(authControllerProvider.notifier)
        .login(email: 'user@example.test', password: 'MemoLens1');

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: harness.container,
        child: const MemoLensApp(),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Đăng xuất'));
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsOneWidget);
    expect(harness.storage.accessToken, isNull);
    expect(harness.storage.refreshToken, isNull);
  });
}

Future<void> _setSize(WidgetTester tester, double width) async {
  await tester.binding.setSurfaceSize(Size(width, 800));
  addTearDown(() => tester.binding.setSurfaceSize(null));
}

Future<void> _pumpPage(
  WidgetTester tester,
  _WidgetHarness harness,
  Widget page,
) {
  return tester.pumpWidget(
    UncontrolledProviderScope(
      container: harness.container,
      child: MaterialApp(theme: ThemeData(useMaterial3: true), home: page),
    ),
  );
}

class _WidgetHarness {
  _WidgetHarness() {
    repository = AuthRepository(api: api, storage: storage);
    container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
    );
  }

  final FakeAuthApi api = FakeAuthApi();
  final FakeTokenStorage storage = FakeTokenStorage();
  late final AuthRepository repository;
  late final ProviderContainer container;

  Future<void> dispose() async {
    container.dispose();
    await repository.dispose();
  }
}
