import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/app.dart';
import '../../../app/theme/app_spacing.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../../core/widgets/primary_button.dart';
import '../../../core/widgets/secondary_button.dart';

class LoginPlaceholderPage extends ConsumerStatefulWidget {
  const LoginPlaceholderPage({super.key});

  @override
  ConsumerState<LoginPlaceholderPage> createState() =>
      _LoginPlaceholderPageState();
}

class _LoginPlaceholderPageState extends ConsumerState<LoginPlaceholderPage> {
  String? _healthMessage;
  bool _checkingHealth = false;

  Future<void> _checkHealth() async {
    setState(() => _checkingHealth = true);
    try {
      final health = await ref.read(apiClientProvider).getHealth();
      if (!mounted) return;
      setState(
        () => _healthMessage = health.success
            ? 'Máy chủ MemoLens đang hoạt động.'
            : health.message,
      );
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() => _healthMessage = error.message);
    } finally {
      if (mounted) setState(() => _checkingHealth = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final config = ref.watch(appConfigProvider);
    return Scaffold(
      body: PaperPage(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: PaperCard(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Icon(Icons.lock_outline_rounded, size: 42),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    'Chào mừng đến MemoLens',
                    style: Theme.of(context).textTheme.headlineSmall,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'Đăng nhập bằng JWT sẽ được triển khai ở Phase 19B. Nhật ký của bạn vẫn riêng tư ngay từ nền tảng.',
                    style: Theme.of(context).textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  const PrimaryButton(
                    label: 'Đăng nhập (sắp có)',
                    onPressed: null,
                  ),
                  if (kDebugMode) ...[
                    const SizedBox(height: AppSpacing.sm),
                    SecondaryButton(
                      label: _checkingHealth
                          ? 'Đang kiểm tra...'
                          : 'Kiểm tra máy chủ phát triển',
                      icon: Icons.wifi_tethering_rounded,
                      onPressed: _checkingHealth || !config.hasValidApiBaseUrl
                          ? null
                          : _checkHealth,
                    ),
                    if (_healthMessage != null) ...[
                      const SizedBox(height: AppSpacing.sm),
                      Text(
                        _healthMessage!,
                        style: Theme.of(context).textTheme.bodyMedium,
                        textAlign: TextAlign.center,
                      ),
                    ],
                  ],
                  if (kDebugMode) ...[
                    const SizedBox(height: AppSpacing.md),
                    Text(
                      'API phát triển: ${config.apiBaseUrl}',
                      style: Theme.of(context).textTheme.bodyMedium,
                      textAlign: TextAlign.center,
                    ),
                  ],
                  const SizedBox(height: AppSpacing.sm),
                  TextButton(
                    onPressed: () => context.go('/home'),
                    child: const Text('Xem trang mẫu tạm thời'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
