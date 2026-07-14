import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../../core/widgets/primary_button.dart';
import '../../../core/widgets/secondary_button.dart';
import '../application/auth_controller.dart';

class EmailConfirmationPage extends ConsumerWidget {
  const EmailConfirmationPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(authControllerProvider);
    final email = state.pendingEmail ?? '';

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
                  const Icon(Icons.mark_email_unread_outlined, size: 46),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    'Xác nhận email của bạn',
                    style: Theme.of(context).textTheme.headlineSmall,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    email.isEmpty
                        ? 'Hãy mở email xác nhận từ MemoLens, sau đó quay lại đăng nhập.'
                        : 'MemoLens đã gửi hướng dẫn tới $email. Link hiện mở trang xác nhận của ứng dụng web; sau khi xác nhận, hãy quay lại đây để đăng nhập.',
                    style: Theme.of(context).textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                  if (state.message != null) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      state.message!,
                      style: Theme.of(context).textTheme.bodySmall,
                      textAlign: TextAlign.center,
                    ),
                  ],
                  const SizedBox(height: AppSpacing.lg),
                  if (email.isNotEmpty)
                    SecondaryButton(
                      label: state.isBusy
                          ? 'Đang gửi lại...'
                          : 'Gửi lại email xác nhận',
                      icon: Icons.refresh_rounded,
                      onPressed: state.isBusy
                          ? null
                          : ref
                                .read(authControllerProvider.notifier)
                                .resendConfirmationEmail,
                    ),
                  const SizedBox(height: AppSpacing.sm),
                  PrimaryButton(
                    label: 'Quay lại đăng nhập',
                    onPressed: state.isBusy
                        ? null
                        : ref.read(authControllerProvider.notifier).showLogin,
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
