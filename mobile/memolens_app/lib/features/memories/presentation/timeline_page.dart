import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../app/theme/app_colors.dart';
import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/error_view.dart';
import '../../../core/widgets/loading_indicator.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../authentication/application/auth_controller.dart';
import '../application/memory_controllers.dart';
import '../data/models/memory_models.dart';
import 'widgets/private_memory_image.dart';

class TimelinePage extends ConsumerStatefulWidget {
  const TimelinePage({super.key});
  @override
  ConsumerState<TimelinePage> createState() => _TimelinePageState();
}

class _TimelinePageState extends ConsumerState<TimelinePage> {
  final _search = TextEditingController();
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(timelineControllerProvider.notifier).loadInitial(),
    );
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(timelineControllerProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('MemoLens'),
        actions: [
          PopupMenuButton<String>(
            onSelected: (value) {
              if (value == 'logout') {
                ref.read(authControllerProvider.notifier).logout();
              }
            },
            itemBuilder: (_) => const [
              PopupMenuItem(value: 'logout', child: Text('Đăng xuất')),
            ],
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/memories/create'),
        icon: const Icon(Icons.add),
        label: const Text('Tạo kỷ niệm'),
      ),
      body: PaperPage(
        scrollable: false,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Dòng thời gian',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            const SizedBox(height: AppSpacing.xs),
            const Text('Những khoảnh khắc chỉ dành cho bạn.'),
            const SizedBox(height: AppSpacing.md),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _search,
                    onSubmitted: (value) => ref
                        .read(timelineControllerProvider.notifier)
                        .loadInitial(
                          query: state.query.copyWith(
                            search: value,
                            clearSearch: value.trim().isEmpty,
                          ),
                        ),
                    decoration: InputDecoration(
                      hintText: 'Tìm kỷ niệm',
                      prefixIcon: const Icon(Icons.search),
                      suffixIcon: state.query.search?.isNotEmpty == true
                          ? IconButton(
                              icon: const Icon(Icons.clear),
                              onPressed: () {
                                _search.clear();
                                ref
                                    .read(timelineControllerProvider.notifier)
                                    .loadInitial(
                                      query: state.query.copyWith(
                                        clearSearch: true,
                                      ),
                                    );
                              },
                            )
                          : null,
                    ),
                  ),
                ),
                const SizedBox(width: AppSpacing.xs),
                IconButton.filledTonal(
                  onPressed: () => _openFilters(context, state.query),
                  icon: const Icon(Icons.tune),
                  tooltip: 'Bộ lọc',
                ),
              ],
            ),
            if (state.query.hasFilters)
              Padding(
                padding: const EdgeInsets.only(top: AppSpacing.sm),
                child: Wrap(
                  spacing: AppSpacing.xs,
                  children: [
                    InputChip(
                      label: const Text('Đang lọc'),
                      onDeleted: () {
                        _search.clear();
                        ref
                            .read(timelineControllerProvider.notifier)
                            .loadInitial(query: const MemoryQuery());
                      },
                    ),
                  ],
                ),
              ),
            const SizedBox(height: AppSpacing.md),
            Expanded(child: _TimelineContent(state: state)),
          ],
        ),
      ),
    );
  }

  Future<void> _openFilters(BuildContext context, MemoryQuery query) async {
    String? feeling = query.feeling;
    String? tag = query.tag;
    var sort = query.sort;
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) => StatefulBuilder(
        builder: (context, setModalState) => SafeArea(
          child: Padding(
            padding: EdgeInsets.fromLTRB(
              AppSpacing.page,
              AppSpacing.page,
              AppSpacing.page,
              MediaQuery.viewInsetsOf(context).bottom + AppSpacing.page,
            ),
            child: Wrap(
              children: [
                Text('Bộ lọc', style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: AppSpacing.md),
                DropdownButtonFormField<String?>(
                  initialValue: feeling,
                  decoration: const InputDecoration(labelText: 'Cảm xúc'),
                  items: [
                    const DropdownMenuItem(
                      value: null,
                      child: Text('Tất cả cảm xúc'),
                    ),
                    ...memoryFeelings.map(
                      (item) =>
                          DropdownMenuItem(value: item, child: Text(item)),
                    ),
                  ],
                  onChanged: (value) => setModalState(() => feeling = value),
                ),
                const SizedBox(height: AppSpacing.sm),
                TextFormField(
                  initialValue: tag,
                  decoration: const InputDecoration(
                    labelText: 'Thẻ',
                    hintText: 'Ví dụ: du lịch',
                  ),
                  onChanged: (value) => tag = value,
                ),
                const SizedBox(height: AppSpacing.sm),
                SegmentedButton<MemorySort>(
                  segments: const [
                    ButtonSegment(
                      value: MemorySort.newest,
                      label: Text('Mới nhất'),
                    ),
                    ButtonSegment(
                      value: MemorySort.oldest,
                      label: Text('Cũ nhất'),
                    ),
                  ],
                  selected: {sort},
                  onSelectionChanged: (value) =>
                      setModalState(() => sort = value.first),
                ),
                const SizedBox(height: AppSpacing.md),
                FilledButton(
                  onPressed: () {
                    ref
                        .read(timelineControllerProvider.notifier)
                        .loadInitial(
                          query: MemoryQuery(
                            search: query.search,
                            feeling: feeling,
                            tag: tag,
                            from: query.from,
                            to: query.to,
                            year: query.year,
                            month: query.month,
                            sort: sort,
                          ),
                        );
                    Navigator.pop(sheetContext);
                  },
                  child: const Text('Áp dụng'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TimelineContent extends ConsumerWidget {
  const _TimelineContent({required this.state});
  final TimelineState state;
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (state.isLoading && state.items.isEmpty) {
      return const LoadingIndicator(label: 'Đang mở dòng thời gian...');
    }
    if (state.error != null && state.items.isEmpty) {
      return ErrorView(
        title: 'Không thể tải kỷ niệm',
        message: state.error!,
        actionLabel: 'Thử lại',
        onAction: () => ref.read(timelineControllerProvider.notifier).refresh(),
      );
    }
    if (state.items.isEmpty) {
      return RefreshIndicator(
        onRefresh: () =>
            ref.read(timelineControllerProvider.notifier).refresh(),
        child: ListView(
          children: const [
            SizedBox(height: 64),
            PaperCard(
              child: Column(
                children: [
                  Icon(Icons.auto_stories_outlined, size: 38),
                  SizedBox(height: AppSpacing.sm),
                  Text('Chưa có kỷ niệm nào.'),
                  SizedBox(height: AppSpacing.xs),
                  Text(
                    'Hãy lưu lại một khoảnh khắc riêng của bạn.',
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            ),
          ],
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: () => ref.read(timelineControllerProvider.notifier).refresh(),
      child: ListView.separated(
        itemCount: state.items.length + (state.hasNextPage ? 1 : 0),
        separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
        itemBuilder: (context, index) {
          if (index == state.items.length) {
            ref.read(timelineControllerProvider.notifier).loadMore();
            return const Padding(
              padding: EdgeInsets.all(AppSpacing.md),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _MemoryListCard(item: state.items[index]);
        },
      ),
    );
  }
}

class _MemoryListCard extends StatelessWidget {
  const _MemoryListCard({required this.item});
  final MemoryListItem item;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => context.push('/memories/${item.id}'),
    borderRadius: BorderRadius.circular(AppSpacing.radius),
    child: PaperCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _TimelineCover(item: item),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.title,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: AppSpacing.xxs),
                Text(
                  '${formatMemoryDate(item.memoryDate)} • ${item.feeling}',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                if (item.location?.isNotEmpty == true)
                  Padding(
                    padding: const EdgeInsets.only(top: AppSpacing.xxs),
                    child: Text(
                      item.location!,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                if (item.shortStoryPreview?.isNotEmpty == true)
                  Padding(
                    padding: const EdgeInsets.only(top: AppSpacing.xs),
                    child: Text(
                      item.shortStoryPreview!,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ),
                if (item.tags.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: AppSpacing.xs),
                    child: Wrap(
                      spacing: 4,
                      children: item.tags
                          .take(3)
                          .map(
                            (tag) => Chip(
                              label: Text('#$tag'),
                              visualDensity: VisualDensity.compact,
                            ),
                          )
                          .toList(growable: false),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    ),
  );
}

class _TimelineCover extends StatelessWidget {
  const _TimelineCover({required this.item});
  final MemoryListItem item;

  @override
  Widget build(BuildContext context) => Container(
    width: 76,
    height: 76,
    clipBehavior: Clip.antiAlias,
    decoration: BoxDecoration(
      color: AppColors.surfaceMuted,
      borderRadius: BorderRadius.circular(12),
      border: Border.all(color: AppColors.border),
    ),
    child: item.coverImageId == null
        ? Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.photo_library_outlined, color: AppColors.teal),
              Text(
                item.imageCount == 0 ? 'Chưa có ảnh' : '${item.imageCount} ảnh',
                style: Theme.of(context).textTheme.labelSmall,
              ),
            ],
          )
        : PrivateMemoryImage(imageId: item.coverImageId!),
  );
}
