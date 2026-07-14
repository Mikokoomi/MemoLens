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
import '../data/models/register_request.dart';
import 'widgets/auth_form_field.dart';
import 'widgets/password_field.dart';

class RegisterPage extends ConsumerStatefulWidget {
  const RegisterPage({super.key});

  @override
  ConsumerState<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends ConsumerState<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _displayNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _displayNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    await ref
        .read(authControllerProvider.notifier)
        .register(
          RegisterRequest(
            displayName: _displayNameController.text,
            email: _emailController.text,
            password: _passwordController.text,
            confirmPassword: _confirmPasswordController.text,
          ),
        );
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider);
    final isBusy = authState.isBusy;
    final errors = authState.validationErrors;

    return Scaffold(
      body: PaperPage(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 460),
            child: Form(
              key: _formKey,
              child: PaperCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Icon(
                      Icons.edit_note_rounded,
                      color: AppColors.teal,
                      size: 46,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      'Tạo tài khoản',
                      style: Theme.of(context).textTheme.headlineSmall,
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'Bắt đầu không gian ký ức riêng tư của bạn.',
                      style: Theme.of(context).textTheme.bodyMedium,
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    AuthFormField(
                      controller: _displayNameController,
                      label: 'Tên hiển thị (không bắt buộc)',
                      icon: Icons.person_outline_rounded,
                      textInputAction: TextInputAction.next,
                      enabled: !isBusy,
                      validator: (value) => (value?.trim().length ?? 0) > 100
                          ? 'Tên hiển thị không quá 100 ký tự.'
                          : null,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    AuthFormField(
                      controller: _emailController,
                      label: 'Email',
                      icon: Icons.email_outlined,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.newUsername],
                      enabled: !isBusy,
                      validator: (value) {
                        if (errors['email']?.isNotEmpty == true) {
                          return errors['email']!.first;
                        }
                        final email = value?.trim() ?? '';
                        if (email.isEmpty) return 'Vui lòng nhập email.';
                        if (!email.contains('@') || !email.contains('.')) {
                          return 'Email chưa đúng định dạng.';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: AppSpacing.md),
                    PasswordField(
                      controller: _passwordController,
                      label: 'Mật khẩu',
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.newPassword],
                      enabled: !isBusy,
                      validator: (value) {
                        if (errors['password']?.isNotEmpty == true) {
                          return errors['password']!.first;
                        }
                        if (value == null || value.length < 8) {
                          return 'Mật khẩu cần ít nhất 8 ký tự.';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: AppSpacing.md),
                    PasswordField(
                      controller: _confirmPasswordController,
                      label: 'Xác nhận mật khẩu',
                      textInputAction: TextInputAction.done,
                      autofillHints: const [AutofillHints.newPassword],
                      enabled: !isBusy,
                      validator: (value) => value != _passwordController.text
                          ? 'Mật khẩu xác nhận không khớp.'
                          : null,
                      onFieldSubmitted: (_) => isBusy ? null : _submit(),
                    ),
                    if (authState.status == AuthStatus.failure &&
                        authState.message != null) ...[
                      const SizedBox(height: AppSpacing.sm),
                      Text(
                        authState.message!,
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: AppColors.danger,
                        ),
                        textAlign: TextAlign.center,
                      ),
                    ],
                    const SizedBox(height: AppSpacing.lg),
                    PrimaryButton(
                      label: isBusy ? 'Đang tạo tài khoản...' : 'Tạo tài khoản',
                      icon: Icons.person_add_alt_1_rounded,
                      onPressed: isBusy ? null : _submit,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    TextButton(
                      onPressed: isBusy ? null : () => context.go('/login'),
                      child: const Text('Đã có tài khoản? Đăng nhập'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
