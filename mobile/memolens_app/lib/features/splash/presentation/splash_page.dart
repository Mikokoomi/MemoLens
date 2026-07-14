import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../app/theme/app_colors.dart';
import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/loading_indicator.dart';
import '../../../core/widgets/paper_page.dart';

class SplashPage extends StatefulWidget {
  const SplashPage({super.key});

  @override
  State<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends State<SplashPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) context.go('/login');
    });
  }

  @override
  Widget build(BuildContext context) {
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
