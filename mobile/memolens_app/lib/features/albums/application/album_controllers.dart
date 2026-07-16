import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../app/providers.dart';
import '../../authentication/application/auth_controller.dart';
import '../../memories/data/memory_repository.dart';
import '../../memories/data/models/memory_models.dart';
import '../../memories/application/memory_controllers.dart'
    show memoryRepositoryProvider;
import '../data/album_models.dart';
import '../data/album_repository.dart';

final albumRepositoryProvider = Provider<AlbumRepository>(
  (ref) => ApiAlbumRepository(ref.watch(authenticatedDioProvider)),
);
final albumListControllerProvider =
    NotifierProvider<AlbumListController, AlbumListState>(
      AlbumListController.new,
    );
final albumDetailsControllerProvider =
    NotifierProvider<AlbumDetailsController, AlbumDetailsState>(
      AlbumDetailsController.new,
    );
final albumFormControllerProvider =
    NotifierProvider<AlbumFormController, AlbumFormState>(
      AlbumFormController.new,
    );
final albumMemoryPickerControllerProvider =
    NotifierProvider<AlbumMemoryPickerController, AlbumMemoryPickerState>(
      AlbumMemoryPickerController.new,
    );

class AlbumListState {
  const AlbumListState({
    this.items = const [],
    this.loading = false,
    this.loaded = false,
    this.error,
  });
  final List<AlbumListItem> items;
  final bool loading;
  final bool loaded;
  final String? error;
}

class AlbumListController extends Notifier<AlbumListState> {
  late AlbumRepository _repository;
  String? _userId;
  int _version = 0;
  @override
  AlbumListState build() {
    _repository = ref.watch(albumRepositoryProvider);
    ref.listen(authControllerProvider, (_, next) {
      if (_userId != next.user?.id) {
        _userId = next.user?.id;
        _version++;
        state = const AlbumListState();
      }
    }, fireImmediately: true);
    return const AlbumListState();
  }

  Future<void> load({bool force = false}) async {
    if (state.loading || (state.loaded && !force)) return;
    final v = ++_version;
    state = AlbumListState(items: state.items, loading: true, loaded: true);
    try {
      final page = await _repository.list();
      if (v == _version) {
        state = AlbumListState(items: page.items, loaded: true);
      }
    } catch (e) {
      if (v == _version) {
        state = AlbumListState(
          items: state.items,
          loaded: true,
          error: _message(e),
        );
      }
    }
  }

  void refresh() => load(force: true);
  void prepend(AlbumDetails album) =>
      state = AlbumListState(items: [album, ...state.items], loaded: true);
  void upsert(AlbumDetails album) {
    final items = [...state.items];
    final index = items.indexWhere((item) => item.id == album.id);
    if (index < 0) {
      items.insert(0, album);
    } else {
      items[index] = album;
    }
    state = AlbumListState(items: items, loaded: true);
  }

  void remove(int id) => state = AlbumListState(
    items: state.items.where((x) => x.id != id).toList(),
    loaded: true,
  );
}

class AlbumDetailsState {
  const AlbumDetailsState({
    this.details,
    this.loading = false,
    this.busy = false,
    this.error,
  });
  final AlbumDetails? details;
  final bool loading;
  final bool busy;
  final String? error;
}

class AlbumDetailsController extends Notifier<AlbumDetailsState> {
  late AlbumRepository _repository;
  int _version = 0;
  String? _userId;
  @override
  AlbumDetailsState build() {
    _repository = ref.watch(albumRepositoryProvider);
    ref.listen(authControllerProvider, (_, next) {
      if (_userId != next.user?.id) {
        _userId = next.user?.id;
        _version++;
        state = const AlbumDetailsState();
      }
    }, fireImmediately: true);
    return const AlbumDetailsState();
  }

  Future<void> load(int id) async {
    final v = ++_version;
    state = const AlbumDetailsState(loading: true);
    try {
      final d = await _repository.details(id);
      if (v == _version) state = AlbumDetailsState(details: d);
    } catch (e) {
      if (v == _version) state = AlbumDetailsState(error: _message(e));
    }
  }

  Future<AlbumDetails?> save(int? id, AlbumDraft draft) async {
    if (state.busy) return null;
    state = AlbumDetailsState(details: state.details, busy: true);
    try {
      final d = id == null
          ? await _repository.create(draft)
          : await _repository.update(id, draft);
      state = AlbumDetailsState(details: d);
      return d;
    } catch (e) {
      state = AlbumDetailsState(details: state.details, error: _message(e));
      return null;
    }
  }

  Future<bool> addMemories(int id, List<int> memoryIds) =>
      _run(() => _repository.addMemories(id, memoryIds));
  Future<bool> removeMemory(int id, int memoryId) =>
      _runVoid(() => _repository.removeMemory(id, memoryId));
  Future<bool> delete(int id) => _runVoid(() => _repository.delete(id));
  Future<bool> _run(Future<AlbumDetails> Function() work) async {
    if (state.busy) return false;
    state = AlbumDetailsState(details: state.details, busy: true);
    try {
      state = AlbumDetailsState(details: await work());
      return true;
    } catch (e) {
      state = AlbumDetailsState(details: state.details, error: _message(e));
      return false;
    }
  }

  Future<bool> _runVoid(Future<void> Function() work) async {
    if (state.busy) return false;
    state = AlbumDetailsState(details: state.details, busy: true);
    try {
      await work();
      state = AlbumDetailsState(details: state.details);
      return true;
    } catch (e) {
      state = AlbumDetailsState(details: state.details, error: _message(e));
      return false;
    }
  }
}

class AlbumFormState {
  const AlbumFormState({
    this.title = '',
    this.description = '',
    this.memoryIds = const <int>{},
    this.saving = false,
    this.error,
    this.validationErrors = const {},
  });
  final String title;
  final String description;
  final Set<int> memoryIds;
  final bool saving;
  final String? error;
  final Map<String, List<String>> validationErrors;
  AlbumFormState copyWith({
    String? title,
    String? description,
    Set<int>? memoryIds,
    bool? saving,
    String? error,
    Map<String, List<String>>? validationErrors,
    bool clearError = false,
  }) => AlbumFormState(
    title: title ?? this.title,
    description: description ?? this.description,
    memoryIds: memoryIds ?? this.memoryIds,
    saving: saving ?? this.saving,
    error: clearError ? null : error ?? this.error,
    validationErrors: validationErrors ?? this.validationErrors,
  );
  AlbumDraft get draft => AlbumDraft(
    title: title.trim(),
    description: description.trim().isEmpty ? null : description.trim(),
    memoryIds: memoryIds.toList(growable: false),
  );
}

class AlbumFormController extends Notifier<AlbumFormState> {
  late AlbumRepository _repository;
  String? _userId;
  int _version = 0;
  @override
  AlbumFormState build() {
    _repository = ref.watch(albumRepositoryProvider);
    ref.listen(authControllerProvider, (_, next) {
      if (_userId != next.user?.id) {
        _userId = next.user?.id;
        _version++;
        state = const AlbumFormState();
      }
    }, fireImmediately: true);
    return const AlbumFormState();
  }

  void setInfo({required String title, required String description}) => state =
      state.copyWith(title: title, description: description, clearError: true);
  void setMemorySelected(int id, bool selected) {
    final ids = {...state.memoryIds};
    selected ? ids.add(id) : ids.remove(id);
    state = state.copyWith(memoryIds: ids, clearError: true);
  }

  void clear() => state = const AlbumFormState();
  void seed(AlbumDetails details) => state = AlbumFormState(
    title: details.title,
    description: details.description ?? '',
  );
  Future<AlbumDetails?> save({int? id}) async {
    if (state.saving) return null;
    final version = ++_version;
    final draft = state.draft;
    state = state.copyWith(
      saving: true,
      clearError: true,
      validationErrors: const {},
    );
    try {
      final saved = id == null
          ? await _repository.create(draft)
          : await _repository.update(id, draft);
      if (version == _version) state = const AlbumFormState();
      return saved;
    } on MemoryRequestException catch (error) {
      if (version == _version) {
        state = state.copyWith(
          saving: false,
          error: error.safeMessage,
          validationErrors: error.validationErrors,
        );
      }
      return null;
    } catch (_) {
      if (version == _version) {
        state = state.copyWith(
          saving: false,
          error: 'KhÃ´ng thá»ƒ lÆ°u Album lÃºc nÃ y. Vui lÃ²ng thá»­ láº¡i.',
        );
      }
      return null;
    }
  }
}

class AlbumMemoryPickerState {
  const AlbumMemoryPickerState({
    this.items = const [],
    this.loading = false,
    this.loaded = false,
    this.error,
  });
  final List<MemoryListItem> items;
  final bool loading;
  final bool loaded;
  final String? error;
}

class AlbumMemoryPickerController extends Notifier<AlbumMemoryPickerState> {
  late MemoryRepository _repository;
  String? _userId;
  int _version = 0;
  @override
  AlbumMemoryPickerState build() {
    _repository = ref.watch(memoryRepositoryProvider);
    ref.listen(authControllerProvider, (_, next) {
      if (_userId != next.user?.id) {
        _userId = next.user?.id;
        _version++;
        state = const AlbumMemoryPickerState();
      }
    }, fireImmediately: true);
    return const AlbumMemoryPickerState();
  }

  Future<void> load({bool force = false}) async {
    if (state.loading || (state.loaded && !force)) return;
    final version = ++_version;
    state = AlbumMemoryPickerState(
      items: state.items,
      loading: true,
      loaded: true,
    );
    try {
      final page = await _repository.getMemories(const MemoryQuery());
      if (version == _version) {
        state = AlbumMemoryPickerState(items: page.items, loaded: true);
      }
    } catch (error) {
      if (version == _version) {
        state = AlbumMemoryPickerState(
          items: state.items,
          loaded: true,
          error: _message(error),
        );
      }
    }
  }
}

String _message(Object error) => error is MemoryRequestException
    ? error.safeMessage
    : 'Không thể hoàn tất thao tác Album lúc này.';
