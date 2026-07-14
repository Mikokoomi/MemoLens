import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/config/app_config.dart';
import '../core/network/api_client.dart';
import '../core/storage/secure_token_storage.dart';
import '../features/authentication/data/auth_api.dart';
import '../features/authentication/data/auth_interceptor.dart';
import '../features/authentication/data/auth_repository.dart';

final appConfigProvider = Provider<AppConfig>(
  (ref) => AppConfig.fromEnvironment(),
);

final dioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  return ApiClient.createDio(config);
});

final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(ref.watch(dioProvider)),
);

final secureTokenStorageProvider = Provider<TokenStorage>(
  (ref) => SecureTokenStorage(),
);

final authApiProvider = Provider<AuthApi>(
  (ref) => AuthApi(ref.watch(dioProvider)),
);

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  final repository = AuthRepository(
    api: ref.watch(authApiProvider),
    storage: ref.watch(secureTokenStorageProvider),
  );
  ref.onDispose(repository.dispose);
  return repository;
});

final authenticatedDioProvider = Provider<Dio>((ref) {
  final dio = ApiClient.createDio(ref.watch(appConfigProvider));
  dio.interceptors.add(
    AuthInterceptor(dio: dio, session: ref.watch(authRepositoryProvider)),
  );
  return dio;
});
