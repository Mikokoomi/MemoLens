import '../../../core/network/api_exception.dart';

enum AuthFailureReason {
  invalidCredentials,
  emailConfirmationRequired,
  invalidSession,
  validation,
  unavailable,
  malformedResponse,
  unknown,
}

class AuthApiException extends ApiException {
  const AuthApiException(
    super.type,
    super.message, {
    required this.reason,
    super.statusCode,
    this.validationErrors = const {},
  });

  final AuthFailureReason reason;
  final Map<String, List<String>> validationErrors;
}
