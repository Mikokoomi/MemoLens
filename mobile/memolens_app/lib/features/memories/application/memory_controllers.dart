import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../app/providers.dart';
import '../../authentication/application/auth_controller.dart';
import '../data/memory_repository.dart';
import '../data/models/memory_models.dart';

final memoryRepositoryProvider = Provider<MemoryRepository>(
  (ref) => ApiMemoryRepository(ref.watch(authenticatedDioProvider)),
);
final timelineControllerProvider =
    NotifierProvider<TimelineController, TimelineState>(TimelineController.new);
final memoryDetailsControllerProvider =
    NotifierProvider<MemoryDetailsController, MemoryDetailsState>(
      MemoryDetailsController.new,
    );
final memoryFormControllerProvider =
    NotifierProvider<MemoryFormController, MemoryFormState>(
      MemoryFormController.new,
    );

class TimelineState {
  const TimelineState({
    this.query = const MemoryQuery(),
    this.items = const [],
    this.isLoading = false,
    this.isLoadingMore = false,
    this.hasLoaded = false,
    this.hasNextPage = false,
    this.error,
  });
  final MemoryQuery query;
  final List<MemoryListItem> items;
  final bool isLoading;
  final bool isLoadingMore;
  final bool hasLoaded;
  final bool hasNextPage;
  final String? error;
  TimelineState copyWith({
    MemoryQuery? query,
    List<MemoryListItem>? items,
    bool? isLoading,
    bool? isLoadingMore,
    bool? hasLoaded,
    bool? hasNextPage,
    String? error,
    bool clearError = false,
  }) => TimelineState(
    query: query ?? this.query,
    items: items ?? this.items,
    isLoading: isLoading ?? this.isLoading,
    isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    hasLoaded: hasLoaded ?? this.hasLoaded,
    hasNextPage: hasNextPage ?? this.hasNextPage,
    error: clearError ? null : error ?? this.error,
  );
}

class TimelineController extends Notifier<TimelineState> {
  late final MemoryRepository _repository;
  int _version = 0;
  String? _userId;
  @override
  TimelineState build() {
    _repository = ref.watch(memoryRepositoryProvider);
    ref.listen(authControllerProvider, (_, next) {
      if (next.user?.id != _userId) {
        _userId = next.user?.id;
        _version++;
        state = const TimelineState();
      }
    }, fireImmediately: true);
    return const TimelineState();
  }

  Future<void> loadInitial({MemoryQuery? query, bool force = false}) async {
    if (state.isLoading || (!force && state.hasLoaded && query == null)) return;
    final effective = (query ?? state.query).copyWith(page: 1);
    final version = ++_version;
    state = state.copyWith(
      query: effective,
      isLoading: true,
      hasLoaded: true,
      clearError: true,
    );
    try {
      final result = await _repository.getMemories(effective);
      if (version == _version) {
        state = TimelineState(
          query: effective,
          items: result.items,
          hasLoaded: true,
          hasNextPage: result.hasNextPage,
        );
      }
    } on MemoryRequestException catch (error) {
      if (version == _version) {
        state = state.copyWith(isLoading: false, error: error.safeMessage);
      }
    } catch (_) {
      if (version == _version) {
        state = state.copyWith(
          isLoading: false,
          error: 'Không thể tải kỷ niệm lúc này. Vui lòng thử lại.',
        );
      }
    }
  }

  Future<void> refresh() => loadInitial(force: true);
  Future<void> loadMore() async {
    if (state.isLoading || state.isLoadingMore || !state.hasNextPage) return;
    final query = state.query.copyWith(page: state.query.page + 1);
    final version = _version;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final result = await _repository.getMemories(query);
      if (version == _version) {
        state = TimelineState(
          query: query,
          items: [...state.items, ...result.items],
          hasLoaded: true,
          hasNextPage: result.hasNextPage,
        );
      }
    } on MemoryRequestException catch (error) {
      if (version == _version) {
        state = state.copyWith(isLoadingMore: false, error: error.safeMessage);
      }
    } catch (_) {
      if (version == _version) {
        state = state.copyWith(
          isLoadingMore: false,
          error: 'Không thể tải thêm kỷ niệm.',
        );
      }
    }
  }

  void upsert(MemoryDetails details) {
    final item = MemoryListItem.fromDetails(details);
    final index = state.items.indexWhere((entry) => entry.id == item.id);
    final items = [...state.items];
    if (index >= 0) {
      items[index] = item;
    } else {
      items.insert(0, item);
    }
    state = state.copyWith(items: items);
  }

  void remove(int id) => state = state.copyWith(
    items: state.items.where((item) => item.id != id).toList(growable: false),
  );
}

class MemoryDetailsState {
  const MemoryDetailsState({
    this.isLoading = false,
    this.isDeleting = false,
    this.details,
    this.error,
  });
  final bool isLoading;
  final bool isDeleting;
  final MemoryDetails? details;
  final String? error;
  MemoryDetailsState copyWith({
    bool? isLoading,
    bool? isDeleting,
    MemoryDetails? details,
    String? error,
    bool clearError = false,
  }) => MemoryDetailsState(
    isLoading: isLoading ?? this.isLoading,
    isDeleting: isDeleting ?? this.isDeleting,
    details: details ?? this.details,
    error: clearError ? null : error ?? this.error,
  );
}

class MemoryDetailsController extends Notifier<MemoryDetailsState> {
  late final MemoryRepository _repository;
  int _version = 0;
  @override
  MemoryDetailsState build() {
    _repository = ref.watch(memoryRepositoryProvider);
    return const MemoryDetailsState();
  }

  Future<void> load(int id) async {
    final version = ++_version;
    state = const MemoryDetailsState(isLoading: true);
    try {
      final details = await _repository.getMemory(id);
      if (version == _version) state = MemoryDetailsState(details: details);
    } on MemoryRequestException catch (error) {
      if (version == _version) {
        state = MemoryDetailsState(error: error.safeMessage);
      }
    } catch (_) {
      if (version == _version) {
        state = const MemoryDetailsState(
          error: 'Không thể tải kỷ niệm lúc này.',
        );
      }
    }
  }

  void replace(MemoryDetails details) =>
      state = MemoryDetailsState(details: details);
  Future<bool> delete(int id) async {
    if (state.isDeleting) return false;
    state = state.copyWith(isDeleting: true, clearError: true);
    try {
      await _repository.deleteMemory(id);
      state = state.copyWith(isDeleting: false);
      return true;
    } on MemoryRequestException catch (error) {
      state = state.copyWith(isDeleting: false, error: error.safeMessage);
      return false;
    } catch (_) {
      state = state.copyWith(
        isDeleting: false,
        error: 'Không thể chuyển kỷ niệm vào thùng rác.',
      );
      return false;
    }
  }
}

class MemoryFormState {
  const MemoryFormState({
    this.isLoading = false,
    this.isSaving = false,
    this.details,
    this.error,
    this.validationErrors = const {},
  });
  final bool isLoading;
  final bool isSaving;
  final MemoryDetails? details;
  final String? error;
  final Map<String, List<String>> validationErrors;
  MemoryFormState copyWith({
    bool? isLoading,
    bool? isSaving,
    MemoryDetails? details,
    String? error,
    Map<String, List<String>>? validationErrors,
    bool clearError = false,
  }) => MemoryFormState(
    isLoading: isLoading ?? this.isLoading,
    isSaving: isSaving ?? this.isSaving,
    details: details ?? this.details,
    error: clearError ? null : error ?? this.error,
    validationErrors: validationErrors ?? this.validationErrors,
  );
}

class MemoryFormController extends Notifier<MemoryFormState> {
  late final MemoryRepository _repository;
  @override
  MemoryFormState build() {
    _repository = ref.watch(memoryRepositoryProvider);
    return const MemoryFormState();
  }

  Future<void> loadForEdit(int id) async {
    state = const MemoryFormState(isLoading: true);
    try {
      state = MemoryFormState(details: await _repository.getMemory(id));
    } on MemoryRequestException catch (error) {
      state = MemoryFormState(error: error.safeMessage);
    } catch (_) {
      state = const MemoryFormState(
        error: 'Không thể tải kỷ niệm để chỉnh sửa.',
      );
    }
  }

  Future<MemoryDetails?> create(MemoryDraft draft) =>
      _save(() => _repository.createMemory(draft));
  Future<MemoryDetails?> update(int id, MemoryDraft draft) =>
      _save(() => _repository.updateMemory(id, draft));
  Future<MemoryDetails?> _save(
    Future<MemoryDetails> Function() operation,
  ) async {
    if (state.isSaving) return null;
    state = state.copyWith(
      isSaving: true,
      clearError: true,
      validationErrors: const {},
    );
    try {
      final result = await operation();
      state = MemoryFormState(details: result);
      return result;
    } on MemoryRequestException catch (error) {
      state = state.copyWith(
        isSaving: false,
        error: error.safeMessage,
        validationErrors: error.validationErrors,
      );
      return null;
    } catch (_) {
      state = state.copyWith(
        isSaving: false,
        error: 'Không thể lưu kỷ niệm lúc này. Vui lòng thử lại.',
      );
      return null;
    }
  }
}
