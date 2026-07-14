import 'package:dio/dio.dart';

import '../../../core/network/api_exception.dart';
import 'auth_api_exception.dart';
import 'models/auth_tokens.dart';
import 'models/authenticated_user.dart';
import 'models/login_request.dart';
import 'models/register_request.dart';

class AuthApi {
  AuthApi(this._dio);

  final Dio _dio;

  Future<AuthTokens> login(LoginRequest request) async {
    final data = await _postForData(
      '/api/v1/auth/login',
      request.toJson(),
      operation: _AuthOperation.login,
    );
    return _parseTokens(data);
  }

  Future<String> register(RegisterRequest request) {
    return _postForMessage(
      '/api/v1/auth/register',
      request.toJson(),
      operation: _AuthOperation.register,
    );
  }

  Future<String> resendConfirmationEmail(String email) {
    return _postForMessage(
      '/api/v1/auth/resend-confirmation-email',
      {'email': email.trim()},
      operation: _AuthOperation.resendConfirmation,
    );
  }

  Future<AuthTokens> refresh(String refreshToken) async {
    final data = await _postForData('/api/v1/auth/refresh', {
      'refreshToken': refreshToken,
    }, operation: _AuthOperation.refresh);
    return _parseTokens(data);
  }

  Future<void> logout(String refreshToken) async {
    await _postForMessage('/api/v1/auth/logout', {
      'refreshToken': refreshToken,
    }, operation: _AuthOperation.logout);
  }

  Future<AuthenticatedUser> getCurrentUser(String accessToken) async {
    try {
      final response = await _dio.get<Object>(
        '/api/v1/account/me',
        options: Options(headers: {'Authorization': 'Bearer $accessToken'}),
      );
      final envelope = _readEnvelope(response.data);
      final data = envelope['data'];
      if (data is! Map) throw const FormatException();
      return AuthenticatedUser.fromJson(Map<String, dynamic>.from(data));
    } on DioException catch (error) {
      throw _mapError(error, _AuthOperation.currentUser);
    } on FormatException {
      throw _malformedResponse();
    }
  }

  Future<Map<String, dynamic>> _postForData(
    String path,
    Map<String, dynamic> body, {
    required _AuthOperation operation,
  }) async {
    try {
      final response = await _dio.post<Object>(path, data: body);
      final envelope = _readEnvelope(response.data);
      final data = envelope['data'];
      if (data is! Map) throw const FormatException();
      return Map<String, dynamic>.from(data);
    } on DioException catch (error) {
      throw _mapError(error, operation);
    } on FormatException {
      throw _malformedResponse();
    }
  }

  Future<String> _postForMessage(
    String path,
    Map<String, dynamic> body, {
    required _AuthOperation operation,
  }) async {
    try {
      final response = await _dio.post<Object>(path, data: body);
      final envelope = _readEnvelope(response.data);
      return envelope['message'] as String? ?? '';
    } on DioException catch (error) {
      throw _mapError(error, operation);
    } on FormatException {
      throw _malformedResponse();
    }
  }

  Map<String, dynamic> _readEnvelope(Object? body) {
    if (body is! Map) throw const FormatException();
    final envelope = Map<String, dynamic>.from(body);
    if (envelope['success'] is! bool) throw const FormatException();
    return envelope;
  }

  AuthTokens _parseTokens(Map<String, dynamic> data) {
    try {
      return AuthTokens.fromJson(data);
    } on FormatException {
      throw _malformedResponse();
    }
  }

  AuthApiException _mapError(DioException error, _AuthOperation operation) {
    if (error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.sendTimeout ||
        error.type == DioExceptionType.receiveTimeout) {
      return const AuthApiException(
        ApiErrorType.timeout,
        'Kết nối mất quá lâu. Vui lòng thử lại.',
        reason: AuthFailureReason.unavailable,
      );
    }
    if (error.type == DioExceptionType.connectionError ||
        error.response == null) {
      return const AuthApiException(
        ApiErrorType.unavailable,
        'Không thể kết nối tới MemoLens. Vui lòng thử lại.',
        reason: AuthFailureReason.unavailable,
      );
    }

    final statusCode = error.response?.statusCode;
    final body = error.response?.data;
    final envelope = body is Map ? Map<String, dynamic>.from(body) : null;
    final serverMessage = envelope?['message'] as String?;
    final validationErrors = _readValidationErrors(envelope?['errors']);

    if (statusCode == 400 && validationErrors.isNotEmpty) {
      return AuthApiException(
        ApiErrorType.validation,
        serverMessage ?? 'Thông tin chưa hợp lệ. Vui lòng kiểm tra lại.',
        reason: AuthFailureReason.validation,
        statusCode: statusCode,
        validationErrors: validationErrors,
      );
    }

    if (statusCode == 401 && operation == _AuthOperation.login) {
      final confirmationRequired =
          serverMessage?.toLowerCase().contains('xác nhận email') == true;
      return AuthApiException(
        ApiErrorType.unauthorized,
        confirmationRequired
            ? 'Vui lòng xác nhận email trước khi đăng nhập.'
            : 'Email hoặc mật khẩu không đúng.',
        reason: confirmationRequired
            ? AuthFailureReason.emailConfirmationRequired
            : AuthFailureReason.invalidCredentials,
        statusCode: statusCode,
      );
    }

    if (statusCode == 401 &&
        (operation == _AuthOperation.refresh ||
            operation == _AuthOperation.currentUser)) {
      return AuthApiException(
        ApiErrorType.unauthorized,
        'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
        reason: AuthFailureReason.invalidSession,
        statusCode: statusCode,
      );
    }

    final safe = ApiException.fromStatusCode(statusCode);
    return AuthApiException(
      safe.type,
      safe.message,
      reason: statusCode == 400
          ? AuthFailureReason.validation
          : AuthFailureReason.unknown,
      statusCode: statusCode,
      validationErrors: validationErrors,
    );
  }

  Map<String, List<String>> _readValidationErrors(Object? value) {
    if (value is! Map) return const {};
    final result = <String, List<String>>{};
    for (final entry in value.entries) {
      final messages = entry.value;
      if (entry.key is String && messages is List) {
        result[entry.key as String] = messages.whereType<String>().toList();
      }
    }
    return result;
  }

  AuthApiException _malformedResponse() => const AuthApiException(
    ApiErrorType.malformedResponse,
    'Phản hồi từ MemoLens không hợp lệ.',
    reason: AuthFailureReason.malformedResponse,
  );
}

enum _AuthOperation {
  login,
  register,
  refresh,
  logout,
  currentUser,
  resendConfirmation,
}
