import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../app/theme/app_colors.dart';
import '../../data/memory_image_repository.dart';

class PrivateMemoryImage extends ConsumerWidget {
  const PrivateMemoryImage({
    required this.imageId,
    this.fit = BoxFit.cover,
    this.onTap,
    super.key,
  });

  final int imageId;
  final BoxFit fit;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final image = ref.watch(privateImageBytesProvider(imageId));
    return InkWell(
      onTap: onTap,
      child: image.when(
        loading: () => const _ImagePlaceholder(icon: Icons.photo_outlined),
        error: (_, _) => _ImagePlaceholder(
          icon: Icons.broken_image_outlined,
          action: () => ref.invalidate(privateImageBytesProvider(imageId)),
        ),
        data: (bytes) => Image.memory(bytes, fit: fit, gaplessPlayback: true),
      ),
    );
  }
}

class LocalMemoryImagePreview extends StatelessWidget {
  const LocalMemoryImagePreview({required this.bytes, super.key});
  final Uint8List bytes;
  @override
  Widget build(BuildContext context) =>
      Image.memory(bytes, fit: BoxFit.cover, gaplessPlayback: true);
}

class _ImagePlaceholder extends StatelessWidget {
  const _ImagePlaceholder({required this.icon, this.action});
  final IconData icon;
  final VoidCallback? action;
  @override
  Widget build(BuildContext context) => ColoredBox(
    color: AppColors.surfaceMuted,
    child: Center(
      child: action == null
          ? Icon(icon, color: AppColors.teal)
          : IconButton(
              onPressed: action,
              icon: Icon(icon, color: AppColors.danger),
              tooltip: 'Tai lai anh',
            ),
    ),
  );
}
