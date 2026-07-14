class LoginRequest {
  const LoginRequest({
    required this.email,
    required this.password,
    this.deviceName = 'MemoLens Flutter',
  });

  final String email;
  final String password;
  final String deviceName;

  Map<String, dynamic> toJson() => {
    'email': email.trim(),
    'password': password,
    'deviceName': deviceName,
  };
}
