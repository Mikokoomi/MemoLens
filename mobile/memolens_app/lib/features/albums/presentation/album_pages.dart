import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/error_view.dart';
import '../../../core/widgets/loading_indicator.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../memories/presentation/widgets/private_memory_image.dart';
import '../application/album_controllers.dart';
import '../data/album_models.dart';

class AlbumsPage extends ConsumerStatefulWidget {
  const AlbumsPage({super.key});
  @override
  ConsumerState<AlbumsPage> createState() => _AlbumsPageState();
}

class _AlbumsPageState extends ConsumerState<AlbumsPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(albumListControllerProvider.notifier).load(),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(albumListControllerProvider);
    Widget content;
    if (state.loading && state.items.isEmpty) {
      content = const LoadingIndicator(label: 'Đang mở bộ sưu tập...');
    } else if (state.error != null && state.items.isEmpty) {
      content = ErrorView(
        title: 'Không thể tải Album',
        message: state.error!,
        actionLabel: 'Thử lại',
        onAction: () =>
            ref.read(albumListControllerProvider.notifier).refresh(),
      );
    } else if (state.items.isEmpty) {
      content = const Center(
        child: PaperCard(
          child: Text(
            'Chưa có Album nào. Tạo Album sẽ được hoàn thiện ở Checkpoint 2C.',
            textAlign: TextAlign.center,
          ),
        ),
      );
    } else {
      content = RefreshIndicator(
        onRefresh: () async =>
            ref.read(albumListControllerProvider.notifier).refresh(),
        child: ListView.separated(
          itemCount: state.items.length,
          separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
          itemBuilder: (_, index) => _AlbumCard(item: state.items[index]),
        ),
      );
    }
    return Scaffold(
      appBar: AppBar(
        title: const Text('Bộ sưu tập'),
        actions: [
          IconButton(
            onPressed: () => _showDeferred(context),
            icon: const Icon(Icons.add),
            tooltip: 'Tạo Album',
          ),
        ],
      ),
      body: PaperPage(scrollable: false, child: content),
    );
  }
}

class _AlbumCard extends StatelessWidget {
  const _AlbumCard({required this.item});
  final AlbumListItem item;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => _showDeferred(context),
    child: PaperCard(
      child: Row(
        children: [
          _Cover(imageId: item.effectiveCoverImageId, size: 64),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.title,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                if (item.description?.isNotEmpty == true)
                  Text(
                    item.description!,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                Text(
                  '${item.memoryCount} kỷ niệm',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
          const Icon(Icons.chevron_right),
        ],
      ),
    ),
  );
}

void _showDeferred(BuildContext context) =>
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Tạo và xem chi tiết Album sẽ được hoàn thiện ở Checkpoint 2C.',
        ),
      ),
    );

class _Cover extends StatelessWidget {
  const _Cover({required this.imageId, required this.size});
  final int? imageId;
  final double size;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: size,
    height: size,
    child: ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: imageId == null
          ? const ColoredBox(
              color: Color(0xffeee6da),
              child: Icon(Icons.photo_library_outlined),
            )
          : PrivateMemoryImage(imageId: imageId!),
    ),
  );
}
