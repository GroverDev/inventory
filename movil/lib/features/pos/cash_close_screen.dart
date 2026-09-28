import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/cash_session.dart';
import '../../models/catalog.dart';
import '../../services/sale_service.dart';
import 'cash_close_calc.dart';
import 'pos_dialogs.dart';

/// Cierre de caja: arqueo por medio de pago, a ciegas — se declara lo
/// contado sin ver lo esperado, y el servidor compara y avisa la diferencia
/// recién al confirmar. Espejo del modal "Cerrar Caja" de
/// `PointOfSaleView.vue`.
///
/// Devuelve `true` si la caja quedó cerrada, o `null`/`false` si se canceló.
class CashCloseScreen extends StatefulWidget {
  const CashCloseScreen({
    super.key,
    required this.session,
    required this.paymentMethods,
  });

  final CashSession session;
  final List<PaymentMethod> paymentMethods;

  @override
  State<CashCloseScreen> createState() => _CashCloseScreenState();
}

class _CashCloseScreenState extends State<CashCloseScreen> {
  CashCloseSettings _settings = const CashCloseSettings();
  bool _loadingSettings = true;

  /// Lo declarado por medio, por id. `null` = sin completar.
  final Map<String, double?> _declared = {};

  /// Conteo del efectivo por billete y moneda, por valor. `null` = sin tocar.
  final Map<double, int?> _denomQty = {};
  bool _showDenominations = false;

  // Un controller por campo, creado una sola vez: reasignar `.text` en vez de
  // instanciar uno nuevo en cada build es lo que mantiene el cursor donde el
  // cajero está escribiendo (ver `openCashDialog` en pos_dialogs.dart, mismo
  // criterio).
  final Map<String, TextEditingController> _countCtrls = {};
  final Map<double, TextEditingController> _denomCtrls = {};
  final _notesCtrl = TextEditingController();
  bool _saving = false;
  String _supervisorToken = '';

  /// Medios que se arquean: el efectivo siempre, los demás si se configuró.
  List<PaymentMethod> get _counted =>
      widget.paymentMethods.where((m) => m.isCounted).toList();

  double get _denomTotal => denominationTotal(_denomQty);

  /// Mientras se cuenta por billetes, el efectivo declarado es su suma.
  bool get _countingByDenomination => countingByDenomination(
      requiredByPolicy: _settings.requireDenominations,
      toggledOn: _showDenominations,
      total: _denomTotal);

  String? get _cashMethodId {
    for (final m in widget.paymentMethods) {
      if (m.affectsCash) return m.id;
    }
    return null;
  }

  @override
  void initState() {
    super.initState();
    _loadSettings();
  }

  @override
  void dispose() {
    _notesCtrl.dispose();
    for (final c in _countCtrls.values) {
      c.dispose();
    }
    for (final c in _denomCtrls.values) {
      c.dispose();
    }
    super.dispose();
  }

  TextEditingController _countCtrl(String methodId) =>
      _countCtrls.putIfAbsent(methodId, () => TextEditingController());

  TextEditingController _denomCtrl(double value) =>
      _denomCtrls.putIfAbsent(value, () => TextEditingController());

  Future<void> _loadSettings() async {
    setState(() => _loadingSettings = true);
    try {
      final s = await context.read<SaleService>().closeSettings();
      if (!mounted) return;
      setState(() => _settings = s);
      if (s.requireDenominations) _syncCashFromDenominations();
    } on ApiException {
      // Silencioso: se usan los valores por defecto (sin tope de observación
      // salvo el general) mientras no se pueda leer la configuración.
    } finally {
      if (mounted) setState(() => _loadingSettings = false);
    }
  }

  void _onDenomChanged(double value, String text) {
    setState(() => _denomQty[value] = int.tryParse(text.trim()));
    _syncCashFromDenominations();
  }

  void _syncCashFromDenominations() {
    final id = _cashMethodId;
    if (id == null) return;
    setState(() {
      _declared[id] = _denomTotal;
      _countCtrl(id).text = _fmt(_denomTotal);
    });
  }

  /// De dónde sale lo que se declara en cada medio. Espejo de `countHint` en
  /// `PointOfSaleView.vue`.
  String _countHint(PaymentMethod m) {
    if (m.affectsCash) return 'lo contado en el cajón, con el fondo inicial';
    final n = m.name.toLowerCase();
    if (n.contains('tarjeta') || n.contains('pos') || n.contains('datafono') ||
        n.contains('datáfono')) {
      return 'total del cierre de lote del datáfono';
    }
    return 'lo recibido en la cuenta';
  }

  Future<void> _submit() async {
    final faltan =
        _counted.where((m) => _declared[m.id] == null).toList();
    if (faltan.isNotEmpty) {
      _snack('Declare ${faltan.map((m) => m.name).join(', ')} (0 si no hubo).');
      return;
    }
    setState(() => _saving = true);
    try {
      final usaBilletes = _countingByDenomination;
      final result = await context.read<SaleService>().closeSession(
            widget.session.id,
            counts: buildCounts(_declared),
            denominations: usaBilletes ? buildDenominations(_denomQty) : const [],
            notes: _notesCtrl.text.trim(),
            supervisorAuthToken:
                _supervisorToken.isEmpty ? null : _supervisorToken,
          );
      // La autorización valió para este intento y nada más.
      _supervisorToken = '';
      if (!mounted) return;
      await _showResult(result.counts);
      if (mounted) Navigator.pop(context, true);
    } on ApiException catch (e) {
      _supervisorToken = '';
      final requires =
          e.data is Map ? (e.data as Map)['CloseRequires']?.toString() : null;
      if (requires == 'supervisor' && _notesCtrl.text.trim().isNotEmpty) {
        // La observación ya está: falta la firma.
        final token = await supervisorAuthDialog(
          context,
          context.read<SaleService>(),
          reason: e.message,
        );
        if (token != null) {
          _supervisorToken = token;
          if (mounted) await _submit();
          return;
        }
      } else {
        // Cualquier otro rechazo (falta la observación, billetes que no
        // suman) se muestra tal cual: el cajero corrige y reintenta.
        _snack(e.message);
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  /// Resultado del arqueo, recién visible después de cerrar — antes de eso el
  /// conteo es a ciegas.
  Future<void> _showResult(List<CashCount> counts) {
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (_) => AlertDialog(
        title: const Text('Caja cerrada'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (final c in counts)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(c.name, style: const TextStyle(fontWeight: FontWeight.bold)),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text('Esperado ${currency(c.expected)}'),
                          Text('Declarado ${currency(c.declared)}'),
                        ],
                      ),
                      if (c.difference != 0)
                        Text(
                          c.difference > 0
                              ? 'Sobrante ${currency(c.difference)}'
                              : 'Faltante ${currency(c.difference.abs())}',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            color: c.difference > 0 ? Colors.blue : Colors.red,
                          ),
                        )
                      else
                        const Text('Cuadró', style: TextStyle(color: Colors.grey)),
                      const Divider(height: 16),
                    ],
                  ),
                ),
            ],
          ),
        ),
        actions: [
          FilledButton(
              onPressed: () => Navigator.pop(context), child: const Text('Aceptar')),
        ],
      ),
    );
  }

  void _snack(String m) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(m)));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Cerrar caja')),
      body: _loadingSettings
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
              children: [
                const Text(
                  'Cuente y declare lo que tiene. El sistema compara con lo '
                  'esperado recién al confirmar el cierre.',
                  style: TextStyle(fontSize: 13, color: Colors.grey),
                ),
                const SizedBox(height: 16),
                for (final m in _counted) _methodField(m),
                const SizedBox(height: 8),
                _denominationsSection(),
                const SizedBox(height: 16),
                TextField(
                  controller: _notesCtrl,
                  maxLines: 3,
                  decoration: const InputDecoration(
                    labelText: 'Observaciones',
                    hintText: 'Motivo del faltante o sobrante, si lo hay',
                  ),
                ),
              ],
            ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: FilledButton.icon(
            onPressed: _saving ? null : _submit,
            icon: _saving
                ? const SizedBox(
                    height: 18,
                    width: 18,
                    child: CircularProgressIndicator(strokeWidth: 2))
                : const Icon(Icons.lock),
            label: const Text('Cerrar y arquear'),
          ),
        ),
      ),
    );
  }

  Widget _methodField(PaymentMethod m) {
    // Mientras se cuenta por billetes, el campo de efectivo lo llena el
    // desglose y no se edita a mano — para que no queden dos fuentes de
    // verdad para el mismo número.
    final locked = m.affectsCash && _countingByDenomination;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextField(
        enabled: !locked,
        controller: _countCtrl(m.id),
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        onChanged: (v) =>
            setState(() => _declared[m.id] = double.tryParse(v.trim())),
        decoration: InputDecoration(
          labelText: '${m.name} contado (Bs.)',
          helperText: _countHint(m),
        ),
      ),
    );
  }

  Widget _denominationsSection() {
    if (_cashMethodId == null) return const SizedBox.shrink();
    final show = _settings.requireDenominations || _showDenominations;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (!_settings.requireDenominations)
          TextButton.icon(
            onPressed: () => setState(() {
              _showDenominations = !_showDenominations;
              if (_showDenominations) _syncCashFromDenominations();
            }),
            icon: Icon(show ? Icons.expand_less : Icons.payments_outlined),
            label: Text(show
                ? 'Ocultar conteo por billetes'
                : 'Contar el efectivo por billetes y monedas'),
          ),
        if (show) ...[
          const Text('Efectivo por denominación',
              style: TextStyle(fontSize: 12, color: Colors.grey)),
          const SizedBox(height: 6),
          Wrap(
            spacing: 10,
            runSpacing: 10,
            children: [
              for (final v in bobDenominations)
                SizedBox(
                  width: 90,
                  child: TextField(
                    controller: _denomCtrl(v),
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                      labelText: currency(v),
                      isDense: true,
                    ),
                    onChanged: (t) => _onDenomChanged(v, t),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 6),
          Text('Total contado: ${currency(_denomTotal)}',
              style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ],
    );
  }

  String _fmt(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toStringAsFixed(2);
}
