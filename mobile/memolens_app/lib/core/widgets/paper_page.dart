import 'package:flutter/material.dart';

import '../../app/theme/app_colors.dart';
import '../../app/theme/app_spacing.dart';

class PaperPage extends StatelessWidget {
  const PaperPage({required this.child, super.key, this.scrollable = true});

  final Widget child;
  final bool scrollable;

  @override
  Widget build(BuildContext context) {
    final content = SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.page),
        child: child,
      ),
    );
    return Container(
      color: AppColors.paper,
      child: scrollable ? SingleChildScrollView(child: content) : content,
    );
  }
}
