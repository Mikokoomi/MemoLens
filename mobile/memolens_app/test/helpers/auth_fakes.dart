import 'dart:async';

import 'package:dio/dio.dart';
import 'package:memolens_app/core/storage/secure_token_storage.dart';
import 'package:memolens_app/features/authentication/data/auth_api.dart';
import 'package:memolens_app/features/authentication/data/models/auth_tokens.dart';
import 'package:memolens_app/features/authentication/data/models/authenticated_user.dart';
import 'package:memolens_app/features/authentication/data/models/login_request.dart';
import 'package:memolens_app/features/authentication/data/models/register_request.dart';

const testUser = AuthenticatedUser(
  id: 'user-1',
  email: 'user@example.test',
  displayName: 'Người dùng thử',
  roles: ['User'],
);

const testTokens = AuthTokens(
  accessToken: 'access-token',
  refreshToken: 'refresh-token',
  expiresInSeconds: 900,
  tokenType: 'Bearer',
  user: testUser,
);

class FakeTokenStorage implements TokenStorage {
  String? accessToken;
  String? refreshToken;
  int clearCount = 0;
  int savePairCount = 0;
  int readTokensCount = 0;
  Object? readTokensError;
  Completer<StoredTokens>? readTokensCompleter;

  @override
  Future<StoredTokens> readTokens() async {
    readTokensCount++;
    if (readTokensError case final error?) throw error;
    if (readTokensCompleter case final completer?) return completer.future;
    return StoredTokens(accessToken: accessToken, refreshToken: refreshToken);
  }

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
    savePairCount++;
  }

  @override
  Future<void> clearTokens() async {
    accessToken = null;
    refreshToken = null;
    clearCount++;
  }

  @override
  Future<void> deleteAccessToken() async => accessToken = null;

  @override
  Future<void> deleteRefreshToken() async => refreshToken = null;

  @override
  Future<String?> readAccessToken() async => accessToken;

  @override
  Future<String?> readRefreshToken() async => refreshToken;

  @override
  Future<void> saveAccessToken(String token) async => accessToken = token;

  @override
  Future<void> saveRefreshToken(String token) async => refreshToken = token;
}

class FakeAuthApi extends AuthApi {
  FakeAuthApi() : super(Dio());

  AuthTokens loginResult = testTokens;
  AuthTokens refreshResult = testTokens;
  AuthenticatedUser currentUserResult = testUser;
  Object? loginError;
  Object? refreshError;
  Object? currentUserError;
  Object? logoutError;
  Completer<AuthTokens>? loginCompleter;
  int loginCalls = 0;
  int refreshCalls = 0;
  int logoutCalls = 0;
  int registerCalls = 0;

  @override
  Future<AuthTokens> login(LoginRequest request) async {
    loginCalls++;
    if (loginError case final error?) throw error;
    if (loginCompleter case final completer?) return completer.future;
    return loginResult;
  }

  @override
  Future<AuthTokens> refresh(String refreshToken) async {
    refreshCalls++;
    if (refreshError case final error?) throw error;
    return refreshResult;
  }

  @override
  Future<AuthenticatedUser> getCurrentUser(String accessToken) async {
    if (currentUserError case final error?) throw error;
    return currentUserResult;
  }

  @override
  Future<void> logout(String refreshToken) async {
    logoutCalls++;
    if (logoutError case final error?) throw error;
  }

  @override
  Future<String> register(RegisterRequest request) async {
    registerCalls++;
    return 'Đăng ký thành công.';
  }

  @override
  Future<String> resendConfirmationEmail(String email) async =>
      'Đã nhận yêu cầu.';
}
