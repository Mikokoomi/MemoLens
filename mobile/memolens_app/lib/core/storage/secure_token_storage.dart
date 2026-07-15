import 'package:flutter_secure_storage/flutter_secure_storage.dart';

abstract interface class TokenStorage {
  Future<StoredTokens> readTokens();
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

class StoredTokens {
  const StoredTokens({this.accessToken, this.refreshToken});

  final String? accessToken;
  final String? refreshToken;
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage({FlutterSecureStorage? storage})
    : _storage =
          storage ??
          const FlutterSecureStorage(
            aOptions: AndroidOptions(
              // Token migration must be recoverable if Android stops the app.
              migrateWithBackup: true,
            ),
          );

  static const _accessTokenKey = 'memolens.mobile.access_token';
  static const _refreshTokenKey = 'memolens.mobile.refresh_token';
  final FlutterSecureStorage _storage;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    final previousTokens = await readTokens();

    try {
      await saveAccessToken(accessToken);
      await saveRefreshToken(refreshToken);
    } catch (_) {
      await _restoreToken(_accessTokenKey, previousTokens.accessToken);
      await _restoreToken(_refreshTokenKey, previousTokens.refreshToken);
      rethrow;
    }
  }

  @override
  Future<StoredTokens> readTokens() async {
    // A single platform call avoids overlapping cipher-migration work during
    // Android startup and keeps the access/refresh pair consistent.
    final values = await _storage.readAll();
    return StoredTokens(
      accessToken: values[_accessTokenKey],
      refreshToken: values[_refreshTokenKey],
    );
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
