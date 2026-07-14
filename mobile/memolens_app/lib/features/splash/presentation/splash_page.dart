import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/theme/app_colors.dart';
import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/loading_indicator.dart';
import '../../../core/widgets/paper_page.dart';
import '../../authentication/application/auth_controller.dart';
import '../../authentication/application/auth_state.dart';

class SplashPage extends ConsumerStatefulWidget {
  const SplashPage({super.key});

  @override
  ConsumerState<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends ConsumerState<SplashPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        ref.read(authControllerProvider.notifier).initializeSession();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider);
    if (authState.status == AuthStatus.temporarilyUnavailable ||
        authState.status == AuthStatus.failure) {
      return Scaffold(
        body: PaperPage(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.cloud_off_outlined, size: 46),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    'Chưa thể mở MemoLens',
                    style: Theme.of(context).textTheme.headlineSmall,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    authState.message ?? 'Vui lòng thử lại.',
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  FilledButton.icon(
                    onPressed: ref
                        .read(authControllerProvider.notifier)
                        .retryInitialization,
                    icon: const Icon(Icons.refresh_rounded),
                    label: const Text('Thử lại'),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }
    return Scaffold(
      body: PaperPage(
        scrollable: false,
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.menu_book_rounded,
                color: AppColors.teal,
                size: 52,
              ),
              const SizedBox(height: AppSpacing.md),
              Text('MemoLens', style: Theme.of(context).textTheme.displaySmall),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Không gian riêng cho những kỷ niệm của bạn.',
                style: Theme.of(context).textTheme.bodyMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.xl),
              const LoadingIndicator(label: 'Đang chuẩn bị nhật ký của bạn...'),
            ],
          ),
        ),
      ),
    );
  }
}
