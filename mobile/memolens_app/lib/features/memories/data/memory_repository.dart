import 'package:dio/dio.dart';
import '../../../core/network/api_exception.dart';
import 'models/memory_models.dart';

abstract class MemoryRepository {
  Future<MemoryPage> getMemories(MemoryQuery query);
  Future<MemoryDetails> getMemory(int id);
  Future<MemoryDetails> createMemory(MemoryDraft draft);
  Future<MemoryDetails> updateMemory(int id, MemoryDraft draft);
  Future<void> deleteMemory(int id);
  Future<MemoryDetails> restoreMemory(int id);
}

class ApiMemoryRepository implements MemoryRepository {
  ApiMemoryRepository(this._dio);
  final Dio _dio;
  @override
  Future<MemoryPage> getMemories(MemoryQuery query) async =>
      MemoryPage.fromJson(
        await _request(
          () => _dio.get<dynamic>(
            '/api/v1/memories',
            queryParameters: query.toQueryParameters(),
          ),
        ),
      );
  @override
  Future<MemoryDetails> getMemory(int id) async => MemoryDetails.fromJson(
    await _request(() => _dio.get<dynamic>('/api/v1/memories/$id')),
  );
  @override
  Future<MemoryDetails> createMemory(MemoryDraft draft) async =>
      MemoryDetails.fromJson(
        await _request(
          () => _dio.post<dynamic>('/api/v1/memories', data: draft.toJson()),
        ),
      );
  @override
  Future<MemoryDetails> updateMemory(int id, MemoryDraft draft) async =>
      MemoryDetails.fromJson(
        await _request(
          () => _dio.put<dynamic>('/api/v1/memories/$id', data: draft.toJson()),
        ),
      );
  @override
  Future<void> deleteMemory(int id) async {
    await _request(() => _dio.delete<dynamic>('/api/v1/memories/$id'));
  }

  @override
  Future<MemoryDetails> restoreMemory(int id) async => MemoryDetails.fromJson(
    await _request(() => _dio.post<dynamic>('/api/v1/memories/$id/restore')),
  );
  Future<Map<String, dynamic>> _request(
    Future<Response<dynamic>> Function() call,
  ) async {
    try {
      final response = await call();
      final envelope = Map<String, dynamic>.from(response.data as Map);
      if (envelope['success'] != true || envelope['data'] is! Map) {
        throw const ApiException(
          ApiErrorType.malformedResponse,
          'Dữ liệu trả về từ MemoLens chưa hợp lệ.',
        );
      }
      return Map<String, dynamic>.from(envelope['data'] as Map);
    } on DioException catch (error) {
      final body = error.response?.data is Map
          ? Map<String, dynamic>.from(error.response!.data as Map)
          : const <String, dynamic>{};
      final validation = body['errors'] is Map
          ? Map<String, List<String>>.fromEntries(
              (body['errors'] as Map).entries.map(
                (entry) => MapEntry(
                  entry.key.toString(),
                  List<String>.from(entry.value as List),
                ),
              ),
            )
          : const <String, List<String>>{};
      throw MemoryRequestException(
        ApiException.fromStatusCode(error.response?.statusCode),
        message: body['message'] as String?,
        validationErrors: validation,
      );
    }
  }
}

class MemoryRequestException implements Exception {
  const MemoryRequestException(
    this.apiException, {
    this.message,
    this.validationErrors = const {},
  });
  final ApiException apiException;
  final String? message;
  final Map<String, List<String>> validationErrors;
  String get safeMessage =>
      message?.trim().isNotEmpty == true ? message! : apiException.message;
}
