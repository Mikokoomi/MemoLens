import '../data/models/authenticated_user.dart';

enum AuthStatus {
  initializing,
  unauthenticated,
  authenticating,
  authenticated,
  registrationPendingConfirmation,
  temporarilyUnavailable,
  failure,
}

class AuthState {
  const AuthState({
    required this.status,
    this.user,
    this.pendingEmail,
    this.message,
    this.validationErrors = const {},
  });

  const AuthState.initializing() : this(status: AuthStatus.initializing);
  const AuthState.unauthenticated({String? message})
    : this(status: AuthStatus.unauthenticated, message: message);

  final AuthStatus status;
  final AuthenticatedUser? user;
  final String? pendingEmail;
  final String? message;
  final Map<String, List<String>> validationErrors;

  bool get isBusy => status == AuthStatus.authenticating;
  bool get isAuthenticated => status == AuthStatus.authenticated;
}
