import 'package:flutter_secure_storage/flutter_secure_storage.dart';

abstract interface class TokenStorage {
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  });
  Future<void> saveAccessToken(String token);
  Future<String?> readAccessToken();
  Future<void> deleteAccessToken();
  Future<void> saveRefreshToken(String token);
  Future<String?> readRefreshToken();
  Future<void> deleteRefreshToken();
  Future<void> clearTokens();
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  static const _accessTokenKey = 'memolens.mobile.access_token';
  static const _refreshTokenKey = 'memolens.mobile.refresh_token';
  final FlutterSecureStorage _storage;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    final previousAccessToken = await readAccessToken();
    final previousRefreshToken = await readRefreshToken();

    try {
      await saveAccessToken(accessToken);
      await saveRefreshToken(refreshToken);
    } catch (_) {
      await _restoreToken(_accessTokenKey, previousAccessToken);
      await _restoreToken(_refreshTokenKey, previousRefreshToken);
      rethrow;
    }
  }

  Future<void> _restoreToken(String key, String? value) {
    return value == null
        ? _storage.delete(key: key)
        : _storage.write(key: key, value: value);
  }

  @override
  Future<void> saveAccessToken(String token) =>
      _storage.write(key: _accessTokenKey, value: token);

  @override
  Future<String?> readAccessToken() => _storage.read(key: _accessTokenKey);

  @override
  Future<void> deleteAccessToken() => _storage.delete(key: _accessTokenKey);

  @override
  Future<void> saveRefreshToken(String token) =>
      _storage.write(key: _refreshTokenKey, value: token);

  @override
  Future<String?> readRefreshToken() => _storage.read(key: _refreshTokenKey);

  @override
  Future<void> deleteRefreshToken() => _storage.delete(key: _refreshTokenKey);

  @override
  Future<void> clearTokens() async {
    await Future.wait([
      _storage.delete(key: _accessTokenKey),
      _storage.delete(key: _refreshTokenKey),
    ]);
  }
}
