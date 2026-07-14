import 'package:dio/dio.dart';

import 'auth_repository.dart';

class AuthInterceptor extends Interceptor {
  AuthInterceptor({required Dio dio, required AuthSessionCoordinator session})
    : _dio = dio,
      _session = session;

  static const _retryKey = 'memolens.auth.retried';
  static const _publicPaths = <String>{
    '/api/v1/health',
    '/api/v1/auth/register',
    '/api/v1/auth/login',
    '/api/v1/auth/refresh',
    '/api/v1/auth/logout',
    '/api/v1/auth/confirm-email',
    '/api/v1/auth/resend-confirmation-email',
    '/api/v1/auth/forgot-password',
    '/api/v1/auth/reset-password',
  };

  final Dio _dio;
  final AuthSessionCoordinator _session;

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (!_publicPaths.contains(options.path)) {
      final accessToken = await _session.readAccessToken();
      if (accessToken != null && accessToken.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $accessToken';
      }
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    final request = err.requestOptions;
    final shouldRefresh =
        err.response?.statusCode == 401 &&
        !_publicPaths.contains(request.path) &&
        request.extra[_retryKey] != true;

    if (!shouldRefresh) {
      handler.next(err);
      return;
    }

    try {
      final accessToken = await _session.refreshAccessToken();
      if (accessToken == null) {
        handler.next(err);
        return;
      }

      request.extra[_retryKey] = true;
      request.headers['Authorization'] = 'Bearer $accessToken';
      final response = await _dio.fetch<Object>(request);
      handler.resolve(response);
    } catch (_) {
      handler.next(err);
    }
  }
}
