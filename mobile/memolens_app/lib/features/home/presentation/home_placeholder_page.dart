import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../../core/widgets/primary_button.dart';
import '../../authentication/application/auth_controller.dart';

class HomePlaceholderPage extends ConsumerWidget {
  const HomePlaceholderPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(authControllerProvider);
    final user = state.user;

    return Scaffold(
      appBar: AppBar(title: const Text('MemoLens')),
      body: PaperPage(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 560),
            child: PaperCard(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Không gian ký ức riêng tư',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'Xin chào ${user?.safeDisplayName ?? ''}. Timeline và thao tác với kỷ niệm sẽ đến ở Phase 19C. Trang này không hiển thị dữ liệu mẫu.',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  PrimaryButton(
                    label: state.isBusy ? 'Đang đăng xuất...' : 'Đăng xuất',
                    icon: Icons.logout_rounded,
                    onPressed: state.isBusy
                        ? null
                        : ref.read(authControllerProvider.notifier).logout,
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
