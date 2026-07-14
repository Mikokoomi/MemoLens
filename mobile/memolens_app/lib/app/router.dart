import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../core/widgets/error_view.dart';
import '../features/authentication/presentation/login_placeholder_page.dart';
import '../features/home/presentation/home_placeholder_page.dart';
import '../features/splash/presentation/splash_page.dart';

final routerProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/',
    routes: [
      GoRoute(path: '/', builder: (context, state) => const SplashPage()),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginPlaceholderPage(),
      ),
      GoRoute(
        path: '/home',
        builder: (context, state) => const HomePlaceholderPage(),
      ),
    ],
    errorBuilder: (context, state) => Scaffold(
      body: ErrorView(
        title: 'Không tìm thấy trang này',
        message: 'Trang bạn mở không có trong MemoLens.',
        actionLabel: 'Về trang đăng nhập',
        onAction: () => context.go('/login'),
      ),
    ),
  );
});
