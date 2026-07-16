import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/app/providers.dart';
import 'package:memolens_app/features/authentication/application/auth_controller.dart';
import 'package:memolens_app/features/authentication/data/auth_repository.dart';
import 'package:memolens_app/features/memories/data/memory_image_repository.dart';

import '../helpers/album_fakes.dart';
import '../helpers/auth_fakes.dart';

void main() {
  test(
    'private Album cover state is refreshed after logout and account switch',
    () async {
      final api = FakeAuthApi();
      final storage = FakeTokenStorage();
      final authRepository = AuthRepository(api: api, storage: storage);
      final images = FakeMemoryImageRepository();
      final container = ProviderContainer(
        overrides: [
          authRepositoryProvider.overrideWithValue(authRepository),
          memoryImageRepositoryProvider.overrideWithValue(images),
        ],
      );
      addTearDown(() async {
        container.dispose();
        await authRepository.dispose();
      });

      api.loginResult = testTokensFor('a@example.test');
      await container
          .read(authControllerProvider.notifier)
          .login(email: 'a@example.test', password: 'MemoLens1');
      await container.read(privateImageBytesProvider(91).future);
      expect(images.requestedImageIds, [91]);

      await container.read(authControllerProvider.notifier).logout();
      api.loginResult = testTokensFor('b@example.test');
      await container
          .read(authControllerProvider.notifier)
          .login(email: 'b@example.test', password: 'MemoLens1');
      await container.read(privateImageBytesProvider(91).future);

      expect(images.requestedImageIds, [91, 91]);
    },
  );
}
