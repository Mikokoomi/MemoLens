class RegisterRequest {
  const RegisterRequest({
    required this.email,
    required this.password,
    required this.confirmPassword,
    this.displayName,
  });

  final String? displayName;
  final String email;
  final String password;
  final String confirmPassword;

  Map<String, dynamic> toJson() => {
    'displayName': displayName?.trim(),
    'email': email.trim(),
    'password': password,
    'confirmPassword': confirmPassword,
  };
}
