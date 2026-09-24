import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/cash_session.dart';
import '../../services/sale_service.dart';
import '../pos/pos_dialogs.dart';

/// Cierre de caja a ciegas, igual que en la web: el cajero declara lo que
/// tiene en cada medio sin ver lo esperado, y recién al cerrar ve la diferencia.
///
/// El servidor decide si hace falta observación o supervisor (según la
/// configuración del cierre y los intentos). Si pide supervisor y la
/// observación ya está, se abre la autorización y se reintenta con esa firma,
/// que vale solo para este intento.
///
/// Devuelve el turno cerrado, o null si se salió sin cerrar.
class CloseCashScreen extends StatefulWidget {
  const CloseCashScreen({super.key, required this.session});
  final CashSession session;

  @override
  State<CloseCashScreen> createState() => _CloseCashScreenState();
}

class _CloseCashScreenState extends State<CloseCashScreen> {
  CashCloseSettings? _settings;
  String? _loadError;

  final Map<String, TextEditingController> _declared = {};
  final Map<double, TextEditingController> _qty = {
    for (final v in bobDenominations) v: TextEditingController(),
  };
  final _notes = TextEditingController();
  final _notesFocus = FocusNode();

  bool _showDenominations = false;
  bool _saving = false;
  String? _error;
  CashSession? _result;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    for (final c in [..._declared.values, ..._qty.values, _notes]) {
      c.dispose();
    }
    _notesFocus.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loadError = null);
    try {
      final s = await context.read<SaleService>().closeSettings();
      if (!mounted) return;
      setState(() {
        _settings = s;
        for (final m in s.counted) {
          _declared[m.id] = TextEditingController();
        }
        // Con billetes obligatorios el efectivo es su suma: arranca en cero.
        final cash = _cash;
        if (s.requireDenominations && cash != null) _declared[cash.id]!.text = '0.00';
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _loadError = e.message);
    }
  }

  CloseMethod? get _cash => _settings?.counted.where((m) => m.affectsCash).firstOrNull;

  Map<double, int> get _quantities => {
        for (final e in _qty.entries) e.key: int.tryParse(e.value.text.trim()) ?? 0,
      };

  double get _denominationTotal => denominationTotal(_quantities);

  /// Mientras se cuenta por billetes, el efectivo declarado es su suma.
  bool get _countingByDenomination =>
      (_settings?.requireDenominations ?? false) || (_showDenominations && _denominationTotal > 0);

  void _syncCash() {
    final cash = _cash;
    if (cash == null) return;
    _declared[cash.id]!.text = _denominationTotal.toStringAsFixed(2);
    setState(() {});
  }

  double? _parse(TextEditingController c) =>
      double.tryParse(c.text.trim().replaceAll(',', '.'));

  /// De dónde sale lo que se declara en cada medio.
  String _hint(CloseMethod m) {
    if (m.affectsCash) return 'Lo contado en el cajón, con el fondo inicial';
    if (RegExp(r'tarjeta|pos|dat[aá]fono', caseSensitive: false).hasMatch(m.name)) {
      return 'Total del cierre de lote del datáfono';
    }
    return 'Lo recibido en la cuenta';
  }

  Future<void> _submit({String supervisorToken = ''}) async {
    final s = _settings;
    if (s == null) return;

    final faltan = s.counted.where((m) => _parse(_declared[m.id]!) == null).map((m) => m.name).toList();
    if (faltan.isNotEmpty) {
      setState(() => _error = 'Declare ${faltan.join(', ')} (0 si no hubo).');
      return;
    }

    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final cerrado = await context.read<SaleService>().closeSession(
            widget.session.id,
            counts: {for (final m in s.counted) m.id: _parse(_declared[m.id]!)!},
            // Todas las filas, también las en cero: el servidor las descarta al
            // guardar, y así un cajón vacío también puede declararse.
            denominations: _countingByDenomination
                ? [for (final e in _quantities.entries) DenominationCount(e.key, e.value)]
                : const [],
            notes: _notes.text.trim(),
            supervisorAuthToken: supervisorToken,
          );
      if (mounted) setState(() => _result = cerrado);
    } on ApiException catch (e) {
      if (!mounted) return;
      final requires = e.data is Map ? e.data['CloseRequires'] : null;
      setState(() {
        _saving = false;
        _error = e.message;
      });

      // La observación ya está: falta la firma. Se pide y se reintenta una vez.
      if (requires == 'supervisor' && supervisorToken.isEmpty && _notes.text.trim().isNotEmpty) {
        final token = await supervisorAuthDialog(context, context.read<SaleService>(), reason: e.message);
        if (token != null && mounted) await _submit(supervisorToken: token);
      } else if (requires != null && _notes.text.trim().isEmpty) {
        _notesFocus.requestFocus();
      }
      return;
    }
    if (mounted) setState(() => _saving = false);
  }

  @override
  Widget build(BuildContext context) {
    final result = _result;
    return PopScope(
      // Ya cerrada, volver entrega el turno a quien abrió la pantalla.
      canPop: result == null,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && result != null) Navigator.pop(context, result);
      },
      child: Scaffold(
        appBar: AppBar(title: Text(result == null ? 'Cerrar caja' : 'Caja cerrada')),
        body: result != null
            ? _resultView(result)
            : _settings == null
                ? _loadingOrError()
                : _form(_settings!),
      ),
    );
  }

  Widget _loadingOrError() => Center(
        child: _loadError == null
            ? const CircularProgressIndicator()
            : Column(mainAxisSize: MainAxisSize.min, children: [
                Text(_loadError!),
                const SizedBox(height: 8),
                FilledButton(onPressed: _load, child: const Text('Reintentar')),
              ]),
      );

  Widget _form(CashCloseSettings s) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
      children: [
        Text(
          'Cuente y declare lo que tiene. El sistema lo compara con lo esperado al cerrar.',
          style: TextStyle(color: Theme.of(context).hintColor),
        ),
        const SizedBox(height: 12),
        for (final m in s.counted) ...[
          _methodField(m, s),
          if (m.affectsCash && (_showDenominations || s.requireDenominations)) _denominationGrid(s),
          const SizedBox(height: 12),
        ],
        TextField(
          controller: _notes,
          focusNode: _notesFocus,
          maxLines: 2,
          decoration: const InputDecoration(
            labelText: 'Observaciones',
            helperText: 'Obligatoria si hay una diferencia importante.',
          ),
        ),
        if (_error != null) ...[
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.errorContainer,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.onErrorContainer)),
          ),
        ],
        const SizedBox(height: 16),
        FilledButton.icon(
          style: FilledButton.styleFrom(
            minimumSize: const Size.fromHeight(52),
            backgroundColor: Theme.of(context).colorScheme.error,
            foregroundColor: Theme.of(context).colorScheme.onError,
          ),
          onPressed: _saving ? null : () => _submit(),
          icon: _saving
              ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : const Icon(Icons.lock),
          label: const Text('Cerrar y arquear'),
        ),
      ],
    );
  }

  Widget _methodField(CloseMethod m, CashCloseSettings s) {
    final porBilletes = m.affectsCash && _countingByDenomination;
    return TextField(
      controller: _declared[m.id],
      readOnly: porBilletes,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
      style: const TextStyle(fontSize: 18),
      decoration: InputDecoration(
        labelText: m.name,
        helperText: porBilletes ? 'Suma de billetes y monedas' : _hint(m),
        prefixText: 'Bs. ',
        hintText: '0.00',
        suffixIcon: m.affectsCash && !s.requireDenominations
            ? IconButton(
                tooltip: 'Contar por billetes y monedas',
                isSelected: _showDenominations,
                icon: const Icon(Icons.payments_outlined),
                onPressed: () => setState(() => _showDenominations = !_showDenominations),
              )
            : null,
      ),
    );
  }

  Widget _denominationGrid(CashCloseSettings s) {
    return Container(
      margin: const EdgeInsets.only(top: 8),
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        border: Border.all(color: Theme.of(context).dividerColor),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(s.requireDenominations ? 'Billetes y monedas (obligatorio)' : 'Billetes y monedas',
              style: TextStyle(color: Theme.of(context).hintColor)),
          const SizedBox(height: 4),
          LayoutBuilder(
            builder: (_, c) {
              final ancho = (c.maxWidth - 8) / 2;
              return Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  for (final v in bobDenominations)
                    SizedBox(
                      width: ancho,
                      child: TextField(
                        key: ValueKey('den-$v'),
                        controller: _qty[v],
                        keyboardType: TextInputType.number,
                        inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                        textAlign: TextAlign.end,
                        decoration: InputDecoration(
                          isDense: true,
                          prefixText: '${DenominationCount.label(v)} × ',
                          hintText: '0',
                        ),
                        onChanged: (_) => _syncCash(),
                      ),
                    ),
                ],
              );
            },
          ),
          const SizedBox(height: 8),
          Text('Total contado: ${currency(_denominationTotal)}',
              textAlign: TextAlign.end, style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  /// Recién ahora se ve el resultado: esperado, declarado y diferencia.
  Widget _resultView(CashSession r) {
    Color colorDe(double d) => d == 0 ? Colors.green : d > 0 ? Colors.orange.shade800 : Colors.red;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        for (final c in r.counts)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(c.name, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                  const SizedBox(height: 6),
                  _row('Esperado', currency(c.expected)),
                  _row('Declarado', currency(c.declared)),
                  _row(
                    c.difference == 0 ? 'Sin diferencia' : c.difference > 0 ? 'Sobrante' : 'Faltante',
                    '${c.difference > 0 ? '+' : ''}${currency(c.difference)}',
                    color: colorDe(c.difference),
                  ),
                ],
              ),
            ),
          ),
        if (r.closeAuthorizedByName.isNotEmpty)
          ListTile(
            leading: const Icon(Icons.verified_user_outlined),
            title: Text('Autorizó ${r.closeAuthorizedByName}'),
          ),
        const SizedBox(height: 16),
        FilledButton(
          style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
          onPressed: () => Navigator.pop(context, r),
          child: const Text('Listo'),
        ),
      ],
    );
  }

  Widget _row(String k, String v, {Color? color}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(k, style: TextStyle(color: color)),
            Text(v, style: TextStyle(fontWeight: FontWeight.bold, color: color)),
          ],
        ),
      );
}
