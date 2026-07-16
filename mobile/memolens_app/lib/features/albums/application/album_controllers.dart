import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../app/providers.dart';
import '../../authentication/application/auth_controller.dart';
import '../../memories/data/memory_repository.dart';
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
  @override
  AlbumDetailsState build() {
    _repository = ref.watch(albumRepositoryProvider);
    ref.listen(authControllerProvider, (_, next) {
      if (next.user == null) {
        _version++;
        state = const AlbumDetailsState();
      }
    });
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

String _message(Object error) => error is MemoryRequestException
    ? error.safeMessage
    : 'Không thể hoàn tất thao tác Album lúc này.';
