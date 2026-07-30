import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../models/cash_session.dart';
import '../../models/discount.dart';
import '../../providers/cart_provider.dart';
import '../../services/sale_service.dart';

/// ── Descartar la venta en curso ─────────────────────────────
///
/// Muestra qué se pierde (cantidad y total) para que la confirmación sea
/// informada y no un reflejo. Ninguno de los dos botones dice solo "Cancelar":
/// en un diálogo titulado "Descartar venta" eso volvería ambiguo cuál cancela
/// la venta y cuál cancela el diálogo.
Future<bool> confirmDiscardSale(BuildContext context, CartProvider cart) {
  final items = cart.itemCount;
  return confirm(
    context,
    title: 'Descartar venta',
    message: 'Se quitarán $items ${items == 1 ? 'producto' : 'productos'} '
        'por ${currency(cart.total)}. No se puede deshacer.',
    cancelLabel: 'Seguir vendiendo',
    confirmLabel: 'Descartar venta',
    destructive: true,
  );
}

/// Resultado de elegir un descuento (línea o cabecera).
class DiscountResult {
  final String id;
  final String label;
  final String type; // 'Percentage' | 'FixedAmount'
  final double value;

  const DiscountResult({
    required this.id,
    required this.label,
    required this.type,
    required this.value,
  });
}

/// ── Abrir caja ──────────────────────────────────────────────
Future<double?> openCashDialog(BuildContext context) {
  final ctrl = TextEditingController(text: '0');
  return showDialog<double>(
    context: context,
    builder: (_) => AlertDialog(
      title: const Text('Abrir caja'),
      content: TextField(
        controller: ctrl,
        autofocus: true,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        decoration: const InputDecoration(
          labelText: 'Fondo inicial (Bs.)',
          helperText: 'Efectivo con el que inicias el turno.',
        ),
      ),
      actions: [
        TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancelar')),
        FilledButton(
          onPressed: () =>
              Navigator.pop(context, double.tryParse(ctrl.text.trim()) ?? 0),
          child: const Text('Abrir'),
        ),
      ],
    ),
  );
}

/// ── Cerrar caja (arqueo) ────────────────────────────────────
Future<({double declaredAmount, String notes})?> closeCashDialog(
    BuildContext context, CashSession session) {
  final amountCtrl = TextEditingController();
  final notesCtrl = TextEditingController();
  return showDialog<({double declaredAmount, String notes})>(
    context: context,
    builder: (_) => StatefulBuilder(
      builder: (context, setState) {
        final declared = double.tryParse(amountCtrl.text.trim());
        final diff = declared == null ? null : declared - session.expectedCash;
        return AlertDialog(
          title: const Text('Cerrar caja'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _kv('Fondo inicial', currency(session.openingAmount)),
                _kv('Ventas', currency(session.totalSales)),
                if (session.totalExpenses > 0)
                  _kv('Gastos', '− ${currency(session.totalExpenses)}'),
                if (session.totalWithdrawals > 0)
                  _kv('Retiros', '− ${currency(session.totalWithdrawals)}'),
                if (session.totalIncome > 0)
                  _kv('Ingresos', currency(session.totalIncome)),
                const Divider(),
                _kv('Esperado en caja', currency(session.expectedCash),
                    bold: true),
                const SizedBox(height: 12),
                TextField(
                  controller: amountCtrl,
                  autofocus: true,
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  decoration:
                      const InputDecoration(labelText: 'Monto físico contado (Bs.)'),
                  onChanged: (_) => setState(() {}),
                ),
                if (diff != null) ...[
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Diferencia'),
                      Text(
                        currency(diff),
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: diff >= 0 ? Colors.green : Colors.red,
                        ),
                      ),
                    ],
                  ),
                ],
                const SizedBox(height: 8),
                TextField(
                  controller: notesCtrl,
                  maxLines: 2,
                  decoration:
                      const InputDecoration(labelText: 'Observaciones (opcional)'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Cancelar')),
            FilledButton(
              onPressed: declared == null
                  ? null
                  : () => Navigator.pop(context, (
                        declaredAmount: declared,
                        notes: notesCtrl.text.trim(),
                      )),
              child: const Text('Cerrar y arquear'),
            ),
          ],
        );
      },
    ),
  );
}

/// ── Registrar movimiento ────────────────────────────────────
Future<({String type, double amount, String description})?> movementDialog(
    BuildContext context) {
  String type = 'expense';
  final amountCtrl = TextEditingController();
  final descCtrl = TextEditingController();
  const types = [
    ('expense', 'Gasto'),
    ('withdrawal', 'Retiro'),
    ('income', 'Ingreso'),
  ];
  return showDialog<({String type, double amount, String description})>(
    context: context,
    builder: (_) => StatefulBuilder(
      builder: (context, setState) => AlertDialog(
        title: const Text('Registrar movimiento'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SegmentedButton<String>(
              segments: [
                for (final t in types)
                  ButtonSegment(value: t.$1, label: Text(t.$2)),
              ],
              selected: {type},
              onSelectionChanged: (s) => setState(() => type = s.first),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: amountCtrl,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Monto (Bs.)'),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: descCtrl,
              decoration: const InputDecoration(labelText: 'Descripción *'),
            ),
          ],
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancelar')),
          FilledButton(
            onPressed: () {
              final amount = double.tryParse(amountCtrl.text.trim()) ?? 0;
              final desc = descCtrl.text.trim();
              if (amount <= 0 || desc.isEmpty) return;
              Navigator.pop(
                  context, (type: type, amount: amount, description: desc));
            },
            child: const Text('Guardar'),
          ),
        ],
      ),
    ),
  );
}

/// ── Elegir descuento (catálogo o manual) ────────────────────
Future<DiscountResult?> pickDiscount(
  BuildContext context, {
  required double baseAmount,
  required List<Discount> catalog,
  required String title,
}) {
  var mode = catalog.isEmpty ? 'manual' : 'catalog';
  String? selectedId;
  String manualType = 'Percentage';
  final manualCtrl = TextEditingController();

  double previewFor(String type, double value) {
    if (value <= 0) return 0;
    return type == 'Percentage'
        ? math.min(baseAmount * value / 100, baseAmount)
        : math.min(value, baseAmount);
  }

  return showModalBottomSheet<DiscountResult>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => Padding(
      padding: EdgeInsets.only(
          bottom: MediaQuery.of(context).viewInsets.bottom,
          left: 16,
          right: 16),
      child: StatefulBuilder(
        builder: (context, setState) {
          final manualValue = double.tryParse(manualCtrl.text.trim()) ?? 0;
          double preview;
          if (mode == 'catalog') {
            Discount? d;
            for (final x in catalog) {
              if (x.id == selectedId) {
                d = x;
                break;
              }
            }
            preview = d == null ? 0 : previewFor(d.type, d.value);
          } else {
            preview = previewFor(manualType, manualValue);
          }
          final canApply = mode == 'catalog' ? selectedId != null : manualValue > 0;

          return Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(title,
                  style: const TextStyle(
                      fontSize: 16, fontWeight: FontWeight.bold)),
              const SizedBox(height: 4),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Base'),
                  Text(currency(baseAmount),
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                ],
              ),
              const SizedBox(height: 8),
              SegmentedButton<String>(
                segments: const [
                  ButtonSegment(value: 'catalog', label: Text('Predefinido')),
                  ButtonSegment(value: 'manual', label: Text('Manual')),
                ],
                selected: {mode},
                onSelectionChanged: (s) => setState(() => mode = s.first),
              ),
              const SizedBox(height: 12),
              if (mode == 'catalog')
                ConstrainedBox(
                  constraints: const BoxConstraints(maxHeight: 280),
                  child: catalog.isEmpty
                      ? const Padding(
                          padding: EdgeInsets.all(24),
                          child: Text('Sin descuentos configurados.',
                              textAlign: TextAlign.center),
                        )
                      : ListView(
                          shrinkWrap: true,
                          children: [
                            for (final d in catalog)
                              RadioListTile<String>(
                                value: d.id,
                                groupValue: selectedId,
                                onChanged: (v) =>
                                    setState(() => selectedId = v),
                                title: Text(d.name),
                                subtitle: d.description.isEmpty
                                    ? null
                                    : Text(d.description),
                                secondary: Text(d.valueLabel,
                                    style: const TextStyle(
                                        fontWeight: FontWeight.bold)),
                              ),
                          ],
                        ),
                )
              else
                Column(
                  children: [
                    SegmentedButton<String>(
                      segments: const [
                        ButtonSegment(
                            value: 'Percentage', label: Text('Porcentaje')),
                        ButtonSegment(
                            value: 'FixedAmount', label: Text('Monto fijo')),
                      ],
                      selected: {manualType},
                      onSelectionChanged: (s) =>
                          setState(() => manualType = s.first),
                    ),
                    const SizedBox(height: 8),
                    TextField(
                      controller: manualCtrl,
                      keyboardType: const TextInputType.numberWithOptions(
                          decimal: true),
                      decoration: InputDecoration(
                        labelText: manualType == 'Percentage'
                            ? 'Porcentaje (%)'
                            : 'Monto (Bs.)',
                      ),
                      onChanged: (_) => setState(() {}),
                    ),
                  ],
                ),
              if (preview > 0)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text('Ahorro: ${currency(preview)}',
                      style: const TextStyle(
                          color: Colors.green, fontWeight: FontWeight.w600)),
                ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: !canApply
                    ? null
                    : () {
                        DiscountResult result;
                        if (mode == 'catalog') {
                          final d =
                              catalog.firstWhere((x) => x.id == selectedId);
                          result = DiscountResult(
                            id: d.id,
                            label: '${d.name} (${d.valueLabel})',
                            type: d.type,
                            value: d.value,
                          );
                        } else {
                          result = DiscountResult(
                            id: '',
                            label: manualType == 'Percentage'
                                ? 'Manual ${manualValue.toStringAsFixed(0)}%'
                                : 'Manual ${currency(manualValue)}',
                            type: manualType,
                            value: manualValue,
                          );
                        }
                        Navigator.pop(context, result);
                      },
                child: const Text('Aplicar'),
              ),
              const SizedBox(height: 12),
            ],
          );
        },
      ),
    ),
  );
}

/// ── Autorización de supervisor ──────────────────────────────
/// Devuelve el token del supervisor si las credenciales son válidas y NO es cajero.
Future<String?> supervisorAuthDialog(
  BuildContext context,
  SaleService saleService, {
  required String reason,
}) {
  final emailCtrl = TextEditingController();
  final passCtrl = TextEditingController();
  bool loading = false;
  String? error;
  return showDialog<String>(
    context: context,
    builder: (dialogContext) => StatefulBuilder(
      builder: (context, setState) {
        Future<void> verify() async {
          if (emailCtrl.text.trim().isEmpty || passCtrl.text.isEmpty) {
            setState(() => error = 'Ingresa email y contraseña del supervisor.');
            return;
          }
          setState(() {
            loading = true;
            error = null;
          });
          try {
            final res = await saleService.supervisorLogin(
                emailCtrl.text.trim(), passCtrl.text);
            if (res.rolName == 'Cajero') {
              setState(() {
                loading = false;
                error = 'El usuario no tiene permisos de supervisor.';
              });
              return;
            }
            if (dialogContext.mounted) {
              Navigator.pop(dialogContext, res.token);
            }
          } on ApiException catch (e) {
            setState(() {
              loading = false;
              error = e.message;
            });
          }
        }

        return AlertDialog(
          title: const Text('Autorización requerida'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(reason, style: const TextStyle(fontSize: 13)),
              const SizedBox(height: 12),
              TextField(
                controller: emailCtrl,
                keyboardType: TextInputType.emailAddress,
                decoration:
                    const InputDecoration(labelText: 'Email del supervisor'),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: passCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Contraseña'),
              ),
              if (error != null) ...[
                const SizedBox(height: 8),
                Text(error!, style: const TextStyle(color: Colors.red)),
              ],
            ],
          ),
          actions: [
            TextButton(
                onPressed: loading ? null : () => Navigator.pop(dialogContext),
                child: const Text('Cancelar')),
            FilledButton(
              onPressed: loading ? null : verify,
              child: loading
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Autorizar'),
            ),
          ],
        );
      },
    ),
  );
}

Widget _kv(String label, String value, {bool bold = false}) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label),
          Text(value,
              style: TextStyle(
                  fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
        ],
      ),
    );
