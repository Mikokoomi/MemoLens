import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/config/app_config.dart';
import '../core/network/api_client.dart';
import '../core/storage/secure_token_storage.dart';
import 'router.dart';
import 'theme/app_theme.dart';

final appConfigProvider = Provider<AppConfig>(
  (ref) => AppConfig.fromEnvironment(),
);

final dioProvider = Provider((ref) {
  final config = ref.watch(appConfigProvider);
  return ApiClient.createDio(config);
});

final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(ref.watch(dioProvider)),
);

final secureTokenStorageProvider = Provider<TokenStorage>(
  (ref) => SecureTokenStorage(),
);

class MemoLensApp extends ConsumerWidget {
  const MemoLensApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp.router(
      title: 'MemoLens',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      routerConfig: ref.watch(routerProvider),
    );
  }
}
