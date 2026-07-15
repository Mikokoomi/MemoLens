import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/features/memories/data/memory_repository.dart';

void main() {
  test('delete accepts a successful API response without data', () async {
    final dio = Dio()..httpClientAdapter = _DeleteSuccessAdapter();
    final repository = ApiMemoryRepository(dio);

    await expectLater(repository.deleteMemory(42), completes);
  });
}

class _DeleteSuccessAdapter implements HttpClientAdapter {
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<List<int>>? requestStream,
    Future<void>? cancelFuture,
  ) async => ResponseBody.fromString(
    '{"success":true,"message":"Deleted."}',
    200,
    headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    },
  );

  @override
  void close({bool force = false}) {}
}
