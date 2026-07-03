import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../models/login_models.dart';
import '../../providers/auth_provider.dart';

/// Configuración obligatoria de 2FA tras el primer login (cuenta sin 2FA).
class TotpSetupScreen extends StatefulWidget {
  const TotpSetupScreen({super.key});

  @override
  State<TotpSetupScreen> createState() => _TotpSetupScreenState();
}

class _TotpSetupScreenState extends State<TotpSetupScreen> {
  final _formKey = GlobalKey<FormState>();
  final _code = TextEditingController();

  TotpSetupData? _setup;
  bool _loadingSetup = true;
  String? _setupError;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadSetup());
  }

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _loadSetup() async {
    setState(() {
      _loadingSetup = true;
      _setupError = null;
    });
    final auth = context.read<AuthProvider>();
    final data = await auth.startTotpSetup();
    if (!mounted) return;
    setState(() {
      _setup = data;
      _setupError = data == null ? (auth.error ?? 'No se pudo cargar.') : null;
      _loadingSetup = false;
    });
  }

  /// Decodifica el PNG base64 (admite prefijo data URI).
  Uint8List? _decodeQr(String raw) {
    if (raw.isEmpty) return null;
    final comma = raw.indexOf(',');
    final b64 = raw.startsWith('data:') && comma != -1
        ? raw.substring(comma + 1)
        : raw;
    try {
      return base64Decode(b64);
    } catch (_) {
      return null;
    }
  }

  Future<void> _enable() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    final auth = context.read<AuthProvider>();
    final codes = await auth.enableTotp(_code.text.trim());
    if (!mounted) return;
    if (codes == null) {
      if (auth.error != null) {
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(SnackBar(content: Text(auth.error!)));
      }
      return;
    }
    await _showRecoveryCodes(codes);
    if (mounted) context.read<AuthProvider>().finishTotpSetup();
  }

  Future<void> _showRecoveryCodes(List<String> codes) {
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: const Text('Guarda tus códigos de recuperación'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Cada código sirve una sola vez si pierdes el acceso a tu app. '
              'Guárdalos en un lugar seguro.',
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Theme.of(ctx).colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(8),
              ),
              child: SelectableText(
                codes.join('\n'),
                style: const TextStyle(
                    fontFamily: 'monospace', fontSize: 16, height: 1.6),
              ),
            ),
            const SizedBox(height: 8),
            TextButton.icon(
              onPressed: () {
                Clipboard.setData(ClipboardData(text: codes.join('\n')));
                ScaffoldMessenger.of(ctx)
                  ..hideCurrentSnackBar()
                  ..showSnackBar(
                      const SnackBar(content: Text('Códigos copiados')));
              },
              icon: const Icon(Icons.copy),
              label: const Text('Copiar códigos'),
            ),
          ],
        ),
        actions: [
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Ya los guardé, continuar'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Configurar 2FA'),
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
            child: _buildBody(context, auth),
          ),
        ),
      ),
    );
  }

  Widget _buildBody(BuildContext context, AuthProvider auth) {
    if (_loadingSetup) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 48),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_setup == null) {
      return Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.error_outline, size: 48),
          const SizedBox(height: 12),
          Text(_setupError ?? 'No se pudo cargar la configuración.',
              textAlign: TextAlign.center),
          const SizedBox(height: 16),
          FilledButton(onPressed: _loadSetup, child: const Text('Reintentar')),
        ],
      );
    }

    final qrBytes = _decodeQr(_setup!.qrCodeBase64);
    return Form(
      key: _formKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Escanea el código QR con Google Authenticator, Microsoft '
            'Authenticator o similar.',
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: 20),
          if (qrBytes != null)
            Center(
              child: Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Image.memory(qrBytes, width: 200, height: 200),
              ),
            ),
          const SizedBox(height: 16),
          Text('¿No puedes escanear? Ingresa esta clave manualmente:',
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodySmall),
          const SizedBox(height: 8),
          InkWell(
            onTap: () {
              Clipboard.setData(ClipboardData(text: _setup!.secretKey));
              ScaffoldMessenger.of(context)
                ..hideCurrentSnackBar()
                ..showSnackBar(const SnackBar(content: Text('Clave copiada')));
            },
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Flexible(
                    child: SelectableText(
                      _setup!.secretKey,
                      style: const TextStyle(
                          fontFamily: 'monospace',
                          fontSize: 16,
                          letterSpacing: 1.5),
                    ),
                  ),
                  const SizedBox(width: 8),
                  const Icon(Icons.copy, size: 18),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          TextFormField(
            controller: _code,
            keyboardType: TextInputType.number,
            inputFormatters: [
              FilteringTextInputFormatter.digitsOnly,
              LengthLimitingTextInputFormatter(6),
            ],
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 24, letterSpacing: 4),
            decoration: const InputDecoration(
              labelText: 'Código de 6 dígitos',
              prefixIcon: Icon(Icons.password_outlined),
            ),
            validator: (v) {
              final value = (v ?? '').trim();
              if (value.length != 6) return 'Debe tener 6 dígitos';
              return null;
            },
            onFieldSubmitted: (_) => _enable(),
          ),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: auth.loading ? null : _enable,
            child: auth.loading
                ? const SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Text('Activar 2FA'),
          ),
        ],
      ),
    );
  }
}
