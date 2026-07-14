class AppConfig {
  const AppConfig({required this.apiBaseUrl});

  static const _defaultBaseUrl = 'http://10.0.2.2:5296';

  final String apiBaseUrl;

  factory AppConfig.fromEnvironment() {
    const configuredUrl = String.fromEnvironment('API_BASE_URL');
    return AppConfig(
      apiBaseUrl: configuredUrl.isEmpty ? _defaultBaseUrl : configuredUrl,
    );
  }

  bool get hasValidApiBaseUrl {
    final uri = Uri.tryParse(apiBaseUrl);
    return uri != null &&
        uri.hasScheme &&
        uri.hasAuthority &&
        (uri.scheme == 'http' || uri.scheme == 'https');
  }

  Uri resolve(String path) {
    if (!hasValidApiBaseUrl) {
      throw const FormatException('API_BASE_URL không hợp lệ.');
    }
    return Uri.parse(apiBaseUrl).resolve(path);
  }
}
