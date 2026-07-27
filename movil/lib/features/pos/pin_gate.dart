import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../core/storage/pin_storage.dart';
import '../../providers/auth_provider.dart';

/// Compuerta que exige el PIN local antes de mostrar [child].
///
/// Si el usuario todavía no definió un PIN, lo obliga a crearlo. Tras
/// [PinStorage.maxAttempts] fallos cierra la sesión completa, de modo que
/// recuperar el acceso exige contraseña y 2FA.
class PinGate extends StatefulWidget {
  const PinGate({super.key, required this.child});

  final Widget child;

  @override
  State<PinGate> createState() => _PinGateState();
}

enum _GateMode { checking, create, verify, unlocked }

class _PinGateState extends State<PinGate> {
  final _storage = PinStorage();
  final _formKey = GlobalKey<FormState>();
  final _pin = TextEditingController();
  final _confirm = TextEditingController();

  _GateMode _mode = _GateMode.checking;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _decideMode();
  }

  @override
  void dispose() {
    _pin.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _decideMode() async {
    final has = await _storage.hasPin();
    if (!mounted) return;
    setState(() => _mode = has ? _GateMode.verify : _GateMode.create);
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    setState(() {
      _busy = true;
      _error = null;
    });

    if (_mode == _GateMode.create) {
      await _storage.setPin(_pin.text);
      if (!mounted) return;
      setState(() {
        _busy = false;
        _mode = _GateMode.unlocked;
      });
      return;
    }

    final ok = await _storage.verifyPin(_pin.text);
    if (!mounted) return;

    if (ok) {
      setState(() {
        _busy = false;
        _mode = _GateMode.unlocked;
      });
      return;
    }

    final failed = await _storage.failedAttempts();
    if (!mounted) return;

    if (failed >= PinStorage.maxAttempts) {
      // Demasiados intentos: cerramos sesión. AuthProvider.logout() limpia el
      // storage (incluido el PIN) y devuelve la app al login.
      await context.read<AuthProvider>().logout();
      return;
    }

    _pin.clear();
    setState(() {
      _busy = false;
      _error =
          'PIN incorrecto. Te ${PinStorage.maxAttempts - failed == 1 ? 'queda' : 'quedan'} '
          '${PinStorage.maxAttempts - failed} '
          '${PinStorage.maxAttempts - failed == 1 ? 'intento' : 'intentos'}.';
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_mode == _GateMode.unlocked) return widget.child;

    if (_mode == _GateMode.checking) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    final creating = _mode == _GateMode.create;

    return Scaffold(
      appBar: AppBar(title: Text(creating ? 'Crear PIN' : 'PIN de acceso')),
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
                  Icon(creating ? Icons.pin_outlined : Icons.lock_outline,
                      size: 64, color: Theme.of(context).colorScheme.primary),
                  const SizedBox(height: 16),
                  Text(
                    creating
                        ? 'Define un PIN de 4 a 6 dígitos para entrar al punto de venta.'
                        : 'Ingresa tu PIN para entrar al punto de venta.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 24),
                  _PinField(
                    controller: _pin,
                    label: creating ? 'Nuevo PIN' : 'PIN',
                    autofocus: true,
                    validator: (v) {
                      final value = (v ?? '').trim();
                      if (value.length < 4) return 'Mínimo 4 dígitos';
                      return null;
                    },
                    onSubmitted: creating ? null : (_) => _submit(),
                  ),
                  if (creating) ...[
                    const SizedBox(height: 16),
                    _PinField(
                      controller: _confirm,
                      label: 'Confirmar PIN',
                      validator: (v) =>
                          (v ?? '').trim() == _pin.text ? null : 'No coincide',
                      onSubmitted: (_) => _submit(),
                    ),
                  ],
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    Text(
                      _error!,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                          color: Theme.of(context).colorScheme.error),
                    ),
                  ],
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: _busy ? null : _submit,
                    child: _busy
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text(creating ? 'Guardar PIN' : 'Entrar'),
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

class _PinField extends StatelessWidget {
  const _PinField({
    required this.controller,
    required this.label,
    required this.validator,
    this.autofocus = false,
    this.onSubmitted,
  });

  final TextEditingController controller;
  final String label;
  final String? Function(String?) validator;
  final bool autofocus;
  final void Function(String)? onSubmitted;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      autofocus: autofocus,
      obscureText: true,
      keyboardType: TextInputType.number,
      inputFormatters: [
        FilteringTextInputFormatter.digitsOnly,
        LengthLimitingTextInputFormatter(6),
      ],
      textAlign: TextAlign.center,
      style: const TextStyle(fontSize: 24, letterSpacing: 8),
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: const Icon(Icons.password_outlined),
      ),
      validator: validator,
      onFieldSubmitted: onSubmitted,
    );
  }
}
