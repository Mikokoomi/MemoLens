import 'dart:async';

import '../../../core/storage/secure_token_storage.dart';
import 'auth_api.dart';
import 'auth_api_exception.dart';
import 'models/auth_tokens.dart';
import 'models/authenticated_user.dart';
import 'models/login_request.dart';
import 'models/register_request.dart';

abstract interface class AuthSessionCoordinator {
  Future<String?> readAccessToken();
  Future<String?> refreshAccessToken();
  Future<void> clearInvalidSession();
}

class AuthStorageException implements Exception {
  const AuthStorageException();
}

class AuthRepository implements AuthSessionCoordinator {
  AuthRepository({required AuthApi api, required TokenStorage storage})
    : _api = api,
      _storage = storage;

  final AuthApi _api;
  final TokenStorage _storage;
  final StreamController<void> _sessionExpiredController =
      StreamController<void>.broadcast();

  AuthenticatedUser? _currentUser;
  Future<AuthTokens?>? _refreshFuture;

  AuthenticatedUser? get currentUser => _currentUser;
  Stream<void> get sessionExpired => _sessionExpiredController.stream;

  Future<AuthenticatedUser?> initializeSession() async {
    final storedTokens = await _readStoredTokens();
    final accessToken = storedTokens.accessToken;
    final refreshToken = storedTokens.refreshToken;

    if (accessToken == null && refreshToken == null) return null;
    if (accessToken == null || refreshToken == null) {
      await _storage.clearTokens();
      return null;
    }

    try {
      _currentUser = await _api.getCurrentUser(accessToken);
      return _currentUser;
    } on AuthApiException catch (error) {
      if (error.reason == AuthFailureReason.unavailable) rethrow;
      if (error.reason != AuthFailureReason.invalidSession) {
        await _storage.clearTokens();
        return null;
      }
    }

    final tokens = await refreshTokens();
    return tokens?.user;
  }

  Future<StoredTokens> _readStoredTokens() async {
    try {
      return await _storage.readTokens();
    } catch (_) {
      throw const AuthStorageException();
    }
  }

  Future<AuthenticatedUser> login({
    required String email,
    required String password,
  }) async {
    final tokens = await _api.login(
      LoginRequest(email: email, password: password),
    );
    await _persistTokens(tokens);
    return tokens.user;
  }

  Future<String> register(RegisterRequest request) => _api.register(request);

  Future<String> resendConfirmationEmail(String email) =>
      _api.resendConfirmationEmail(email);

  Future<void> logout() async {
    final refreshToken = await _storage.readRefreshToken();
    try {
      if (refreshToken != null && refreshToken.isNotEmpty) {
        await _api.logout(refreshToken);
      }
    } finally {
      await _storage.clearTokens();
      _currentUser = null;
    }
  }

  Future<AuthTokens?> refreshTokens() {
    final active = _refreshFuture;
    if (active != null) return active;

    final refresh = _performRefresh();
    _refreshFuture = refresh;
    return refresh.whenComplete(() {
      if (identical(_refreshFuture, refresh)) _refreshFuture = null;
    });
  }

  Future<AuthTokens?> _performRefresh() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) {
      await clearInvalidSession();
      return null;
    }

    try {
      final tokens = await _api.refresh(refreshToken);
      await _persistTokens(tokens);
      return tokens;
    } on AuthApiException catch (error) {
      if (error.reason == AuthFailureReason.unavailable) rethrow;
      await clearInvalidSession();
      return null;
    }
  }

  Future<void> _persistTokens(AuthTokens tokens) async {
    await _storage.saveTokens(
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
    );
    _currentUser = tokens.user;
  }

  @override
  Future<String?> readAccessToken() => _storage.readAccessToken();

  @override
  Future<String?> refreshAccessToken() async =>
      (await refreshTokens())?.accessToken;

  @override
  Future<void> clearInvalidSession() async {
    await _storage.clearTokens();
    _currentUser = null;
    _sessionExpiredController.add(null);
  }

  Future<void> dispose() => _sessionExpiredController.close();
}
