import 'package:flutter_test/flutter_test.dart';
import 'package:memolens_app/features/authentication/data/models/auth_tokens.dart';

void main() {
  test('valid login response parses exact backend fields', () {
    final tokens = AuthTokens.fromJson({
      'accessToken': 'secret-access',
      'refreshToken': 'secret-refresh',
      'expiresInSeconds': 900,
      'tokenType': 'Bearer',
      'user': {
        'id': 'user-1',
        'email': 'user@example.test',
        'displayName': 'Minh',
        'roles': ['User'],
      },
    });

    expect(tokens.expiresInSeconds, 900);
    expect(tokens.user.email, 'user@example.test');
    expect(tokens.user.roles, ['User']);
  });

  test('malformed token response fails safely', () {
    expect(
      () => AuthTokens.fromJson({
        'accessToken': '',
        'refreshToken': 'refresh',
        'expiresInSeconds': 900,
        'tokenType': 'Bearer',
        'user': <String, dynamic>{},
      }),
      throwsFormatException,
    );
  });

  test('token values never appear in toString', () {
    final tokens = AuthTokens.fromJson({
      'accessToken': 'secret-access',
      'refreshToken': 'secret-refresh',
      'expiresInSeconds': 900,
      'tokenType': 'Bearer',
      'user': {
        'id': 'user-1',
        'email': 'user@example.test',
        'displayName': null,
        'roles': ['User'],
      },
    });

    expect(tokens.toString(), isNot(contains('secret-access')));
    expect(tokens.toString(), isNot(contains('secret-refresh')));
  });
}
