enum ApiErrorType {
  validation,
  unauthorized,
  notFound,
  conflict,
  unsupportedMedia,
  server,
  timeout,
  unavailable,
  malformedResponse,
  unknown,
}

class ApiException implements Exception {
  const ApiException(this.type, this.message, {this.statusCode});

  final ApiErrorType type;
  final String message;
  final int? statusCode;

  factory ApiException.fromStatusCode(int? statusCode) {
    return switch (statusCode) {
      400 => const ApiException(
        ApiErrorType.validation,
        'Dữ liệu gửi lên chưa hợp lệ.',
        statusCode: 400,
      ),
      401 => const ApiException(
        ApiErrorType.unauthorized,
        'Phiên truy cập không hợp lệ hoặc đã hết hạn.',
        statusCode: 401,
      ),
      404 => const ApiException(
        ApiErrorType.notFound,
        'Không tìm thấy nội dung được yêu cầu.',
        statusCode: 404,
      ),
      409 => const ApiException(
        ApiErrorType.conflict,
        'Dữ liệu đang xung đột. Vui lòng thử lại.',
        statusCode: 409,
      ),
      415 => const ApiException(
        ApiErrorType.unsupportedMedia,
        'Định dạng dữ liệu chưa được hỗ trợ.',
        statusCode: 415,
      ),
      final code? when code >= 500 => ApiException(
        ApiErrorType.server,
        'Máy chủ MemoLens đang gặp sự cố. Vui lòng thử lại sau.',
        statusCode: statusCode,
      ),
      _ => ApiException(
        ApiErrorType.unknown,
        'Không thể hoàn tất yêu cầu lúc này.',
        statusCode: statusCode,
      ),
    };
  }
}
