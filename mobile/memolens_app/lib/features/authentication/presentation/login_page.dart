import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/theme/app_colors.dart';
import '../../../app/theme/app_spacing.dart';
import '../../../core/widgets/paper_card.dart';
import '../../../core/widgets/paper_page.dart';
import '../../../core/widgets/primary_button.dart';
import '../application/auth_controller.dart';
import '../application/auth_state.dart';
import 'widgets/auth_form_field.dart';
import 'widgets/password_field.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    await ref
        .read(authControllerProvider.notifier)
        .login(
          email: _emailController.text,
          password: _passwordController.text,
        );
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider);
    final isBusy = authState.isBusy;

    return Scaffold(
      body: PaperPage(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: AutofillGroup(
              child: Form(
                key: _formKey,
                child: PaperCard(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Icon(
                        Icons.local_florist_outlined,
                        color: AppColors.teal,
                        size: 42,
                      ),
                      const SizedBox(height: AppSpacing.md),
                      Text(
                        'Chào mừng trở lại',
                        style: Theme.of(context).textTheme.headlineSmall,
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        'Đăng nhập để tiếp tục lưu giữ ký ức riêng của bạn.',
                        style: Theme.of(context).textTheme.bodyMedium,
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: AppSpacing.lg),
                      AuthFormField(
                        controller: _emailController,
                        label: 'Email',
                        icon: Icons.email_outlined,
                        keyboardType: TextInputType.emailAddress,
                        textInputAction: TextInputAction.next,
                        autofillHints: const [AutofillHints.email],
                        enabled: !isBusy,
                        validator: _validateEmail,
                      ),
                      const SizedBox(height: AppSpacing.md),
                      PasswordField(
                        controller: _passwordController,
                        textInputAction: TextInputAction.done,
                        autofillHints: const [AutofillHints.password],
                        enabled: !isBusy,
                        validator: (value) => value == null || value.isEmpty
                            ? 'Vui lòng nhập mật khẩu.'
                            : null,
                        onFieldSubmitted: (_) => isBusy ? null : _submit(),
                      ),
                      if (authState.status == AuthStatus.failure &&
                          authState.message != null) ...[
                        const SizedBox(height: AppSpacing.sm),
                        Text(
                          authState.message!,
                          style: Theme.of(context).textTheme.bodyMedium
                              ?.copyWith(color: AppColors.danger),
                          textAlign: TextAlign.center,
                        ),
                      ],
                      const SizedBox(height: AppSpacing.lg),
                      PrimaryButton(
                        label: isBusy ? 'Đang đăng nhập...' : 'Đăng nhập',
                        icon: Icons.login_rounded,
                        onPressed: isBusy ? null : _submit,
                      ),
                      const SizedBox(height: AppSpacing.sm),
                      TextButton(
                        onPressed: isBusy
                            ? null
                            : () => context.go('/register'),
                        child: const Text('Chưa có tài khoản? Tạo tài khoản'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  String? _validateEmail(String? value) {
    final email = value?.trim() ?? '';
    if (email.isEmpty) return 'Vui lòng nhập email.';
    if (!email.contains('@') || !email.contains('.')) {
      return 'Email chưa đúng định dạng.';
    }
    return null;
  }
}
