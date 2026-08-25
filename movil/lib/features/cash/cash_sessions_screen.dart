import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/cash_session.dart';
import '../../services/sale_service.dart';

enum _Quick { today, week, month, custom }

/// Historial de sesiones de caja: cuándo se abrió y cerró cada turno, con
/// cuánto, y cómo cerró el arqueo.
///
/// El alcance lo decide el servidor: si el usuario es solo Cajero, la API le
/// devuelve únicamente sus propias sesiones (RolePolicy.VeSoloLoPropio); un
/// supervisor ve las de todos, y por eso la tarjeta muestra el nombre.
class CashSessionsScreen extends StatefulWidget {
  const CashSessionsScreen({super.key});

  @override
  State<CashSessionsScreen> createState() => _CashSessionsScreenState();
}

class _CashSessionsScreenState extends State<CashSessionsScreen> {
  static final _dayFmt = DateFormat('dd/MM/yyyy');
  static final _dateTimeFmt = DateFormat('dd/MM/yyyy HH:mm');
  static final _timeFmt = DateFormat('HH:mm');
  static final _queryFmt = DateFormat('yyyy-MM-dd');

  _Quick _quick = _Quick.today;
  late DateTime _from;
  late DateTime _to;

  List<CashSession> _items = const [];
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _applyQuick(_Quick.today, reload: false);
    _load();
  }

  // ── Rangos rápidos ─────────────────────────────────────────
  void _applyQuick(_Quick q, {bool reload = true}) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    _quick = q;
    switch (q) {
      case _Quick.today:
        _from = today;
        _to = today;
        break;
      case _Quick.week:
        _from = today.subtract(Duration(days: today.weekday - 1));
        _to = today;
        break;
      case _Quick.month:
        _from = DateTime(now.year, now.month, 1);
        _to = today;
        break;
      case _Quick.custom:
        break;
    }
    if (reload) _load();
  }

  Future<void> _pickCustomRange() async {
    final now = DateTime.now();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(now.year - 3),
      lastDate: DateTime(now.year, now.month, now.day),
      initialDateRange: DateTimeRange(start: _from, end: _to),
    );
    if (picked != null) {
      setState(() {
        _quick = _Quick.custom;
        _from = picked.start;
        _to = picked.end;
      });
      _load();
    }
  }

  // ── Carga ──────────────────────────────────────────────────
  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await context.read<SaleService>().cashSessions(
            dateFrom: _queryFmt.format(_from),
            dateTo: _queryFmt.format(_to),
          );
      setState(() => _items = res);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Sesiones de caja')),
      body: Column(
        children: [
          _filterBar(),
          const Divider(height: 1),
          Expanded(child: _body()),
        ],
      ),
    );
  }

  // ── UI ─────────────────────────────────────────────────────
  Widget _filterBar() {
    final rangeLabel = _from == _to
        ? _dayFmt.format(_from)
        : '${_dayFmt.format(_from)} — ${_dayFmt.format(_to)}';
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _chip('Hoy', _Quick.today),
                const SizedBox(width: 8),
                _chip('Semana', _Quick.week),
                const SizedBox(width: 8),
                _chip('Mes', _Quick.month),
                const SizedBox(width: 8),
                ActionChip(
                  avatar: const Icon(Icons.date_range, size: 18),
                  label: const Text('Rango'),
                  onPressed: _pickCustomRange,
                ),
              ],
            ),
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              const Icon(Icons.calendar_today, size: 14, color: Colors.grey),
              const SizedBox(width: 6),
              Expanded(
                child: Text(rangeLabel,
                    style: const TextStyle(fontSize: 13, color: Colors.grey)),
              ),
              if (_items.isNotEmpty)
                Text(
                  '${_items.length} sesión(es)',
                  style: const TextStyle(
                      fontSize: 13, fontWeight: FontWeight.bold),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _chip(String label, _Quick q) => ChoiceChip(
        label: Text(label),
        selected: _quick == q,
        onSelected: (_) => _applyQuick(q),
      );

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off, size: 48, color: Colors.grey),
            const SizedBox(height: 12),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Text(_error!, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Reintentar')),
          ],
        ),
      );
    }
    if (_items.isEmpty) {
      return const Center(
        child: Text('No hay sesiones de caja en el período seleccionado.'),
      );
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.builder(
        padding: const EdgeInsets.only(bottom: 16, top: 4),
        itemCount: _items.length,
        itemBuilder: (context, i) => _sessionCard(_items[i]),
      ),
    );
  }

  Widget _sessionCard(CashSession s) {
    // Una caja abierta la devuelve la API esté o no dentro del rango pedido: es
    // a propósito, el turno en curso siempre tiene que verse.
    final cierre = s.isOpen
        ? 'en curso'
        : (s.closedAt!.day == s.openedAt.day &&
                s.closedAt!.month == s.openedAt.month &&
                s.closedAt!.year == s.openedAt.year
            ? _timeFmt.format(s.closedAt!)
            : _dateTimeFmt.format(s.closedAt!));

    // A propósito no es un ListTile: su `trailing` recibe una altura acotada y
    // con el texto del sistema ampliado la columna del importe desbordaba.
    return Card(
      child: InkWell(
        onTap: () => _openArqueo(s),
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(
                      '${_dateTimeFmt.format(s.openedAt)}  →  $cierre',
                      style: const TextStyle(
                          fontWeight: FontWeight.bold, fontSize: 14),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(currency(s.totalSales),
                          style: const TextStyle(
                              fontWeight: FontWeight.bold, fontSize: 15)),
                      const Text('ventas',
                          style:
                              TextStyle(fontSize: 11, color: Colors.grey)),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 4),
              Text(
                'Abrió con ${currency(s.openingAmount)}'
                '${s.isOpen ? '' : '  ·  cerró con ${currency(s.declaredAmount ?? 0)}'}',
                style: const TextStyle(fontSize: 12),
              ),
              if (s.userFullName.isNotEmpty)
                Text(s.userFullName,
                    style: const TextStyle(fontSize: 12, color: Colors.grey)),
              const SizedBox(height: 6),
              _statusBadge(s),
            ],
          ),
        ),
      ),
    );
  }

  /// Abierta / Cuadró / Sobrante / Faltante. El sobrante y el faltante son la
  /// única cosa que hay que poder ver de un vistazo en el historial.
  Widget _statusBadge(CashSession s) {
    final Color color;
    final String label;
    final diff = s.cashDifference;
    if (s.isOpen) {
      color = Colors.green;
      label = 'Abierta';
    } else if (diff == null || diff == 0) {
      color = Colors.blueGrey;
      label = 'Cuadró';
    } else if (diff > 0) {
      color = Colors.blue;
      label = 'Sobrante ${currency(diff)}';
    } else {
      color = Colors.red;
      label = 'Faltante ${currency(diff.abs())}';
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(label,
          style: TextStyle(
              fontSize: 11, color: color, fontWeight: FontWeight.w600)),
    );
  }

  // ── Arqueo ─────────────────────────────────────────────────
  /// Mismo desglose que el diálogo de cerrar caja, pero de lectura: es el
  /// formato que el cajero ya conoce del momento del arqueo.
  Future<void> _openArqueo(CashSession s) async {
    final diff = s.cashDifference;
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Sesión del ${_dayFmt.format(s.openedAt)}',
                style: const TextStyle(
                    fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            _kv('Abierta', _dateTimeFmt.format(s.openedAt)),
            _kv('Cerrada',
                s.isOpen ? 'en curso' : _dateTimeFmt.format(s.closedAt!)),
            if (s.userFullName.isNotEmpty) _kv('Cajero', s.userFullName),
            const Divider(height: 20),
            _kv('Fondo inicial', currency(s.openingAmount)),
            _kv('Ventas', currency(s.totalSales)),
            if (s.totalCashSales != s.totalSales)
              _kv('  en efectivo', currency(s.totalCashSales)),
            if (s.totalExpenses > 0)
              _kv('Gastos', '− ${currency(s.totalExpenses)}'),
            if (s.totalWithdrawals > 0)
              _kv('Retiros', '− ${currency(s.totalWithdrawals)}'),
            if (s.totalIncome > 0) _kv('Ingresos', currency(s.totalIncome)),
            if (s.totalReturns > 0)
              _kv('Devoluciones', '− ${currency(s.totalReturns)}',
                  color: Colors.orange),
            const Divider(height: 20),
            _kv(s.isOpen ? 'Esperado en caja' : 'Esperado',
                currency(s.expectedCash),
                bold: true),
            if (!s.isOpen) ...[
              _kv('Declarado', currency(s.declaredAmount ?? 0), bold: true),
              if (diff != null)
                _kv('Diferencia',
                    '${diff >= 0 ? '+ ' : '− '}${currency(diff.abs())}',
                    bold: true,
                    color: diff == 0
                        ? null
                        : (diff > 0 ? Colors.blue : Colors.red)),
            ],
            if (s.notes.isNotEmpty) ...[
              const SizedBox(height: 12),
              const Text('Observaciones',
                  style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Colors.grey)),
              const SizedBox(height: 2),
              Text(s.notes,
                  style: const TextStyle(
                      fontSize: 13, fontStyle: FontStyle.italic)),
            ],
          ],
        ),
      ),
    );
  }

  Widget _kv(String label, String value, {bool bold = false, Color? color}) =>
      Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Flexible(
              child: Text(label,
                  style: TextStyle(
                      fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
            ),
            const SizedBox(width: 12),
            Text(value,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal,
                    color: color)),
          ],
        ),
      );
}
