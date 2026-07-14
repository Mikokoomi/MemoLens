import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/core/network/api_exception.dart';
import 'package:memolens_app/features/authentication/data/auth_api_exception.dart';
import 'package:memolens_app/features/authentication/data/auth_repository.dart';

import '../helpers/auth_fakes.dart';

void main() {
  late FakeAuthApi api;
  late FakeTokenStorage storage;
  late AuthRepository repository;

  setUp(() {
    api = FakeAuthApi();
    storage = FakeTokenStorage();
    repository = AuthRepository(api: api, storage: storage);
  });

  tearDown(() => repository.dispose());

  test('successful login stores both tokens as one pair', () async {
    final user = await repository.login(
      email: 'user@example.test',
      password: 'MemoLens1',
    );

    expect(user, testUser);
    expect(storage.accessToken, testTokens.accessToken);
    expect(storage.refreshToken, testTokens.refreshToken);
    expect(storage.savePairCount, 1);
  });

  test('failed login stores nothing', () async {
    api.loginError = const AuthApiException(
      ApiErrorType.unauthorized,
      'Không thể đăng nhập.',
      reason: AuthFailureReason.invalidCredentials,
    );

    await expectLater(
      repository.login(email: 'user@example.test', password: 'bad'),
      throwsA(isA<AuthApiException>()),
    );
    expect(storage.accessToken, isNull);
    expect(storage.refreshToken, isNull);
  });

  test('rotated refresh replaces both stored tokens', () async {
    storage
      ..accessToken = 'old-access'
      ..refreshToken = 'old-refresh';

    final tokens = await repository.refreshTokens();

    expect(tokens, testTokens);
    expect(storage.accessToken, testTokens.accessToken);
    expect(storage.refreshToken, testTokens.refreshToken);
  });

  test('invalid refresh clears tokens', () async {
    storage
      ..accessToken = 'old-access'
      ..refreshToken = 'old-refresh';
    api.refreshError = const AuthApiException(
      ApiErrorType.unauthorized,
      'Phiên hết hạn.',
      reason: AuthFailureReason.invalidSession,
    );

    expect(await repository.refreshTokens(), isNull);
    expect(storage.accessToken, isNull);
    expect(storage.refreshToken, isNull);
  });

  test('backend unavailable initialization keeps stored tokens', () async {
    storage
      ..accessToken = 'stored-access'
      ..refreshToken = 'stored-refresh';
    api.currentUserError = const AuthApiException(
      ApiErrorType.unavailable,
      'Mất kết nối.',
      reason: AuthFailureReason.unavailable,
    );

    await expectLater(
      repository.initializeSession(),
      throwsA(isA<AuthApiException>()),
    );
    expect(storage.accessToken, 'stored-access');
    expect(storage.refreshToken, 'stored-refresh');
    expect(storage.clearCount, 0);
  });

  test('logout clears local tokens even when server fails', () async {
    storage
      ..accessToken = 'stored-access'
      ..refreshToken = 'stored-refresh';
    api.logoutError = const AuthApiException(
      ApiErrorType.unavailable,
      'Mất kết nối.',
      reason: AuthFailureReason.unavailable,
    );

    await expectLater(repository.logout(), throwsA(isA<AuthApiException>()));
    expect(storage.accessToken, isNull);
    expect(storage.refreshToken, isNull);
  });

  test('simultaneous refresh calls share one rotation', () async {
    storage.refreshToken = 'stored-refresh';

    final results = await Future.wait([
      repository.refreshTokens(),
      repository.refreshTokens(),
      repository.refreshTokens(),
    ]);

    expect(results, everyElement(testTokens));
    expect(api.refreshCalls, 1);
  });
}
