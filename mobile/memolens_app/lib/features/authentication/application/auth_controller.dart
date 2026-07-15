import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/providers.dart';
import '../data/auth_api_exception.dart';
import '../data/auth_repository.dart';
import '../data/models/register_request.dart';
import 'auth_state.dart';

final authControllerProvider = NotifierProvider<AuthController, AuthState>(
  AuthController.new,
);

class AuthController extends Notifier<AuthState> {
  late AuthRepository _repository;
  bool _initializationStarted = false;

  @override
  AuthState build() {
    _repository = ref.watch(authRepositoryProvider);
    final subscription = _repository.sessionExpired.listen((_) {
      state = const AuthState.unauthenticated(
        message: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
      );
    });
    ref.onDispose(subscription.cancel);
    return const AuthState.initializing();
  }

  Future<void> initializeSession({bool force = false}) async {
    if (_initializationStarted && !force) return;
    _initializationStarted = true;
    state = const AuthState.initializing();
    _log('session initialization started');

    try {
      final user = await _repository.initializeSession().timeout(
        const Duration(seconds: 10),
      );
      state = user == null
          ? const AuthState.unauthenticated()
          : AuthState(status: AuthStatus.authenticated, user: user);
      _log('session initialization completed: ${state.status.name}');
    } on TimeoutException {
      state = const AuthState(
        status: AuthStatus.temporarilyUnavailable,
        message:
            'Không thể kiểm tra phiên đăng nhập kịp thời. Vui lòng thử lại.',
      );
      _log('session initialization timed out');
    } on AuthStorageException {
      state = const AuthState(
        status: AuthStatus.temporarilyUnavailable,
        message: 'Chưa thể đọc phiên đăng nhập an toàn. Vui lòng thử lại.',
      );
      _log('secure storage could not be read');
    } on AuthApiException catch (error) {
      if (error.reason == AuthFailureReason.unavailable) {
        state = AuthState(
          status: AuthStatus.temporarilyUnavailable,
          message: error.message,
        );
      } else {
        state = AuthState(status: AuthStatus.failure, message: error.message);
      }
    } catch (_) {
      state = const AuthState(
        status: AuthStatus.failure,
        message: 'Không thể khởi tạo phiên đăng nhập an toàn.',
      );
      _log('session initialization failed safely');
    }
  }

  Future<void> retryInitialization() => initializeSession(force: true);

  Future<void> login({required String email, required String password}) async {
    if (state.isBusy) return;
    state = const AuthState(status: AuthStatus.authenticating);
    try {
      final user = await _repository.login(email: email, password: password);
      state = AuthState(status: AuthStatus.authenticated, user: user);
    } on AuthApiException catch (error) {
      if (error.reason == AuthFailureReason.emailConfirmationRequired) {
        state = AuthState(
          status: AuthStatus.registrationPendingConfirmation,
          pendingEmail: email.trim(),
          message: error.message,
        );
      } else {
        state = AuthState(
          status: AuthStatus.failure,
          message: error.message,
          validationErrors: error.validationErrors,
        );
      }
    } catch (_) {
      state = const AuthState(
        status: AuthStatus.failure,
        message: 'Không thể đăng nhập lúc này. Vui lòng thử lại.',
      );
    }
  }

  Future<void> register(RegisterRequest request) async {
    if (state.isBusy) return;
    state = const AuthState(status: AuthStatus.authenticating);
    try {
      final message = await _repository.register(request);
      state = AuthState(
        status: AuthStatus.registrationPendingConfirmation,
        pendingEmail: request.email.trim(),
        message: message,
      );
    } on AuthApiException catch (error) {
      state = AuthState(
        status: AuthStatus.failure,
        message: error.message,
        validationErrors: error.validationErrors,
      );
    } catch (_) {
      state = const AuthState(
        status: AuthStatus.failure,
        message: 'Không thể tạo tài khoản lúc này. Vui lòng thử lại.',
      );
    }
  }

  Future<void> resendConfirmationEmail() async {
    final email = state.pendingEmail;
    if (email == null || state.isBusy) return;
    state = AuthState(status: AuthStatus.authenticating, pendingEmail: email);
    try {
      final message = await _repository.resendConfirmationEmail(email);
      state = AuthState(
        status: AuthStatus.registrationPendingConfirmation,
        pendingEmail: email,
        message: message,
      );
    } on AuthApiException catch (error) {
      state = AuthState(
        status: AuthStatus.registrationPendingConfirmation,
        pendingEmail: email,
        message: error.message,
      );
    }
  }

  Future<void> logout() async {
    if (state.isBusy) return;
    state = const AuthState(status: AuthStatus.authenticating);
    try {
      await _repository.logout();
    } catch (_) {
      // Local logout is authoritative even when revocation cannot reach server.
    }
    state = const AuthState.unauthenticated();
  }

  void showLogin() => state = const AuthState.unauthenticated();

  void _log(String message) {
    if (kDebugMode) debugPrint('[MemoLens auth] $message');
  }
}
