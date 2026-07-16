import 'package:dio/dio.dart';
import '../../../core/network/api_exception.dart';
import '../../memories/data/memory_repository.dart';
import 'album_models.dart';

abstract class AlbumRepository {
  Future<AlbumPage> list();
  Future<AlbumDetails> details(int id);
  Future<AlbumDetails> create(AlbumDraft draft);
  Future<AlbumDetails> update(int id, AlbumDraft draft);
  Future<void> delete(int id);
  Future<AlbumDetails> restore(int id);
  Future<AlbumDetails> addMemories(int id, List<int> memoryIds);
  Future<void> removeMemory(int id, int memoryId);
  Future<void> addMemoryToAlbums(int memoryId, List<int> albumIds);
}

class ApiAlbumRepository implements AlbumRepository {
  ApiAlbumRepository(this._dio);
  final Dio _dio;
  @override
  Future<AlbumPage> list() async =>
      AlbumPage.fromJson(await _request(() => _dio.get('/api/v1/albums')));
  @override
  Future<AlbumDetails> details(int id) async => AlbumDetails.fromJson(
    await _request(() => _dio.get('/api/v1/albums/$id')),
  );
  @override
  Future<AlbumDetails> create(AlbumDraft draft) async => AlbumDetails.fromJson(
    await _request(() => _dio.post('/api/v1/albums', data: draft.toJson())),
  );
  @override
  Future<AlbumDetails> update(int id, AlbumDraft draft) async =>
      AlbumDetails.fromJson(
        await _request(
          () => _dio.put(
            '/api/v1/albums/$id',
            data: draft.toJson(includeMemories: false),
          ),
        ),
      );
  @override
  Future<void> delete(int id) => _void(() => _dio.delete('/api/v1/albums/$id'));
  @override
  Future<AlbumDetails> restore(int id) async => AlbumDetails.fromJson(
    await _request(() => _dio.post('/api/v1/albums/$id/restore')),
  );
  @override
  Future<AlbumDetails> addMemories(int id, List<int> ids) async =>
      AlbumDetails.fromJson(
        await _request(
          () => _dio.post(
            '/api/v1/albums/$id/memories',
            data: {'memoryIds': ids.toSet().toList()},
          ),
        ),
      );
  @override
  Future<void> removeMemory(int id, int memoryId) =>
      _void(() => _dio.delete('/api/v1/albums/$id/memories/$memoryId'));
  @override
  Future<void> addMemoryToAlbums(int memoryId, List<int> ids) => _void(
    () => _dio.post(
      '/api/v1/memories/$memoryId/albums',
      data: {'albumIds': ids.toSet().toList()},
    ),
  );
  Future<Map<String, dynamic>> _request(
    Future<Response<dynamic>> Function() call,
  ) async {
    try {
      final r = await call();
      final e = Map<String, dynamic>.from(r.data as Map);
      if (e['success'] != true || e['data'] is! Map) {
        throw const ApiException(
          ApiErrorType.malformedResponse,
          'Dữ liệu Album không hợp lệ.',
        );
      }
      return Map<String, dynamic>.from(e['data'] as Map);
    } on DioException catch (e) {
      throw _error(e);
    }
  }

  Future<void> _void(Future<Response<dynamic>> Function() call) async {
    try {
      final r = await call();
      if (Map<String, dynamic>.from(r.data as Map)['success'] != true) {
        throw const ApiException(
          ApiErrorType.malformedResponse,
          'Yêu cầu Album không hợp lệ.',
        );
      }
    } on DioException catch (e) {
      throw _error(e);
    }
  }

  MemoryRequestException _error(DioException error) {
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
    return MemoryRequestException(
      ApiException.fromStatusCode(error.response?.statusCode),
      message: body['message'] as String?,
      validationErrors: validation,
    );
  }
}
