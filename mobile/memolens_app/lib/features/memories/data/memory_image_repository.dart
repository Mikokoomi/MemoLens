import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../app/providers.dart';
import '../../../core/network/api_exception.dart';
import '../../authentication/application/auth_controller.dart';
import 'memory_repository.dart';
import 'models/memory_models.dart';

const maxMemoryImages = 10;
const maxMemoryImageBytes = 5 * 1024 * 1024;
const supportedMemoryImageExtensions = {'jpg', 'jpeg', 'png', 'webp'};

class SelectedMemoryImage {
  const SelectedMemoryImage({
    required this.file,
    required this.displayName,
    required this.extension,
    required this.byteLength,
  });

  final XFile file;
  final String displayName;
  final String extension;
  final int byteLength;

  static Future<SelectedMemoryImage> fromXFile(XFile file) async {
    final name = _safeFileName(file.name);
    final extension = _extensionOf(name);
    return SelectedMemoryImage(
      file: file,
      displayName: name,
      extension: extension,
      byteLength: await file.length(),
    );
  }

  String? validationMessage({int existingCount = 0}) {
    if (!supportedMemoryImageExtensions.contains(extension.toLowerCase())) {
      return '$displayName khong dung dinh dang JPG, JPEG, PNG hoac WEBP.';
    }
    if (byteLength > maxMemoryImageBytes) {
      return '$displayName vuot qua gioi han 5 MB.';
    }
    if (existingCount >= maxMemoryImages) {
      return 'Ky niem nay da co toi da $maxMemoryImages anh.';
    }
    return null;
  }
}

class MemoryImageUploadResult {
  const MemoryImageUploadResult({
    required this.images,
    required this.totalImageCount,
    required this.remainingSlots,
  });

  final List<MemoryImageMetadata> images;
  final int totalImageCount;
  final int remainingSlots;

  factory MemoryImageUploadResult.fromJson(Map<String, dynamic> json) {
    final rawImages = json['images'];
    if (rawImages is! List ||
        json['totalImageCount'] is! int ||
        json['remainingSlots'] is! int) {
      throw const MemoryRequestException(
        ApiException(
          ApiErrorType.malformedResponse,
          'Du lieu anh tra ve chua hop le.',
        ),
      );
    }
    return MemoryImageUploadResult(
      images: rawImages
          .map(
            (item) => MemoryImageMetadata.fromJson(
              Map<String, dynamic>.from(item as Map),
            ),
          )
          .toList(growable: false),
      totalImageCount: json['totalImageCount'] as int,
      remainingSlots: json['remainingSlots'] as int,
    );
  }
}

abstract class MemoryImageRepository {
  Future<MemoryImageUploadResult> uploadImages(
    int memoryId,
    List<SelectedMemoryImage> images, {
    void Function(int sent, int total)? onSendProgress,
  });
  Future<Uint8List> loadImageBytes(int imageId);
  Future<void> deleteImage(int memoryId, int imageId);
}

class ApiMemoryImageRepository implements MemoryImageRepository {
  ApiMemoryImageRepository(this._dio);
  final Dio _dio;

  @override
  Future<MemoryImageUploadResult> uploadImages(
    int memoryId,
    List<SelectedMemoryImage> images, {
    void Function(int sent, int total)? onSendProgress,
  }) async {
    if (images.isEmpty) {
      throw const MemoryRequestException(
        ApiException(ApiErrorType.validation, 'Vui long chon it nhat mot anh.'),
      );
    }
    final form = FormData();
    for (final image in images) {
      form.files.add(
        MapEntry(
          'files',
          await MultipartFile.fromFile(
            image.file.path,
            filename: image.displayName,
          ),
        ),
      );
    }
    try {
      final response = await _dio.post<dynamic>(
        '/api/v1/memories/$memoryId/images',
        data: form,
        onSendProgress: onSendProgress,
      );
      final envelope = Map<String, dynamic>.from(response.data as Map);
      if (envelope['success'] != true || envelope['data'] is! Map) {
        throw const MemoryRequestException(
          ApiException(
            ApiErrorType.malformedResponse,
            'Du lieu tai anh tra ve chua hop le.',
          ),
        );
      }
      return MemoryImageUploadResult.fromJson(
        Map<String, dynamic>.from(envelope['data'] as Map),
      );
    } on DioException catch (error) {
      throw _toRequestException(error);
    }
  }

  @override
  Future<Uint8List> loadImageBytes(int imageId) async {
    try {
      final response = await _dio.get<List<int>>(
        '/api/v1/images/$imageId/content',
        options: Options(responseType: ResponseType.bytes),
      );
      final data = response.data;
      if (data == null) {
        throw const MemoryRequestException(
          ApiException(
            ApiErrorType.malformedResponse,
            'Khong the doc noi dung anh.',
          ),
        );
      }
      return Uint8List.fromList(data);
    } on DioException catch (error) {
      throw _toRequestException(error);
    }
  }

  @override
  Future<void> deleteImage(int memoryId, int imageId) async {
    try {
      final response = await _dio.delete<dynamic>(
        '/api/v1/memories/$memoryId/images/$imageId',
      );
      final envelope = Map<String, dynamic>.from(response.data as Map);
      if (envelope['success'] != true) {
        throw const MemoryRequestException(
          ApiException(
            ApiErrorType.malformedResponse,
            'Khong the xoa anh luc nay.',
          ),
        );
      }
    } on DioException catch (error) {
      throw _toRequestException(error);
    }
  }
}

MemoryRequestException _toRequestException(DioException error) {
  final body = error.response?.data is Map
      ? Map<String, dynamic>.from(error.response!.data as Map)
      : const <String, dynamic>{};
  final validation = body['errors'] is Map
      ? Map<String, List<String>>.fromEntries(
          (body['errors'] as Map).entries.map(
            (entry) => MapEntry(
              entry.key.toString(),
              List<String>.from(entry.value as List),
            ),
          ),
        )
      : const <String, List<String>>{};
  return MemoryRequestException(
    ApiException.fromStatusCode(error.response?.statusCode),
    message: body['message'] as String?,
    validationErrors: validation,
  );
}

final memoryImageRepositoryProvider = Provider<MemoryImageRepository>(
  (ref) => ApiMemoryImageRepository(ref.watch(authenticatedDioProvider)),
);

final privateImageBytesProvider = FutureProvider.autoDispose
    .family<Uint8List, int>((ref, imageId) {
      ref.watch(authControllerProvider.select((state) => state.user?.id));
      return ref.watch(memoryImageRepositoryProvider).loadImageBytes(imageId);
    });

String _safeFileName(String rawName) {
  final normalized = rawName.replaceAll('\\', '/').split('/').last.trim();
  return normalized.isEmpty ? 'anh-ky-niem' : normalized;
}

String _extensionOf(String fileName) {
  final dot = fileName.lastIndexOf('.');
  return dot < 0 || dot == fileName.length - 1
      ? ''
      : fileName.substring(dot + 1).toLowerCase();
}
