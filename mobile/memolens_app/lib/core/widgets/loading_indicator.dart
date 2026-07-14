import 'package:flutter/material.dart';

import '../../app/theme/app_colors.dart';

class LoadingIndicator extends StatelessWidget {
  const LoadingIndicator({super.key, this.label});

  final String? label;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        const SizedBox(
          height: 44,
          width: 44,
          child: CircularProgressIndicator(
            color: AppColors.teal,
            strokeWidth: 3,
          ),
        ),
        if (label != null) ...[
          const SizedBox(height: 16),
          Text(
            label!,
            style: Theme.of(context).textTheme.bodyMedium,
            textAlign: TextAlign.center,
          ),
        ],
      ],
    );
  }
}
