import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/theme/app_colors.dart';
import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/error_view.dart';
import '../../../core/widgets/loading_indicator.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../memories/data/models/memory_models.dart';
import '../../memories/presentation/widgets/private_memory_image.dart';
import '../../memories/presentation/memory_pages.dart';
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

  Future<void> _openCreate() async {
    final created = await Navigator.of(context).push<AlbumDetails>(
      MaterialPageRoute(builder: (_) => const CreateAlbumPage()),
    );
    if (!mounted || created == null) return;
    ref.read(albumListControllerProvider.notifier).upsert(created);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: const Text('Đã tạo Album'),
        action: SnackBarAction(
          label: 'Xem Album',
          onPressed: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => AlbumDetailsPage(id: created.id)),
          ),
        ),
      ),
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
      content = Center(
        child: PaperCard(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.photo_album_outlined, size: 36),
              const SizedBox(height: AppSpacing.sm),
              const Text('Chưa có Album nào.'),
              const SizedBox(height: AppSpacing.sm),
              FilledButton.icon(
                onPressed: _openCreate,
                icon: const Icon(Icons.add),
                label: const Text('Tạo Album'),
              ),
            ],
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
            onPressed: _openCreate,
            icon: const Icon(Icons.add),
            tooltip: 'Tạo Album',
          ),
        ],
      ),
      body: PaperPage(scrollable: false, child: content),
    );
  }
}

class CreateAlbumPage extends ConsumerStatefulWidget {
  const CreateAlbumPage({super.key});
  @override
  ConsumerState<CreateAlbumPage> createState() => _CreateAlbumPageState();
}

class _CreateAlbumPageState extends ConsumerState<CreateAlbumPage> {
  final _formKey = GlobalKey<FormState>();
  final _title = TextEditingController();
  final _description = TextEditingController();
  var _step = 0;

  @override
  void initState() {
    super.initState();
    final draft = ref.read(albumFormControllerProvider);
    _title.text = draft.title;
    _description.text = draft.description;
  }

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  void _saveInfo() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    ref
        .read(albumFormControllerProvider.notifier)
        .setInfo(title: _title.text, description: _description.text);
  }

  Future<void> _next() async {
    if (_step == 0) {
      _saveInfo();
      if (!(_formKey.currentState?.validate() ?? false)) return;
      await ref.read(albumMemoryPickerControllerProvider.notifier).load();
    }
    if (mounted) setState(() => _step++);
  }

  void _back() {
    if (_step > 0) {
      setState(() => _step--);
    } else {
      ref.read(albumFormControllerProvider.notifier).clear();
      Navigator.of(context).pop();
    }
  }

  Future<void> _submit() async {
    final saved = await ref.read(albumFormControllerProvider.notifier).save();
    if (!mounted || saved == null) return;
    Navigator.of(context).pop(saved);
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(albumFormControllerProvider);
    return PopScope(
      canPop: _step == 0,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && _step > 0) {
          setState(() => _step--);
        }
        if (didPop) {
          ref.read(albumFormControllerProvider.notifier).clear();
        }
      },
      child: Scaffold(
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.arrow_back),
            onPressed: _back,
          ),
          title: const Text('Tạo Album'),
        ),
        body: PaperPage(
          scrollable: false,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Bước ${_step + 1}/3',
                style: Theme.of(context).textTheme.labelLarge,
              ),
              const SizedBox(height: AppSpacing.sm),
              Expanded(child: _stepBody(state)),
              const SizedBox(height: AppSpacing.md),
              _actions(state),
            ],
          ),
        ),
      ),
    );
  }

  Widget _stepBody(AlbumFormState state) {
    switch (_step) {
      case 0:
        return _AlbumInfoForm(
          formKey: _formKey,
          title: _title,
          description: _description,
          validationErrors: state.validationErrors,
          duplicates: _duplicates(),
          onTitleChanged: (_) => setState(() {}),
          error: state.error,
        );
      case 1:
        return const _AlbumMemoryPicker();
      default:
        return _AlbumConfirmation(
          title: state.title,
          description: state.description,
          memoryIds: state.memoryIds,
        );
    }
  }

  List<AlbumListItem> _duplicates() {
    final normalized = _title.text.trim().toLowerCase();
    if (normalized.isEmpty) return const [];
    return ref
        .watch(albumListControllerProvider)
        .items
        .where((album) => album.title.trim().toLowerCase() == normalized)
        .toList(growable: false);
  }

  Widget _actions(AlbumFormState state) => Row(
    children: [
      if (_step > 0)
        Expanded(
          child: OutlinedButton(
            onPressed: state.saving ? null : _back,
            child: const Text('Quay lại'),
          ),
        )
      else
        Expanded(
          child: OutlinedButton(
            onPressed: state.saving ? null : _back,
            child: const Text('Hủy'),
          ),
        ),
      const SizedBox(width: AppSpacing.sm),
      Expanded(
        child: FilledButton(
          onPressed: state.saving ? null : (_step == 2 ? _submit : _next),
          child: Text(
            state.saving
                ? 'Đang lưu...'
                : _step == 2
                ? 'Tạo Album'
                : 'Tiếp tục',
          ),
        ),
      ),
    ],
  );
}

class AlbumDetailsPage extends ConsumerStatefulWidget {
  const AlbumDetailsPage({required this.id, super.key});
  final int id;
  @override
  ConsumerState<AlbumDetailsPage> createState() => _AlbumDetailsPageState();
}

class _AlbumDetailsPageState extends ConsumerState<AlbumDetailsPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(albumDetailsControllerProvider.notifier).load(widget.id),
    );
  }

  Future<void> _edit(AlbumDetails details) async {
    final updated = await Navigator.of(context).push<AlbumDetails>(
      MaterialPageRoute(builder: (_) => EditAlbumPage(details: details)),
    );
    if (!mounted || updated == null) return;
    ref.read(albumListControllerProvider.notifier).upsert(updated);
    ref.read(albumDetailsControllerProvider.notifier).load(widget.id);
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Đã cập nhật Album')));
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(albumDetailsControllerProvider);
    if (state.loading) {
      return const Scaffold(body: LoadingIndicator(label: 'Đang mở Album...'));
    }
    if (state.error != null && state.details == null) {
      return Scaffold(
        appBar: AppBar(),
        body: ErrorView(
          title: 'Không thể tải Album',
          message: state.error!,
          actionLabel: 'Thử lại',
          onAction: () =>
              ref.read(albumDetailsControllerProvider.notifier).load(widget.id),
        ),
      );
    }
    final details = state.details;
    if (details == null) return const SizedBox.shrink();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Chi tiết Album'),
        actions: [
          PopupMenuButton<String>(
            onSelected: (value) {
              if (value == 'edit') _edit(details);
            },
            itemBuilder: (_) => const [
              PopupMenuItem(value: 'edit', child: Text('Chỉnh sửa Album')),
            ],
          ),
        ],
      ),
      body: PaperPage(
        scrollable: false,
        child: ListView(
          children: [
            PaperCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Center(
                    child: _Cover(
                      imageId: details.effectiveCoverImageId,
                      size: 160,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    details.title,
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  if (details.description?.isNotEmpty == true) ...[
                    const SizedBox(height: AppSpacing.xs),
                    Text(details.description!),
                  ],
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    '${details.memoryCount} kỷ niệm',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            if (details.memories.isEmpty)
              const PaperCard(child: Text('Album này chưa có kỷ niệm nào.'))
            else ...[
              Text('Kỷ niệm', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: AppSpacing.sm),
              ...details.memories.map(
                (item) => Padding(
                  padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                  child: _AlbumMemoryCard(item: item),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class EditAlbumPage extends ConsumerStatefulWidget {
  const EditAlbumPage({required this.details, super.key});
  final AlbumDetails details;
  @override
  ConsumerState<EditAlbumPage> createState() => _EditAlbumPageState();
}

class _EditAlbumPageState extends ConsumerState<EditAlbumPage> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _title;
  late final TextEditingController _description;
  @override
  void initState() {
    super.initState();
    _title = TextEditingController(text: widget.details.title);
    _description = TextEditingController(
      text: widget.details.description ?? '',
    );
    Future.microtask(
      () => ref.read(albumFormControllerProvider.notifier).seed(widget.details),
    );
  }

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    ref
        .read(albumFormControllerProvider.notifier)
        .setInfo(title: _title.text, description: _description.text);
    final saved = await ref
        .read(albumFormControllerProvider.notifier)
        .save(id: widget.details.id);
    if (mounted && saved != null) Navigator.of(context).pop(saved);
  }

  void _cancel() {
    ref.read(albumFormControllerProvider.notifier).clear();
    Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(albumFormControllerProvider);
    final duplicate = ref
        .watch(albumListControllerProvider)
        .items
        .any(
          (album) =>
              album.id != widget.details.id &&
              album.title.trim().toLowerCase() ==
                  _title.text.trim().toLowerCase(),
        );
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: _cancel,
        ),
        title: const Text('Chỉnh sửa Album'),
      ),
      body: PaperPage(
        scrollable: false,
        child: Column(
          children: [
            Expanded(
              child: _AlbumInfoForm(
                formKey: _formKey,
                title: _title,
                description: _description,
                validationErrors: state.validationErrors,
                duplicates: duplicate ? const [] : const [],
                showDuplicate: duplicate,
                onTitleChanged: (_) => setState(() {}),
                error: state.error,
              ),
            ),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: state.saving ? null : _cancel,
                    child: const Text('Hủy'),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: FilledButton(
                    onPressed: state.saving ? null : _save,
                    child: Text(state.saving ? 'Đang lưu...' : 'Lưu thay đổi'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _AlbumInfoForm extends StatelessWidget {
  const _AlbumInfoForm({
    required this.formKey,
    required this.title,
    required this.description,
    required this.validationErrors,
    required this.duplicates,
    this.showDuplicate = false,
    this.onTitleChanged,
    this.error,
  });
  final GlobalKey<FormState> formKey;
  final TextEditingController title;
  final TextEditingController description;
  final Map<String, List<String>> validationErrors;
  final List<AlbumListItem> duplicates;
  final bool showDuplicate;
  final ValueChanged<String>? onTitleChanged;
  final String? error;
  @override
  Widget build(BuildContext context) {
    final hasDuplicate = showDuplicate || duplicates.isNotEmpty;
    return Form(
      key: formKey,
      child: ListView(
        children: [
          Text(
            'Thông tin Album',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: AppSpacing.md),
          if (error != null)
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Text(
                error!,
                style: const TextStyle(color: AppColors.danger),
              ),
            ),
          PaperCard(
            child: Column(
              children: [
                TextFormField(
                  controller: title,
                  onChanged: onTitleChanged,
                  maxLength: 100,
                  autofocus: true,
                  decoration: const InputDecoration(labelText: 'Tên Album'),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Vui lòng nhập tên Album.';
                    }
                    return validationErrors['title']?.first;
                  },
                ),
                if (hasDuplicate)
                  const Padding(
                    padding: EdgeInsets.only(top: AppSpacing.xs),
                    child: Text(
                      'Bạn đã có một Album cùng tên. Bạn vẫn có thể tiếp tục.',
                      style: TextStyle(color: AppColors.teal),
                    ),
                  ),
                const SizedBox(height: AppSpacing.sm),
                TextFormField(
                  controller: description,
                  maxLength: 500,
                  minLines: 3,
                  maxLines: 5,
                  decoration: const InputDecoration(
                    labelText: 'Mô tả (không bắt buộc)',
                  ),
                  validator: (_) => validationErrors['description']?.first,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _AlbumMemoryPicker extends ConsumerStatefulWidget {
  const _AlbumMemoryPicker();
  @override
  ConsumerState<_AlbumMemoryPicker> createState() => _AlbumMemoryPickerState();
}

class _AlbumMemoryPickerState extends ConsumerState<_AlbumMemoryPicker> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(albumMemoryPickerControllerProvider.notifier).load(),
    );
  }

  @override
  Widget build(BuildContext context) {
    final picker = ref.watch(albumMemoryPickerControllerProvider);
    final draft = ref.watch(albumFormControllerProvider);
    if (picker.loading && picker.items.isEmpty) {
      return const LoadingIndicator(label: 'Đang tải kỷ niệm...');
    }
    if (picker.error != null && picker.items.isEmpty) {
      return ErrorView(
        title: 'Không thể tải kỷ niệm',
        message: picker.error!,
        actionLabel: 'Thử lại',
        onAction: () => ref
            .read(albumMemoryPickerControllerProvider.notifier)
            .load(force: true),
      );
    }
    if (picker.items.isEmpty) {
      return const Center(
        child: PaperCard(
          child: Text(
            'Chưa có kỷ niệm nào để chọn. Bạn vẫn có thể tạo Album trống.',
          ),
        ),
      );
    }
    return ListView(
      children: [
        Text('Chọn kỷ niệm', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: AppSpacing.xs),
        Text(
          'Đã chọn ${draft.memoryIds.length} kỷ niệm. Bạn có thể bỏ qua bước này.',
        ),
        const SizedBox(height: AppSpacing.md),
        ...picker.items.map(
          (memory) => Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: PaperCard(
              child: CheckboxListTile(
                value: draft.memoryIds.contains(memory.id),
                onChanged: (selected) => ref
                    .read(albumFormControllerProvider.notifier)
                    .setMemorySelected(memory.id, selected ?? false),
                secondary: _Cover(imageId: memory.coverImageId, size: 48),
                title: Text(memory.title),
                subtitle: Text(formatMemoryDate(memory.memoryDate)),
                controlAffinity: ListTileControlAffinity.trailing,
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _AlbumConfirmation extends ConsumerWidget {
  const _AlbumConfirmation({
    required this.title,
    required this.description,
    required this.memoryIds,
  });
  final String title;
  final String description;
  final Set<int> memoryIds;
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final memories = ref
        .watch(albumMemoryPickerControllerProvider)
        .items
        .where((item) => memoryIds.contains(item.id))
        .toList(growable: false);
    return ListView(
      children: [
        Text(
          'Xác nhận Album',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: AppSpacing.md),
        PaperCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: Theme.of(context).textTheme.titleLarge),
              if (description.trim().isNotEmpty) ...[
                const SizedBox(height: AppSpacing.xs),
                Text(description),
              ],
              const SizedBox(height: AppSpacing.sm),
              Text('${memoryIds.length} kỷ niệm đã chọn'),
            ],
          ),
        ),
        if (memories.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.md),
          ...memories.map(
            (memory) => ListTile(
              leading: _Cover(imageId: memory.coverImageId, size: 40),
              title: Text(memory.title),
            ),
          ),
        ],
      ],
    );
  }
}

class _AlbumCard extends StatelessWidget {
  const _AlbumCard({required this.item});
  final AlbumListItem item;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => Navigator.of(
      context,
    ).push(MaterialPageRoute(builder: (_) => AlbumDetailsPage(id: item.id))),
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

class _AlbumMemoryCard extends StatelessWidget {
  const _AlbumMemoryCard({required this.item});
  final AlbumMemoryItem item;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => Navigator.of(
      context,
    ).push(MaterialPageRoute(builder: (_) => MemoryDetailsPage(id: item.id))),
    child: PaperCard(
      child: Row(
        children: [
          _Cover(imageId: item.coverImageId, size: 56),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.title,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                Text(
                  '${formatMemoryDate(item.memoryDate)}${item.feeling.isEmpty ? '' : ' • ${item.feeling}'}',
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
