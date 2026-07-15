import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../app/theme/app_colors.dart';
import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/error_view.dart';
import '../../../core/widgets/loading_indicator.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../../core/widgets/primary_button.dart';
import '../../../core/widgets/secondary_button.dart';
import '../application/memory_controllers.dart';
import '../data/models/memory_models.dart';

class CreateMemoryPage extends ConsumerWidget {
  const CreateMemoryPage({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(memoryFormControllerProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Tạo kỷ niệm')),
      body: PaperPage(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Lưu một khoảnh khắc',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            const SizedBox(height: AppSpacing.xs),
            const Text('Chỉ bạn mới có thể xem kỷ niệm này.'),
            const SizedBox(height: AppSpacing.lg),
            _MemoryForm(
              initial: null,
              isSaving: state.isSaving,
              error: state.error,
              onSave: (draft) async {
                final details = await ref
                    .read(memoryFormControllerProvider.notifier)
                    .create(draft);
                if (details != null && context.mounted) {
                  ref.read(timelineControllerProvider.notifier).upsert(details);
                  context.go('/memories/${details.id}');
                }
              },
            ),
          ],
        ),
      ),
    );
  }
}

class EditMemoryPage extends ConsumerStatefulWidget {
  const EditMemoryPage({required this.id, super.key});
  final int id;
  @override
  ConsumerState<EditMemoryPage> createState() => _EditMemoryPageState();
}

class _EditMemoryPageState extends ConsumerState<EditMemoryPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref
          .read(memoryFormControllerProvider.notifier)
          .loadForEdit(widget.id),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(memoryFormControllerProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Chỉnh sửa kỷ niệm')),
      body: PaperPage(
        child: state.isLoading
            ? const LoadingIndicator(label: 'Đang mở kỷ niệm...')
            : state.details == null
            ? ErrorView(
                title: 'Không thể mở kỷ niệm',
                message: state.error ?? 'Kỷ niệm này không còn khả dụng.',
                actionLabel: 'Quay lại',
                onAction: () => context.pop(),
              )
            : _MemoryForm(
                initial: MemoryDraft.fromDetails(state.details!),
                isSaving: state.isSaving,
                label: 'Lưu thay đổi',
                error: state.error,
                onSave: (draft) async {
                  final details = await ref
                      .read(memoryFormControllerProvider.notifier)
                      .update(widget.id, draft);
                  if (details != null && context.mounted) {
                    ref
                        .read(timelineControllerProvider.notifier)
                        .upsert(details);
                    ref
                        .read(memoryDetailsControllerProvider.notifier)
                        .replace(details);
                    context.go('/memories/${details.id}');
                  }
                },
              ),
      ),
    );
  }
}

class MemoryDetailsPage extends ConsumerStatefulWidget {
  const MemoryDetailsPage({required this.id, super.key});
  final int id;
  @override
  ConsumerState<MemoryDetailsPage> createState() => _MemoryDetailsPageState();
}

class _MemoryDetailsPageState extends ConsumerState<MemoryDetailsPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(
      () => ref.read(memoryDetailsControllerProvider.notifier).load(widget.id),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(memoryDetailsControllerProvider);
    if (state.isLoading) {
      return const Scaffold(
        body: LoadingIndicator(label: 'Đang mở kỷ niệm...'),
      );
    }
    if (state.details == null) {
      return Scaffold(
        body: ErrorView(
          title: 'Không tìm thấy kỷ niệm',
          message: state.error ?? 'Kỷ niệm này không còn khả dụng.',
          actionLabel: 'Về dòng thời gian',
          onAction: () => context.go('/home'),
        ),
      );
    }
    final details = state.details!;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Kỷ niệm'),
        actions: [
          IconButton(
            onPressed: () => context.push('/memories/${details.id}/edit'),
            icon: const Icon(Icons.edit_outlined),
            tooltip: 'Chỉnh sửa',
          ),
        ],
      ),
      body: PaperPage(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              details.title,
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            const SizedBox(height: AppSpacing.sm),
            Wrap(
              spacing: AppSpacing.xs,
              runSpacing: AppSpacing.xs,
              children: [
                Chip(label: Text(formatMemoryDate(details.memoryDate))),
                Chip(label: Text(details.feeling)),
                if (details.location?.isNotEmpty == true)
                  Chip(label: Text(details.location!)),
              ],
            ),
            const SizedBox(height: AppSpacing.md),
            _GallerySummary(images: details.images),
            const SizedBox(height: AppSpacing.md),
            PaperCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Câu chuyện',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    details.story?.isNotEmpty == true
                        ? details.story!
                        : 'Bạn chưa viết câu chuyện cho kỷ niệm này.',
                  ),
                  if (details.tags.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: AppSpacing.md),
                      child: Wrap(
                        spacing: AppSpacing.xs,
                        runSpacing: AppSpacing.xs,
                        children: details.tags
                            .map((tag) => Chip(label: Text('#$tag')))
                            .toList(growable: false),
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
            Row(
              children: [
                Expanded(
                  child: SecondaryButton(
                    label: 'Sửa',
                    onPressed: () =>
                        context.push('/memories/${details.id}/edit'),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: state.isDeleting
                        ? null
                        : () => _confirmDelete(context, details.id),
                    icon: const Icon(Icons.delete_outline),
                    label: Text(state.isDeleting ? 'Đang chuyển...' : 'Xóa'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppColors.danger,
                    ),
                  ),
                ),
              ],
            ),
            if (state.error != null)
              Padding(
                padding: const EdgeInsets.only(top: AppSpacing.sm),
                child: Text(
                  state.error!,
                  style: const TextStyle(color: AppColors.danger),
                ),
              ),
            const SizedBox(height: AppSpacing.xl),
          ],
        ),
      ),
    );
  }

  Future<void> _confirmDelete(BuildContext context, int id) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Chuyển vào thùng rác?'),
        content: const Text('Kỷ niệm sẽ được ẩn khỏi dòng thời gian của bạn.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: const Text('Chuyển vào thùng rác'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    final deleted = await ref
        .read(memoryDetailsControllerProvider.notifier)
        .delete(id);
    if (deleted && context.mounted) {
      ref.read(timelineControllerProvider.notifier).remove(id);
      context.go('/home');
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Đã chuyển kỷ niệm vào thùng rác.')),
      );
    }
  }
}

class _GallerySummary extends StatelessWidget {
  const _GallerySummary({required this.images});
  final List<MemoryImageMetadata> images;
  @override
  Widget build(BuildContext context) => PaperCard(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Ảnh kèm theo', style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: AppSpacing.sm),
        if (images.isEmpty)
          const Text('Chưa có ảnh trong kỷ niệm này.')
        else
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.sm,
            children: images
                .map(
                  (image) => Container(
                    width: 82,
                    height: 82,
                    decoration: BoxDecoration(
                      color: AppColors.surfaceMuted,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: const Icon(
                      Icons.photo_outlined,
                      color: AppColors.teal,
                    ),
                  ),
                )
                .toList(growable: false),
          ),
        const SizedBox(height: AppSpacing.xs),
        Text(
          images.isEmpty
              ? 'Bạn có thể thêm ảnh khi chỉnh sửa. Tải ảnh sẽ được bổ sung ở giai đoạn sau.'
              : '${images.length} ảnh đã được lưu. Ảnh sẽ được hiển thị trong giai đoạn sau.',
          style: Theme.of(context).textTheme.bodySmall,
        ),
      ],
    ),
  );
}

class _MemoryForm extends StatefulWidget {
  const _MemoryForm({
    required this.initial,
    required this.isSaving,
    required this.onSave,
    this.label = 'Lưu kỷ niệm',
    this.error,
  });
  final MemoryDraft? initial;
  final bool isSaving;
  final Future<void> Function(MemoryDraft) onSave;
  final String label;
  final String? error;
  @override
  State<_MemoryForm> createState() => _MemoryFormState();
}

class _MemoryFormState extends State<_MemoryForm> {
  final _key = GlobalKey<FormState>();
  late final TextEditingController _title;
  late final TextEditingController _story;
  late final TextEditingController _location;
  late final TextEditingController _tags;
  late String _feeling;
  late DateTime _date;
  @override
  void initState() {
    super.initState();
    final d = widget.initial;
    _title = TextEditingController(text: d?.title ?? '');
    _story = TextEditingController(text: d?.story ?? '');
    _location = TextEditingController(text: d?.location ?? '');
    _tags = TextEditingController(text: d?.tags.join(', ') ?? '');
    _feeling = d?.feeling ?? memoryFeelings.first;
    _date = d?.memoryDate ?? DateTime.now();
  }

  @override
  void dispose() {
    _title.dispose();
    _story.dispose();
    _location.dispose();
    _tags.dispose();
    super.dispose();
  }

  Future<void> _datePicker() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(1900),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      helpText: 'Chọn ngày kỷ niệm',
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _submit() async {
    if (!_key.currentState!.validate()) return;
    final tags = <String>{};
    for (final tag in _tags.text.split(',')) {
      if (tag.trim().isNotEmpty) tags.add(tag.trim());
    }
    await widget.onSave(
      MemoryDraft(
        title: _title.text,
        story: _story.text,
        feeling: _feeling,
        memoryDate: _date,
        location: _location.text,
        tags: tags.toList(growable: false),
      ),
    );
  }

  @override
  Widget build(BuildContext context) => Form(
    key: _key,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (widget.error != null)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.md),
            child: Text(
              widget.error!,
              style: const TextStyle(color: AppColors.danger),
            ),
          ),
        PaperCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Câu chuyện', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: AppSpacing.md),
              TextFormField(
                controller: _title,
                maxLength: 120,
                decoration: const InputDecoration(
                  labelText: 'Tiêu đề',
                  hintText: 'Đặt tên cho khoảnh khắc này',
                ),
                validator: (value) => value == null || value.trim().isEmpty
                    ? 'Vui lòng nhập tiêu đề.'
                    : null,
              ),
              const SizedBox(height: AppSpacing.sm),
              TextFormField(
                controller: _story,
                minLines: 6,
                maxLines: 8,
                maxLength: 4000,
                decoration: const InputDecoration(
                  labelText: 'Câu chuyện',
                  hintText: 'Điều gì làm bạn muốn lưu lại hôm nay?',
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        PaperCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Thời gian và cảm xúc',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: AppSpacing.md),
              InkWell(
                onTap: _datePicker,
                borderRadius: BorderRadius.circular(AppSpacing.radius),
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Ngày kỷ niệm',
                    prefixIcon: Icon(Icons.calendar_today_outlined),
                  ),
                  child: Text(formatMemoryDate(_date)),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              DropdownButtonFormField<String>(
                initialValue: _feeling,
                decoration: const InputDecoration(
                  labelText: 'Cảm xúc',
                  prefixIcon: Icon(Icons.sentiment_satisfied_alt_outlined),
                ),
                items: memoryFeelings
                    .map(
                      (item) =>
                          DropdownMenuItem(value: item, child: Text(item)),
                    )
                    .toList(growable: false),
                onChanged: widget.isSaving
                    ? null
                    : (value) => setState(
                        () => _feeling = value ?? memoryFeelings.first,
                      ),
              ),
              const SizedBox(height: AppSpacing.md),
              TextFormField(
                controller: _location,
                maxLength: 200,
                decoration: const InputDecoration(
                  labelText: 'Địa điểm',
                  hintText: 'Ví dụ: Đà Lạt',
                  prefixIcon: Icon(Icons.location_on_outlined),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        PaperCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Thẻ', style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: AppSpacing.xs),
              const Text(
                'Ngăn cách các thẻ bằng dấu phẩy để dễ tìm lại sau này.',
              ),
              const SizedBox(height: AppSpacing.md),
              TextFormField(
                controller: _tags,
                maxLength: 500,
                decoration: const InputDecoration(
                  labelText: 'Thẻ',
                  hintText: 'du lịch, gia đình, cuối tuần',
                  prefixIcon: Icon(Icons.sell_outlined),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        Row(
          children: [
            Expanded(
              child: SecondaryButton(
                label: 'Hủy',
                onPressed: widget.isSaving
                    ? null
                    : () => Navigator.of(context).maybePop(),
              ),
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: PrimaryButton(
                label: widget.isSaving ? 'Đang lưu...' : widget.label,
                icon: Icons.bookmark_added_outlined,
                onPressed: widget.isSaving ? null : _submit,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.xl),
      ],
    ),
  );
}
