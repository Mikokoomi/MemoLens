import 'authenticated_user.dart';

class AuthTokens {
  const AuthTokens({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresInSeconds,
    required this.tokenType,
    required this.user,
  });

  final String accessToken;
  final String refreshToken;
  final int expiresInSeconds;
  final String tokenType;
  final AuthenticatedUser user;

  factory AuthTokens.fromJson(Map<String, dynamic> json) {
    final accessToken = json['accessToken'];
    final refreshToken = json['refreshToken'];
    final expiresInSeconds = json['expiresInSeconds'];
    final tokenType = json['tokenType'];
    final user = json['user'];

    if (accessToken is! String ||
        accessToken.isEmpty ||
        refreshToken is! String ||
        refreshToken.isEmpty ||
        expiresInSeconds is! int ||
        expiresInSeconds <= 0 ||
        tokenType is! String ||
        tokenType.toLowerCase() != 'bearer' ||
        user is! Map) {
      throw const FormatException('Malformed authentication response.');
    }

    return AuthTokens(
      accessToken: accessToken,
      refreshToken: refreshToken,
      expiresInSeconds: expiresInSeconds,
      tokenType: tokenType,
      user: AuthenticatedUser.fromJson(Map<String, dynamic>.from(user)),
    );
  }

  @override
  String toString() =>
      'AuthTokens(expiresInSeconds: $expiresInSeconds, tokenType: $tokenType, user: $user)';
}
