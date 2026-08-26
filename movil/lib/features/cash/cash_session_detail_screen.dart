import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/cash_session.dart';
import '../../models/sale_history.dart';
import '../../services/sale_service.dart';
import '../sales/sale_detail_screen.dart';

/// Detalle de una sesión de caja: el arqueo tal como quedó al cierre y, debajo,
/// las ventas del turno.
///
/// Las ventas se piden por `cash_session_id` y no por fecha: un turno que cruza
/// la medianoche, o dos sesiones del mismo cajero en el mismo día, tienen que
/// quedar separados. Cada venta abre el mismo [SaleDetailScreen] del registro
/// de ventas, así que desde acá también se puede devolver.
class CashSessionDetailScreen extends StatefulWidget {
  const CashSessionDetailScreen({super.key, required this.session});

  final CashSession session;

  @override
  State<CashSessionDetailScreen> createState() =>
      _CashSessionDetailScreenState();
}

class _CashSessionDetailScreenState extends State<CashSessionDetailScreen> {
  static final _dayFmt = DateFormat('dd/MM/yyyy');
  static final _dateTimeFmt = DateFormat('dd/MM/yyyy HH:mm');
  static final _timeFmt = DateFormat('HH:mm');

  List<SaleSummary> _sales = const [];
  bool _loading = true;
  String? _error;

  /// Se registró una devolución acá adentro: al volver, el historial de
  /// sesiones tiene que recargarse porque cambiaron los totales del turno.
  bool _changed = false;

  CashSession get _s => widget.session;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await context.read<SaleService>().sessionSales(_s.id);
      setState(() => _sales = res);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _openSale(SaleSummary s) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => SaleDetailScreen(saleId: s.id)),
    );
    if (changed == true) {
      _changed = true;
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.pop(context, _changed);
      },
      child: Scaffold(
        appBar: AppBar(title: Text('Sesión del ${_dayFmt.format(_s.openedAt)}')),
        body: RefreshIndicator(
          onRefresh: _load,
          child: ListView.builder(
            padding: const EdgeInsets.only(bottom: 24, top: 8),
            // Arqueo + encabezado + las ventas (o el estado de la carga).
            itemCount: 2 + (_sales.isEmpty ? 1 : _sales.length),
            itemBuilder: (context, i) {
              if (i == 0) return _arqueoCard();
              if (i == 1) return _salesHeader();
              if (_sales.isEmpty) return _salesPlaceholder();
              return _saleCard(_sales[i - 2]);
            },
          ),
        ),
      ),
    );
  }

  // ── Arqueo ─────────────────────────────────────────────────
  /// Mismo desglose que el diálogo de cerrar caja, pero de lectura: es el
  /// formato que el cajero ya conoce del momento del arqueo.
  Widget _arqueoCard() {
    final diff = _s.cashDifference;
    return Card(
      margin: const EdgeInsets.fromLTRB(12, 4, 12, 8),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _kv('Abierta', _dateTimeFmt.format(_s.openedAt)),
            _kv('Cerrada',
                _s.isOpen ? 'en curso' : _dateTimeFmt.format(_s.closedAt!)),
            if (_s.userFullName.isNotEmpty) _kv('Cajero', _s.userFullName),
            const Divider(height: 20),
            _kv('Fondo inicial', currency(_s.openingAmount)),
            _kv('Ventas', currency(_s.totalSales)),
            if (_s.totalCashSales != _s.totalSales)
              _kv('  en efectivo', currency(_s.totalCashSales)),
            if (_s.totalExpenses > 0)
              _kv('Gastos', '− ${currency(_s.totalExpenses)}'),
            if (_s.totalWithdrawals > 0)
              _kv('Retiros', '− ${currency(_s.totalWithdrawals)}'),
            if (_s.totalIncome > 0) _kv('Ingresos', currency(_s.totalIncome)),
            if (_s.totalReturns > 0)
              _kv('Devoluciones', '− ${currency(_s.totalReturns)}',
                  color: Colors.orange),
            const Divider(height: 20),
            _kv(_s.isOpen ? 'Esperado en caja' : 'Esperado',
                currency(_s.expectedCash),
                bold: true),
            if (!_s.isOpen) ...[
              _kv('Declarado', currency(_s.declaredAmount ?? 0), bold: true),
              if (diff != null)
                _kv('Diferencia',
                    '${diff >= 0 ? '+ ' : '− '}${currency(diff.abs())}',
                    bold: true,
                    color: diff == 0
                        ? null
                        : (diff > 0 ? Colors.blue : Colors.red)),
            ],
            if (_s.notes.isNotEmpty) ...[
              const SizedBox(height: 12),
              const Text('Observaciones',
                  style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Colors.grey)),
              const SizedBox(height: 2),
              Text(_s.notes,
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

  // ── Ventas del turno ───────────────────────────────────────
  Widget _salesHeader() {
    // Los totales se suman de la lista y no se toman de la sesión: así siguen
    // siendo coherentes con las filas de abajo apenas se registra una
    // devolución, sin tener que volver a pedir la sesión.
    final neto = _sales.fold<double>(0, (a, s) => a + s.netTotal);
    final devuelto = _sales.fold<double>(0, (a, s) => a + s.totalReturned);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text('Ventas del turno',
                    style:
                        TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
              ),
              if (_sales.isNotEmpty)
                Text('${_sales.length} venta(s) · ${currency(neto)}',
                    style: const TextStyle(
                        fontSize: 13, fontWeight: FontWeight.bold)),
            ],
          ),
          if (devuelto > 0)
            Padding(
              padding: const EdgeInsets.only(top: 2),
              child: Text('Devuelto − ${currency(devuelto)}',
                  style: const TextStyle(fontSize: 11, color: Colors.orange)),
            ),
        ],
      ),
    );
  }

  Widget _salesPlaceholder() {
    if (_loading) {
      return const Padding(
        padding: EdgeInsets.all(24),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_error != null) {
      return Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            const Icon(Icons.cloud_off, size: 40, color: Colors.grey),
            const SizedBox(height: 12),
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Reintentar')),
          ],
        ),
      );
    }
    return const Padding(
      padding: EdgeInsets.symmetric(horizontal: 16, vertical: 24),
      child: Text('No se registraron ventas en esta sesión.',
          textAlign: TextAlign.center, style: TextStyle(color: Colors.grey)),
    );
  }

  Widget _saleCard(SaleSummary s) {
    // Sin el nombre del vendedor: todas las ventas de la sesión son del mismo
    // cajero, que ya está arriba en el arqueo. En su lugar va solo la hora.
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: ListTile(
        onTap: () => _openSale(s),
        title: Text(
          s.customerName.isEmpty ? 'Cliente' : s.customerName,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 2),
            Text(s.saleDate == null ? '—' : _timeFmt.format(s.saleDate!),
                style: const TextStyle(fontSize: 12)),
            const SizedBox(height: 4),
            _statusBadge(s.status),
          ],
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            // Con devoluciones se muestra el neto y el facturado tachado: el
            // importe grande tiene que ser lo que quedó cobrado.
            if (s.totalReturned > 0)
              Text(currency(s.total),
                  style: const TextStyle(
                    fontSize: 11,
                    color: Colors.grey,
                    decoration: TextDecoration.lineThrough,
                  )),
            Text(currency(s.netTotal),
                style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 15,
                    color: s.totalReturned > 0 ? Colors.orange : null)),
            if (s.totalDiscounts > 0)
              Text('− ${currency(s.totalDiscounts)}',
                  style: const TextStyle(fontSize: 11, color: Colors.green)),
          ],
        ),
      ),
    );
  }

  Widget _statusBadge(SaleStatus status) {
    final (Color color, String label) = switch (status) {
      SaleStatus.activa => (Colors.green, 'Activa'),
      SaleStatus.conDevolucion => (Colors.orange, 'Devolución parcial'),
      SaleStatus.devueltaTotal => (Colors.red, 'Devuelta total'),
    };
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
}
