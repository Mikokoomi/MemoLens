import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/features/authentication/data/auth_interceptor.dart';
import 'package:memolens_app/features/authentication/data/auth_repository.dart';

void main() {
  test('access token is added only to protected requests', () async {
    final session = _FakeSession(accessToken: 'access');
    final adapter = _RecordingAdapter((_) => 200);
    final dio = _createDio(session, adapter);

    await dio.get<Object>('/api/v1/account/me');
    await dio.post<Object>('/api/v1/auth/login');

    expect(adapter.requests[0].headers['Authorization'], 'Bearer access');
    expect(adapter.requests[1].headers['Authorization'], isNull);
  });

  test('private image content request carries the bearer token', () async {
    final session = _FakeSession(accessToken: 'access');
    final adapter = _RecordingAdapter((_) => 200);
    final dio = _createDio(session, adapter);

    await dio.get<Object>('/api/v1/images/42/content');

    expect(adapter.requests.single.headers['Authorization'], 'Bearer access');
  });

  test('expired protected request refreshes and retries once', () async {
    final session = _FakeSession(accessToken: 'old', refreshedToken: 'new');
    final adapter = _RecordingAdapter(
      (request) => request.headers['Authorization'] == 'Bearer new' ? 200 : 401,
    );
    final dio = _createDio(session, adapter);

    final response = await dio.get<Object>('/api/v1/account/me');

    expect(response.statusCode, 200);
    expect(session.refreshCount, 1);
    expect(adapter.requests, hasLength(2));
  });

  test('simultaneous 401 requests share one refresh operation', () async {
    final session = _FakeSession(
      accessToken: 'old',
      refreshedToken: 'new',
      refreshDelay: const Duration(milliseconds: 20),
    );
    final adapter = _RecordingAdapter(
      (request) => request.headers['Authorization'] == 'Bearer new' ? 200 : 401,
    );
    final dio = _createDio(session, adapter);

    final responses = await Future.wait([
      dio.get<Object>('/api/v1/account/me'),
      dio.get<Object>('/api/v1/account/me'),
      dio.get<Object>('/api/v1/account/me'),
    ]);

    expect(responses.map((response) => response.statusCode), everyElement(200));
    expect(session.refreshCount, 1);
  });

  test('simultaneous private image 401 requests share one refresh', () async {
    final session = _FakeSession(
      accessToken: 'old',
      refreshedToken: 'new',
      refreshDelay: const Duration(milliseconds: 20),
    );
    final adapter = _RecordingAdapter(
      (request) => request.headers['Authorization'] == 'Bearer new' ? 200 : 401,
    );
    final dio = _createDio(session, adapter);

    final responses = await Future.wait([
      dio.get<Object>('/api/v1/images/1/content'),
      dio.get<Object>('/api/v1/images/2/content'),
      dio.get<Object>('/api/v1/images/3/content'),
    ]);

    expect(responses.map((response) => response.statusCode), everyElement(200));
    expect(session.refreshCount, 1);
    expect(adapter.requests, hasLength(6));
  });

  test('refresh endpoint 401 never recursively refreshes', () async {
    final session = _FakeSession(accessToken: 'old', refreshedToken: 'new');
    final adapter = _RecordingAdapter((_) => 401);
    final dio = _createDio(session, adapter);

    await expectLater(
      dio.post<Object>('/api/v1/auth/refresh'),
      throwsA(isA<DioException>()),
    );
    expect(session.refreshCount, 0);
    expect(adapter.requests, hasLength(1));
  });

  test('retried request does not enter an infinite refresh loop', () async {
    final session = _FakeSession(accessToken: 'old', refreshedToken: 'new');
    final adapter = _RecordingAdapter((_) => 401);
    final dio = _createDio(session, adapter);

    await expectLater(
      dio.get<Object>('/api/v1/account/me'),
      throwsA(isA<DioException>()),
    );
    expect(session.refreshCount, 1);
    expect(adapter.requests, hasLength(2));
  });

  test(
    'failed refresh returns original 401 after session is cleared',
    () async {
      final session = _FakeSession(accessToken: 'old');
      final adapter = _RecordingAdapter((_) => 401);
      final dio = _createDio(session, adapter);

      await expectLater(
        dio.get<Object>('/api/v1/account/me'),
        throwsA(isA<DioException>()),
      );

      expect(session.refreshCount, 1);
      expect(session.clearCount, 1);
      expect(session.accessToken, isNull);
      expect(adapter.requests, hasLength(1));
    },
  );
}

Dio _createDio(_FakeSession session, HttpClientAdapter adapter) {
  final dio = Dio(BaseOptions(baseUrl: 'https://example.test'));
  dio.httpClientAdapter = adapter;
  dio.interceptors.add(AuthInterceptor(dio: dio, session: session));
  return dio;
}

class _FakeSession implements AuthSessionCoordinator {
  _FakeSession({
    this.accessToken,
    this.refreshedToken,
    this.refreshDelay = Duration.zero,
  });

  String? accessToken;
  final String? refreshedToken;
  final Duration refreshDelay;
  int refreshCount = 0;
  int clearCount = 0;
  Future<String?>? _activeRefresh;

  @override
  Future<String?> readAccessToken() async => accessToken;

  @override
  Future<String?> refreshAccessToken() {
    final active = _activeRefresh;
    if (active != null) return active;
    final future = _refresh();
    _activeRefresh = future;
    return future.whenComplete(() => _activeRefresh = null);
  }

  Future<String?> _refresh() async {
    refreshCount++;
    if (refreshDelay > Duration.zero) await Future<void>.delayed(refreshDelay);
    if (refreshedToken == null) {
      await clearInvalidSession();
      return null;
    }
    accessToken = refreshedToken;
    return refreshedToken;
  }

  @override
  Future<void> clearInvalidSession() async {
    accessToken = null;
    clearCount++;
  }
}

class _RecordingAdapter implements HttpClientAdapter {
  _RecordingAdapter(this.statusForRequest);

  final int Function(RequestOptions request) statusForRequest;
  final List<RequestOptions> requests = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(
      options.copyWith(headers: Map<String, dynamic>.from(options.headers)),
    );
    final statusCode = statusForRequest(options);
    return ResponseBody.fromString(
      jsonEncode({'success': statusCode < 400}),
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
