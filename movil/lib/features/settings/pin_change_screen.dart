import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../core/storage/pin_storage.dart';
import '../../providers/auth_provider.dart';

/// Crea o cambia el PIN local que protege el punto de venta.
///
/// Si ya existe un PIN se exige el actual. Los fallos cuentan contra el mismo
/// contador que usa `PinGate`, así que agotar los intentos cierra la sesión.
class PinChangeScreen extends StatefulWidget {
  const PinChangeScreen({super.key, required this.hasPin});

  /// `true` si el usuario ya tiene un PIN definido.
  final bool hasPin;

  @override
  State<PinChangeScreen> createState() => _PinChangeScreenState();
}

class _PinChangeScreenState extends State<PinChangeScreen> {
  final _storage = PinStorage();
  final _formKey = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _pin = TextEditingController();
  final _confirm = TextEditingController();

  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _current.dispose();
    _pin.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    setState(() {
      _busy = true;
      _error = null;
    });

    if (widget.hasPin) {
      final ok = await _storage.verifyPin(_current.text);
      if (!mounted) return;
      if (!ok) {
        final failed = await _storage.failedAttempts();
        if (!mounted) return;
        if (failed >= PinStorage.maxAttempts) {
          await context.read<AuthProvider>().logout();
          return;
        }
        final left = PinStorage.maxAttempts - failed;
        _current.clear();
        setState(() {
          _busy = false;
          _error = 'PIN actual incorrecto. Te ${left == 1 ? 'queda' : 'quedan'} '
              '$left ${left == 1 ? 'intento' : 'intentos'}.';
        });
        return;
      }
    }

    await _storage.setPin(_pin.text);
    if (!mounted) return;
    Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.hasPin ? 'Cambiar PIN' : 'Crear PIN')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Icon(Icons.pin_outlined,
                      size: 56, color: Theme.of(context).colorScheme.primary),
                  const SizedBox(height: 16),
                  Text(
                    widget.hasPin
                        ? 'Ingresa tu PIN actual y define uno nuevo de 4 a 6 dígitos.'
                        : 'Define un PIN de 4 a 6 dígitos para entrar al punto de venta.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 24),
                  if (widget.hasPin) ...[
                    _PinField(
                      controller: _current,
                      label: 'PIN actual',
                      autofocus: true,
                      validator: (v) => (v ?? '').trim().length < 4
                          ? 'Mínimo 4 dígitos'
                          : null,
                    ),
                    const SizedBox(height: 16),
                  ],
                  _PinField(
                    controller: _pin,
                    label: 'Nuevo PIN',
                    autofocus: !widget.hasPin,
                    validator: (v) {
                      final value = (v ?? '').trim();
                      if (value.length < 4) return 'Mínimo 4 dígitos';
                      if (widget.hasPin && value == _current.text) {
                        return 'Debe ser distinto al actual';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  _PinField(
                    controller: _confirm,
                    label: 'Confirmar nuevo PIN',
                    validator: (v) =>
                        (v ?? '').trim() == _pin.text ? null : 'No coincide',
                    onSubmitted: (_) => _submit(),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    Text(
                      _error!,
                      textAlign: TextAlign.center,
                      style:
                          TextStyle(color: Theme.of(context).colorScheme.error),
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
                        : const Text('Guardar PIN'),
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
