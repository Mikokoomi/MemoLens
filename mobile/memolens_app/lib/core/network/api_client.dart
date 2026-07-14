import 'package:dio/dio.dart';

import '../config/app_config.dart';
import 'api_exception.dart';
import 'api_response.dart';

class ApiClient {
  ApiClient(this._dio);

  final Dio _dio;

  static Dio createDio(AppConfig config) {
    return Dio(
      BaseOptions(
        baseUrl: config.apiBaseUrl,
        connectTimeout: const Duration(seconds: 10),
        sendTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 15),
        headers: const {'Accept': 'application/json'},
        responseType: ResponseType.json,
      ),
    );
  }

  Future<ApiResponse<Map<String, dynamic>>> getHealth() async {
    try {
      final response = await _dio.get<Object>('/api/v1/health');
      final body = response.data;
      if (body is! Map) {
        throw const ApiException(
          ApiErrorType.malformedResponse,
          'Phản hồi từ MemoLens không hợp lệ.',
        );
      }
      return ApiResponse<Map<String, dynamic>>.fromJson(
        Map<String, dynamic>.from(body),
        (value) => Map<String, dynamic>.from(value! as Map),
      );
    } on ApiException {
      rethrow;
    } on DioException catch (error) {
      throw _toApiException(error);
    } on FormatException {
      throw const ApiException(
        ApiErrorType.malformedResponse,
        'Phản hồi từ MemoLens không hợp lệ.',
      );
    }
  }

  ApiException _toApiException(DioException error) {
    if (error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.sendTimeout ||
        error.type == DioExceptionType.receiveTimeout) {
      return const ApiException(
        ApiErrorType.timeout,
        'Kết nối mất quá lâu. Vui lòng thử lại.',
      );
    }
    if (error.type == DioExceptionType.connectionError ||
        error.response == null) {
      return const ApiException(
        ApiErrorType.unavailable,
        'Không thể kết nối tới MemoLens. Hãy kiểm tra máy chủ phát triển.',
      );
    }
    return ApiException.fromStatusCode(error.response?.statusCode);
  }
}
