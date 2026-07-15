import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker/image_picker.dart';
import 'package:memolens_app/features/memories/data/memory_image_repository.dart';

void main() {
  Future<SelectedMemoryImage> select(String name, int length) =>
      SelectedMemoryImage.fromXFile(_TestXFile(name, length));

  test('accepts supported extensions without case sensitivity', () async {
    for (final name in ['one.JPG', 'two.jpeg', 'three.PnG', 'four.webp']) {
      expect((await select(name, 8)).validationMessage(), isNull);
    }
  });

  test('rejects unsupported and over-limit local images', () async {
    expect((await select('private.gif', 8)).validationMessage(), isNotNull);
    expect(
      (await select('large.jpg', maxMemoryImageBytes + 1)).validationMessage(),
      contains('5 MB'),
    );
  });

  test('strips path-like source names from the display name', () async {
    final image = await select('C:\\private\\nested\\ky-niem.jpg', 8);
    expect(image.displayName, 'ky-niem.jpg');
    expect(image.displayName, isNot(contains('private')));
  });

  test('parses the frozen upload response without private paths', () {
    final result = MemoryImageUploadResult.fromJson({
      'images': [
        {
          'id': 7,
          'originalFileName': 'anh.jpg',
          'uploadedAt': '2026-07-15T00:00:00Z',
          'contentUrl': '/api/v1/images/7/content',
        },
      ],
      'totalImageCount': 1,
      'remainingSlots': 9,
    });
    expect(result.images.single.id, 7);
    expect(result.remainingSlots, 9);
  });
}

class _TestXFile extends XFile {
  _TestXFile(this._name, this._length) : super('');
  final String _name;
  final int _length;

  @override
  String get name => _name;

  @override
  Future<int> length() async => _length;
}
