import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../core/widgets/error_view.dart';
import '../features/authentication/application/auth_controller.dart';
import '../features/authentication/application/auth_state.dart';
import '../features/authentication/presentation/email_confirmation_page.dart';
import '../features/authentication/presentation/login_page.dart';
import '../features/authentication/presentation/register_page.dart';
import '../features/memories/presentation/memory_pages.dart';
import '../features/memories/presentation/timeline_page.dart';
import '../features/splash/presentation/splash_page.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final router = GoRouter(
    initialLocation: '/',
    redirect: (context, routeState) {
      final auth = ref.read(authControllerProvider);
      final location = routeState.matchedLocation;
      final isAuthPage =
          location == '/login' ||
          location == '/register' ||
          location == '/confirm-email';

      if (auth.status == AuthStatus.initializing ||
          auth.status == AuthStatus.temporarilyUnavailable) {
        return location == '/' ? null : '/';
      }
      if (auth.isAuthenticated) {
        return location == '/' || isAuthPage ? '/home' : null;
      }
      if (auth.status == AuthStatus.registrationPendingConfirmation) {
        return location == '/confirm-email' ? null : '/confirm-email';
      }
      if (location == '/' || isAuthPage) return null;
      return '/login';
    },
    routes: [
      GoRoute(path: '/', builder: (context, state) => const SplashPage()),
      GoRoute(path: '/login', builder: (context, state) => const LoginPage()),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterPage(),
      ),
      GoRoute(
        path: '/confirm-email',
        builder: (context, state) => const EmailConfirmationPage(),
      ),
      GoRoute(path: '/home', builder: (context, state) => const TimelinePage()),
      GoRoute(
        path: '/memories/create',
        builder: (context, state) => const CreateMemoryPage(),
      ),
      GoRoute(
        path: '/memories/:id/edit',
        builder: (context, state) {
          final id = int.tryParse(state.pathParameters['id'] ?? '');
          return id == null
              ? Scaffold(
                  body: ErrorView(
                    title: 'Kỷ niệm không hợp lệ',
                    message: 'Liên kết kỷ niệm không hợp lệ.',
                    actionLabel: 'Về dòng thời gian',
                    onAction: () => context.go('/home'),
                  ),
                )
              : EditMemoryPage(id: id);
        },
      ),
      GoRoute(
        path: '/memories/:id',
        builder: (context, state) {
          final id = int.tryParse(state.pathParameters['id'] ?? '');
          return id == null
              ? Scaffold(
                  body: ErrorView(
                    title: 'Kỷ niệm không hợp lệ',
                    message: 'Liên kết kỷ niệm không hợp lệ.',
                    actionLabel: 'Về dòng thời gian',
                    onAction: () => context.go('/home'),
                  ),
                )
              : MemoryDetailsPage(id: id);
        },
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
  ref.listen(authControllerProvider, (_, _) => router.refresh());
  ref.onDispose(router.dispose);
  return router;
});
