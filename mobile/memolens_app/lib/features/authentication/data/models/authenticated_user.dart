class AuthenticatedUser {
  const AuthenticatedUser({
    required this.id,
    required this.email,
    required this.roles,
    this.displayName,
  });

  final String id;
  final String email;
  final String? displayName;
  final List<String> roles;

  String get safeDisplayName {
    final name = displayName?.trim();
    return name == null || name.isEmpty ? email : name;
  }

  factory AuthenticatedUser.fromJson(Map<String, dynamic> json) {
    final id = json['id'];
    final email = json['email'];
    final roles = json['roles'];

    if (id is! String ||
        id.trim().isEmpty ||
        email is! String ||
        email.trim().isEmpty ||
        roles is! List ||
        roles.any((role) => role is! String)) {
      throw const FormatException('Malformed authenticated user.');
    }

    final displayName = json['displayName'];
    if (displayName != null && displayName is! String) {
      throw const FormatException('Malformed display name.');
    }

    return AuthenticatedUser(
      id: id,
      email: email,
      displayName: displayName as String?,
      roles: List<String>.unmodifiable(roles.cast<String>()),
    );
  }

  @override
  String toString() =>
      'AuthenticatedUser(id: $id, email: $email, roles: $roles)';
}
