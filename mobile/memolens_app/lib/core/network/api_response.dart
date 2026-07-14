class ApiResponse<T> {
  const ApiResponse({required this.success, required this.message, this.data});

  final bool success;
  final String message;
  final T? data;

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? value) decodeData,
  ) {
    return ApiResponse<T>(
      success: json['success'] == true,
      message: json['message'] as String? ?? '',
      data: json.containsKey('data') && json['data'] != null
          ? decodeData(json['data'])
          : null,
    );
  }
}
