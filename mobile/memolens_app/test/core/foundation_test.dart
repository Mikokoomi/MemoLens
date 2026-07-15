import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/core/config/app_config.dart';
import 'package:memolens_app/core/network/api_exception.dart';
import 'package:memolens_app/core/storage/secure_token_storage.dart';

void main() {
  test('AppConfig uses a valid Android emulator fallback', () {
    final config = AppConfig.fromEnvironment();

    expect(config.apiBaseUrl, 'http://10.0.2.2:5296');
    expect(config.hasValidApiBaseUrl, isTrue);
  });

  test('AppConfig validates configured base URLs', () {
    expect(
      const AppConfig(apiBaseUrl: 'https://example.test').hasValidApiBaseUrl,
      isTrue,
    );
    expect(
      const AppConfig(apiBaseUrl: 'not-a-url').hasValidApiBaseUrl,
      isFalse,
    );
  });

  test('ApiException maps common HTTP statuses to safe categories', () {
    expect(ApiException.fromStatusCode(401).type, ApiErrorType.unauthorized);
    expect(ApiException.fromStatusCode(404).type, ApiErrorType.notFound);
    expect(ApiException.fromStatusCode(500).type, ApiErrorType.server);
    expect(ApiException.fromStatusCode(418).type, ApiErrorType.unknown);
  });

  test(
    'Token storage abstraction can be replaced without platform storage',
    () async {
      final storage = _FakeTokenStorage();
      await storage.saveAccessToken('access');
      await storage.saveRefreshToken('refresh');

      expect(await storage.readAccessToken(), 'access');
      expect(await storage.readRefreshToken(), 'refresh');

      await storage.clearTokens();
      expect(await storage.readAccessToken(), isNull);
      expect(await storage.readRefreshToken(), isNull);
    },
  );
}

class _FakeTokenStorage implements TokenStorage {
  String? _access;
  String? _refresh;

  @override
  Future<StoredTokens> readTokens() async =>
      StoredTokens(accessToken: _access, refreshToken: _refresh);

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    _access = accessToken;
    _refresh = refreshToken;
  }

  @override
  Future<void> clearTokens() async {
    _access = null;
    _refresh = null;
  }

  @override
  Future<void> deleteAccessToken() async => _access = null;

  @override
  Future<void> deleteRefreshToken() async => _refresh = null;

  @override
  Future<String?> readAccessToken() async => _access;

  @override
  Future<String?> readRefreshToken() async => _refresh;

  @override
  Future<void> saveAccessToken(String token) async => _access = token;

  @override
  Future<void> saveRefreshToken(String token) async => _refresh = token;
}
