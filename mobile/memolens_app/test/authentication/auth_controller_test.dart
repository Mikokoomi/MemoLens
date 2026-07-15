import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/app/providers.dart';
import 'package:memolens_app/core/network/api_exception.dart';
import 'package:memolens_app/core/storage/secure_token_storage.dart';
import 'package:memolens_app/features/authentication/application/auth_controller.dart';
import 'package:memolens_app/features/authentication/application/auth_state.dart';
import 'package:memolens_app/features/authentication/data/auth_api_exception.dart';
import 'package:memolens_app/features/authentication/data/auth_repository.dart';

import '../helpers/auth_fakes.dart';

void main() {
  test('no stored tokens becomes unauthenticated', () async {
    final harness = _Harness();
    addTearDown(harness.dispose);

    await harness.controller.initializeSession();

    expect(harness.state.status, AuthStatus.unauthenticated);
  });

  test('valid stored session becomes authenticated', () async {
    final harness = _Harness()
      ..storage.accessToken = 'access'
      ..storage.refreshToken = 'refresh';
    addTearDown(harness.dispose);

    await harness.controller.initializeSession();

    expect(harness.state.status, AuthStatus.authenticated);
    expect(harness.state.user, testUser);
  });

  test('expired access plus valid refresh becomes authenticated', () async {
    final harness = _Harness()
      ..storage.accessToken = 'expired'
      ..storage.refreshToken = 'refresh';
    harness.api.currentUserError = const AuthApiException(
      ApiErrorType.unauthorized,
      'Hết hạn.',
      reason: AuthFailureReason.invalidSession,
    );
    addTearDown(harness.dispose);

    await harness.controller.initializeSession();

    expect(harness.state.status, AuthStatus.authenticated);
    expect(harness.api.refreshCalls, 1);
  });

  test('invalid refresh becomes unauthenticated', () async {
    final harness = _Harness()
      ..storage.accessToken = 'expired'
      ..storage.refreshToken = 'invalid';
    harness.api
      ..currentUserError = const AuthApiException(
        ApiErrorType.unauthorized,
        'Hết hạn.',
        reason: AuthFailureReason.invalidSession,
      )
      ..refreshError = const AuthApiException(
        ApiErrorType.unauthorized,
        'Refresh không hợp lệ.',
        reason: AuthFailureReason.invalidSession,
      );
    addTearDown(harness.dispose);

    await harness.controller.initializeSession();

    expect(harness.state.status, AuthStatus.unauthenticated);
    expect(harness.storage.accessToken, isNull);
  });

  test('network outage becomes retryable unavailable state', () async {
    final harness = _Harness()
      ..storage.accessToken = 'access'
      ..storage.refreshToken = 'refresh';
    harness.api.currentUserError = const AuthApiException(
      ApiErrorType.unavailable,
      'Mất kết nối.',
      reason: AuthFailureReason.unavailable,
    );
    addTearDown(harness.dispose);

    await harness.controller.initializeSession();

    expect(harness.state.status, AuthStatus.temporarilyUnavailable);
    expect(harness.storage.accessToken, 'access');
  });

  test('secure storage error becomes retryable unavailable state', () async {
    final harness = _Harness()..storage.readTokensError = StateError('storage');
    addTearDown(harness.dispose);

    await harness.controller.initializeSession();

    expect(harness.state.status, AuthStatus.temporarilyUnavailable);
  });

  test(
    'secure storage timeout becomes retryable unavailable state',
    () async {
      final harness = _Harness()
        ..storage.readTokensCompleter = Completer<StoredTokens>();
      addTearDown(harness.dispose);

      await harness.controller.initializeSession();

      expect(harness.state.status, AuthStatus.temporarilyUnavailable);
    },
    timeout: const Timeout(Duration(seconds: 12)),
  );

  test(
    'retry initialization succeeds after temporary storage failure',
    () async {
      final harness = _Harness()
        ..storage.readTokensError = StateError('storage');
      addTearDown(harness.dispose);
      await harness.controller.initializeSession();

      harness.storage
        ..readTokensError = null
        ..accessToken = 'access'
        ..refreshToken = 'refresh';
      await harness.controller.retryInitialization();

      expect(harness.state.status, AuthStatus.authenticated);
    },
  );

  test(
    'session initialization only starts once until an explicit retry',
    () async {
      final harness = _Harness();
      addTearDown(harness.dispose);

      await Future.wait([
        harness.controller.initializeSession(),
        harness.controller.initializeSession(),
      ]);

      expect(harness.storage.readTokensCount, 1);
    },
  );

  test('login success transitions to authenticated', () async {
    final harness = _Harness();
    addTearDown(harness.dispose);

    await harness.controller.login(
      email: 'user@example.test',
      password: 'MemoLens1',
    );

    expect(harness.state.status, AuthStatus.authenticated);
    expect(harness.state.user, testUser);
  });

  test('logout transitions to unauthenticated', () async {
    final harness = _Harness()
      ..storage.accessToken = 'access'
      ..storage.refreshToken = 'refresh';
    addTearDown(harness.dispose);
    await harness.controller.login(
      email: 'user@example.test',
      password: 'MemoLens1',
    );

    await harness.controller.logout();

    expect(harness.state.status, AuthStatus.unauthenticated);
    expect(harness.storage.accessToken, isNull);
  });
}

class _Harness {
  _Harness() {
    repository = AuthRepository(api: api, storage: storage);
    container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
    );
  }

  final FakeAuthApi api = FakeAuthApi();
  final FakeTokenStorage storage = FakeTokenStorage();
  late final AuthRepository repository;
  late final ProviderContainer container;

  AuthController get controller =>
      container.read(authControllerProvider.notifier);
  AuthState get state => container.read(authControllerProvider);

  Future<void> dispose() async {
    container.dispose();
    await repository.dispose();
  }
}
