import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';

/// Verificación en dos pasos durante el login (la cuenta ya tiene 2FA).
class TotpVerifyScreen extends StatefulWidget {
  const TotpVerifyScreen({super.key});

  @override
  State<TotpVerifyScreen> createState() => _TotpVerifyScreenState();
}

class _TotpVerifyScreenState extends State<TotpVerifyScreen> {
  final _formKey = GlobalKey<FormState>();
  final _code = TextEditingController();
  bool _useRecovery = false;
  bool _rememberDevice = false;

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    final auth = context.read<AuthProvider>();
    final value = _code.text.trim();
    final ok = _useRecovery
        ? await auth.verifyRecovery(value, rememberDevice: _rememberDevice)
        : await auth.verifyTotp(value, rememberDevice: _rememberDevice);
    if (!ok && mounted && auth.error != null) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(auth.error!)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Verificación en dos pasos'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Cancelar',
          onPressed: () => context.read<AuthProvider>().logout(),
        ),
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Form(
              key: _formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Icon(Icons.shield_outlined,
                      size: 64, color: Theme.of(context).colorScheme.primary),
                  const SizedBox(height: 16),
                  Text(
                    _useRecovery
                        ? 'Ingresa uno de tus códigos de recuperación.'
                        : 'Ingresa el código de 6 dígitos de tu app de autenticación.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 24),
                  TextFormField(
                    controller: _code,
                    autofocus: true,
                    keyboardType: _useRecovery
                        ? TextInputType.text
                        : TextInputType.number,
                    inputFormatters: _useRecovery
                        ? null
                        : [
                            FilteringTextInputFormatter.digitsOnly,
                            LengthLimitingTextInputFormatter(6),
                          ],
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 24, letterSpacing: 4),
                    decoration: InputDecoration(
                      labelText: _useRecovery
                          ? 'Código de recuperación'
                          : 'Código',
                      prefixIcon: const Icon(Icons.password_outlined),
                    ),
                    validator: (v) {
                      final value = (v ?? '').trim();
                      if (value.isEmpty) return 'Requerido';
                      if (!_useRecovery && value.length != 6) {
                        return 'Debe tener 6 dígitos';
                      }
                      return null;
                    },
                    onFieldSubmitted: (_) => _submit(),
                  ),
                  CheckboxListTile(
                    value: _rememberDevice,
                    onChanged: auth.loading
                        ? null
                        : (v) => setState(() => _rememberDevice = v ?? false),
                    controlAffinity: ListTileControlAffinity.leading,
                    contentPadding: EdgeInsets.zero,
                    dense: true,
                    title: const Text('Recordar este dispositivo por 30 días'),
                  ),
                  const SizedBox(height: 8),
                  FilledButton(
                    onPressed: auth.loading ? null : _submit,
                    child: auth.loading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Verificar'),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    onPressed: auth.loading
                        ? null
                        : () => setState(() {
                              _useRecovery = !_useRecovery;
                              _code.clear();
                            }),
                    child: Text(_useRecovery
                        ? 'Usar código de la app'
                        : 'Usar código de recuperación'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
