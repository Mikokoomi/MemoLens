import 'package:flutter/material.dart';

import 'app_colors.dart';

abstract final class AppTypography {
  static const _bodyFamily = 'sans-serif';

  static TextTheme textTheme = const TextTheme(
    displaySmall: TextStyle(
      fontFamily: _bodyFamily,
      fontSize: 34,
      height: 1.15,
      fontWeight: FontWeight.w700,
      color: AppColors.ink,
    ),
    headlineSmall: TextStyle(
      fontFamily: _bodyFamily,
      fontSize: 24,
      height: 1.2,
      fontWeight: FontWeight.w700,
      color: AppColors.ink,
    ),
    titleLarge: TextStyle(
      fontFamily: _bodyFamily,
      fontSize: 20,
      height: 1.25,
      fontWeight: FontWeight.w700,
      color: AppColors.ink,
    ),
    bodyLarge: TextStyle(
      fontFamily: _bodyFamily,
      fontSize: 16,
      height: 1.5,
      color: AppColors.ink,
    ),
    bodyMedium: TextStyle(
      fontFamily: _bodyFamily,
      fontSize: 14,
      height: 1.45,
      color: AppColors.inkMuted,
    ),
    labelLarge: TextStyle(
      fontFamily: _bodyFamily,
      fontSize: 16,
      height: 1.2,
      fontWeight: FontWeight.w700,
    ),
  );
}
