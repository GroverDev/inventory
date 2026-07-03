import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/sale_history.dart';
import '../../services/sale_service.dart';

class SaleDetailScreen extends StatefulWidget {
  const SaleDetailScreen({super.key, required this.saleId});
  final String saleId;

  @override
  State<SaleDetailScreen> createState() => _SaleDetailScreenState();
}

class _SaleDetailScreenState extends State<SaleDetailScreen> {
  static final _dateFmt = DateFormat('dd/MM/yyyy HH:mm');

  SaleFull? _sale;
  bool _loading = true;
  String? _error;

  /// Se registró una devolución: al volver, la lista debe refrescarse.
  bool _changed = false;

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
      final sale = await context.read<SaleService>().getSaleById(widget.saleId);
      setState(() => _sale = sale);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _snack(String m) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(m)));
  }

  bool get _canReturn {
    final sale = _sale;
    if (sale == null || !sale.isActive) return false;
    // Hay unidades disponibles para devolver en al menos una línea.
    return sale.detail.any((d) => d.quantity - sale.returnedFor(d.id) > 0);
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.pop(context, _changed);
      },
      child: Scaffold(
        appBar: AppBar(title: const Text('Detalle de venta')),
        body: _body(),
        floatingActionButton: _canReturn
            ? FloatingActionButton.extended(
                onPressed: _openReturnSheet,
                icon: const Icon(Icons.assignment_return_outlined),
                label: const Text('Devolución'),
              )
            : null,
      ),
    );
  }

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
    final sale = _sale;
    if (sale == null) {
      return const Center(child: Text('Venta no encontrada.'));
    }

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 88),
      children: [
        _header(sale),
        const SizedBox(height: 16),
        _totals(sale),
        if (sale.payments.isNotEmpty) ...[
          const SizedBox(height: 16),
          _sectionTitle('Cobro'),
          const SizedBox(height: 8),
          _payments(sale),
        ],
        const SizedBox(height: 16),
        _sectionTitle('Productos'),
        const SizedBox(height: 8),
        ...sale.detail.map((d) => _productTile(sale, d)),
        if (sale.hasReturns) ...[
          const SizedBox(height: 16),
          _sectionTitle('Historial de devoluciones'),
          const SizedBox(height: 8),
          ...sale.returns.map(_returnTile),
        ],
      ],
    );
  }

  // ── Secciones ──────────────────────────────────────────────
  Widget _header(SaleFull sale) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                sale.customerName.isEmpty ? 'Cliente' : sale.customerName,
                style: const TextStyle(
                    fontSize: 18, fontWeight: FontWeight.bold),
              ),
            ),
            _statusBadge(sale),
          ],
        ),
        const SizedBox(height: 4),
        Text(
          '${sale.saleDate == null ? '—' : _dateFmt.format(sale.saleDate!)}'
          '   ·   ${sale.sellerName.isEmpty ? '—' : sale.sellerName}',
          style: const TextStyle(fontSize: 13, color: Colors.grey),
        ),
      ],
    );
  }

  Widget _totals(SaleFull sale) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        children: [
          _kv('Subtotal', currency(sale.subtotal)),
          if (sale.totalDiscounts > 0)
            _kv('Descuentos', '− ${currency(sale.totalDiscounts)}',
                color: Colors.green),
          _kv('Total', currency(sale.total), bold: true),
          if (sale.hasReturns) ...[
            const Divider(height: 16),
            _kv('Devuelto', '− ${currency(sale.totalReturned)}',
                color: Colors.orange),
            _kv('Neto final', currency(sale.netTotal),
                bold: true, color: Theme.of(context).colorScheme.primary),
          ],
        ],
      ),
    );
  }

  Widget _payments(SaleFull sale) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: sale.payments.map((p) {
        return Chip(
          avatar: const Icon(Icons.payments_outlined, size: 18),
          label: Text(
            '${p.paymentMethodName.isEmpty ? 'Pago' : p.paymentMethodName}'
            ': ${currency(p.amountGiven)}'
            '${p.amountReturned > 0 ? ' · vuelto ${currency(p.amountReturned)}' : ''}',
          ),
        );
      }).toList(),
    );
  }

  Widget _productTile(SaleFull sale, SaleDetailItem d) {
    final returned = sale.returnedFor(d.id);
    final net = d.quantity - returned;
    final fullyReturned = net == 0;
    return Card(
      child: ListTile(
        dense: true,
        title: Text(
          d.productName,
          style: TextStyle(
            fontWeight: FontWeight.w600,
            decoration:
                fullyReturned ? TextDecoration.lineThrough : null,
            color: fullyReturned ? Colors.grey : null,
          ),
        ),
        subtitle: Text(
          '$net × ${currency(d.unitPrice)}'
          '${returned > 0 ? '   ·   $returned devuelto${returned > 1 ? 's' : ''}' : ''}'
          '${d.lineTotalDiscounts > 0 ? '   ·   desc. ${currency(d.lineTotalDiscounts)}' : ''}',
          style: const TextStyle(fontSize: 12),
        ),
        trailing: Text(
          currency(net * d.unitPrice),
          style: TextStyle(
            fontWeight: FontWeight.bold,
            decoration: fullyReturned ? TextDecoration.lineThrough : null,
            color: fullyReturned ? Colors.grey : null,
          ),
        ),
      ),
    );
  }

  Widget _returnTile(SaleReturnInfo r) {
    return Card(
      color: Colors.orange.withValues(alpha: 0.06),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: (r.isFullReturn ? Colors.red : Colors.orange)
                        .withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    r.isFullReturn ? 'Total' : 'Parcial',
                    style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                        color: r.isFullReturn ? Colors.red : Colors.orange),
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  r.returnDate == null ? '' : _dateFmt.format(r.returnDate!),
                  style: const TextStyle(fontSize: 12, color: Colors.grey),
                ),
                const Spacer(),
                Text('− ${currency(r.totalReturned)}',
                    style: const TextStyle(
                        fontWeight: FontWeight.bold, color: Colors.red)),
              ],
            ),
            if (r.reason != null && r.reason!.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(r.reason!,
                  style: const TextStyle(
                      fontSize: 12,
                      fontStyle: FontStyle.italic,
                      color: Colors.grey)),
            ],
            const SizedBox(height: 6),
            ...r.detail.map((d) => Text(
                  '· ${d.productName}  ×${d.quantityReturned}  (${currency(d.lineTotal)})',
                  style: const TextStyle(fontSize: 12),
                )),
          ],
        ),
      ),
    );
  }

  // ── Devolución ─────────────────────────────────────────────
  Future<void> _openReturnSheet() async {
    final sale = _sale!;
    // Estado local de la hoja: cantidad a devolver por línea.
    final qty = <String, int>{for (final d in sale.detail) d.id: 0};
    final reasonCtrl = TextEditingController();

    final confirmed = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(sheetContext).viewInsets.bottom,
        ),
        child: StatefulBuilder(
          builder: (context, setSheet) {
            double total = 0;
            for (final d in sale.detail) {
              total += (qty[d.id] ?? 0) * d.unitPrice;
            }

            return SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text('Registrar devolución',
                      style: TextStyle(
                          fontSize: 16, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 12),
                  TextField(
                    controller: reasonCtrl,
                    maxLength: 255,
                    decoration: const InputDecoration(
                      labelText: 'Motivo (opcional)',
                      hintText: 'Ej: producto en mal estado…',
                    ),
                  ),
                  const SizedBox(height: 4),
                  ...sale.detail.map((d) {
                    final already = sale.returnedFor(d.id);
                    final available = d.quantity - already;
                    final current = qty[d.id] ?? 0;
                    return Padding(
                      padding: const EdgeInsets.symmetric(vertical: 4),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(d.productName,
                                    maxLines: 2,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(
                                        fontWeight: FontWeight.w600)),
                                Text(
                                  'Disponible: $available'
                                  '${already > 0 ? '  ·  ya devuelto: $already' : ''}',
                                  style: const TextStyle(
                                      fontSize: 12, color: Colors.grey),
                                ),
                              ],
                            ),
                          ),
                          _stepper(
                            value: current,
                            max: available,
                            onChanged: (v) => setSheet(() => qty[d.id] = v),
                          ),
                        ],
                      ),
                    );
                  }),
                  const Divider(),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Total a devolver',
                          style: TextStyle(fontWeight: FontWeight.bold)),
                      Text(currency(total),
                          style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                              color: Colors.red)),
                    ],
                  ),
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    onPressed: total <= 0
                        ? null
                        : () => Navigator.pop(sheetContext, true),
                    icon: const Icon(Icons.check),
                    label: const Text('Confirmar devolución'),
                  ),
                  const SizedBox(height: 12),
                ],
              ),
            );
          },
        ),
      ),
    );

    if (confirmed == true) {
      await _submitReturn(qty, reasonCtrl.text.trim());
    }
    reasonCtrl.dispose();
  }

  Widget _stepper({
    required int value,
    required int max,
    required ValueChanged<int> onChanged,
  }) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.remove_circle_outline),
          onPressed: value > 0 ? () => onChanged(value - 1) : null,
        ),
        SizedBox(
          width: 24,
          child: Text('$value',
              textAlign: TextAlign.center,
              style: const TextStyle(fontWeight: FontWeight.bold)),
        ),
        IconButton(
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.add_circle_outline),
          onPressed: value < max ? () => onChanged(value + 1) : null,
        ),
      ],
    );
  }

  Future<void> _submitReturn(Map<String, int> qty, String reason) async {
    final sale = _sale!;
    final detail = <SaleReturnDetailRequest>[];
    for (final d in sale.detail) {
      final q = qty[d.id] ?? 0;
      if (q > 0) {
        detail.add(SaleReturnDetailRequest(
          saleDetailId: d.id,
          productId: d.productId,
          quantityReturned: q,
          unitPrice: d.unitPrice,
        ));
      }
    }
    if (detail.isEmpty) return;

    try {
      final msg = await context.read<SaleService>().createReturn(
            SaleReturnRequest(
              saleId: sale.id,
              reason: reason.isEmpty ? null : reason,
              detail: detail,
            ),
          );
      _changed = true;
      _snack(msg.isEmpty ? 'Devolución registrada correctamente.' : msg);
      await _load();
    } on ApiException catch (e) {
      _snack(e.message);
    }
  }

  // ── Helpers UI ─────────────────────────────────────────────
  Widget _sectionTitle(String t) => Text(
        t.toUpperCase(),
        style: const TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.bold,
            letterSpacing: 0.6,
            color: Colors.grey),
      );

  Widget _kv(String label, String value, {bool bold = false, Color? color}) =>
      Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
            Text(value,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal,
                    color: color)),
          ],
        ),
      );

  Widget _statusBadge(SaleFull sale) {
    final Color color;
    final String label;
    if (!sale.isActive) {
      color = Colors.red;
      label = 'Devuelta total';
    } else if (sale.hasReturns) {
      color = Colors.orange;
      label = 'Devolución parcial';
    } else {
      color = Colors.green;
      label = 'Activa';
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(label,
          style: TextStyle(
              fontSize: 12, color: color, fontWeight: FontWeight.w600)),
    );
  }
}
