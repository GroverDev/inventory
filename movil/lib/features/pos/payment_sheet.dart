import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../models/catalog.dart';
import '../../models/sale.dart';
import 'payment_draft.dart';

/// Modal de cobro: elige medios, arma los pagos y devuelve la lista (null si
/// se cierra sin confirmar). Las cuentas están en [PaymentDraft].
///
/// Igual que el POS web: el efectivo arranca elegido y vacío, tarjeta y QR con
/// el saldo y sin poder superarlo, atajos de billetes, y un panel que siempre
/// muestra cuánto falta o cuánto vuelto dar.
Future<List<SalePayment>?> showPaymentSheet(
BuildContext context, {
required double total,
required List<PaymentMethod> methods,
}) {
  final draft = PaymentDraft(total);
  final amountCtrl = TextEditingController();
  final amountFocus = FocusNode();
  PaymentMethod? method;

  double typed() => double.tryParse(amountCtrl.text.trim().replaceAll(',', '.')) ?? 0;

  void select(PaymentMethod? m) {
    method = m;
    final inicial = m == null ? null : draft.initialAmount(m);
    amountCtrl.text = inicial == null ? '' : inicial.toStringAsFixed(2);
    // Después del frame: la primera vez el campo todavía no existe.
    if (m != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (amountFocus.context != null) amountFocus.requestFocus();
      });
    }
  }

  // El efectivo es el cobro más común: queda elegido para escribir directo.
  void selectCashIfPending() {
    final efectivo = methods.where((m) => m.requiresChanges).firstOrNull;
    select(draft.pending > 0 ? (efectivo ?? method) : null);
  }

  selectCashIfPending();

  return showModalBottomSheet<List<SalePayment>>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheetContext) => Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(sheetContext).viewInsets.bottom,
      ),
      child: StatefulBuilder(
        builder: (context, setSheet) {
          final scheme = Theme.of(context).colorScheme;
          final m = method;
          final panel = draft.panel(m, typed());

          void add() {
            if (!draft.canAdd(m, typed())) return;
            setSheet(() {
              draft.add(m!, typed());
              // Pago mixto: si queda saldo (pagó una parte con QR), el resto
              // normalmente va en efectivo.
              selectCashIfPending();
            });
          }

          void confirmSale() {
            // Lo escrito y no agregado todavía también cuenta: escribir el
            // monto y tocar Confirmar no debería exigir tocar Agregar antes.
            if (draft.canAdd(m, typed())) add();
            if (draft.covered) Navigator.pop(sheetContext, draft.lines);
          }

          final (Color bg, Color fg) = switch (panel.tone) {
            PayPanelTone.neutral => (scheme.surfaceContainerHighest, scheme.onSurface),
            PayPanelTone.short => (Colors.orange.withValues(alpha: 0.15), Colors.orange.shade900),
            PayPanelTone.ok => (Colors.green.withValues(alpha: 0.15), Colors.green.shade800),
            PayPanelTone.error => (scheme.errorContainer, scheme.onErrorContainer),
          };
          final puedeConfirmar = draft.covered ||
              (draft.canAdd(m, typed()) && draft.paid + typed() + 0.004 >= total);

          return SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Cobrar venta',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                    Text('Total ${currency(total)}',
                        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                  ],
                ),
                const SizedBox(height: 12),

                // Panel de cobro, siempre visible y de alto fijo: primero
                // cuánto cobrar, mientras se escribe cuánto falta, y al cubrir
                // el total cuánto devolver.
                Container(
                  height: 76,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(10)),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(panel.label, style: TextStyle(color: fg)),
                      Text(
                        panel.amount != null ? currency(panel.amount!) : panel.note,
                        style: TextStyle(
                            color: fg,
                            fontWeight: FontWeight.bold,
                            fontSize: panel.amount != null ? 26 : 16),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),

                if (draft.pending > 0) ...[
                  Wrap(
                    spacing: 8,
                    runSpacing: 4,
                    children: [
                      for (final pm in methods)
                        ChoiceChip(
                          label: Text(pm.name),
                          selected: m?.id == pm.id,
                          onSelected: (_) => setSheet(() => select(pm)),
                        ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  if (m != null) ...[
                    Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: amountCtrl,
                            focusNode: amountFocus,
                            keyboardType:
                                const TextInputType.numberWithOptions(decimal: true),
                            textInputAction: TextInputAction.done,
                            style: const TextStyle(fontSize: 20),
                            decoration: InputDecoration(
                              labelText: m.requiresChanges ? 'Efectivo recibido' : 'Monto a cobrar',
                              prefixText: 'Bs. ',
                              errorText: draft.exceedsNoChange(m, typed())
                                  ? 'Sin vuelto: hasta ${currency(draft.pending)}'
                                  : null,
                            ),
                            onChanged: (_) => setSheet(() {}),
                            // Enter agrega el pago y, si con eso se cubre el
                            // total, confirma la venta.
                            onSubmitted: (_) => confirmSale(),
                          ),
                        ),
                        const SizedBox(width: 8),
                        FilledButton.tonal(
                          onPressed: draft.canAdd(m, typed()) ? add : null,
                          child: const Text('Agregar'),
                        ),
                      ],
                    ),
                    // Atajos: el monto exacto y los billetes con que suele pagarse.
                    if (m.requiresChanges)
                      Padding(
                        padding: const EdgeInsets.only(top: 8),
                        child: Wrap(
                          spacing: 8,
                          runSpacing: 4,
                          children: [
                            ActionChip(
                              label: const Text('Exacto'),
                              onPressed: () => setSheet(() =>
                                  amountCtrl.text = draft.pending.toStringAsFixed(2)),
                            ),
                            for (final v in draft.quickAmounts())
                              ActionChip(
                                label: Text('Bs. ${v.toStringAsFixed(0)}'),
                                onPressed: () =>
                                    setSheet(() => amountCtrl.text = v.toStringAsFixed(2)),
                              ),
                          ],
                        ),
                      ),
                  ],
                ],

                if (draft.lines.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  for (var i = 0; i < draft.lines.length; i++)
                    ListTile(
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      title: Text(draft.lines[i].paymentMethodName),
                      subtitle: draft.lines[i].amountReturned > 0
                          ? Text('Vuelto ${currency(draft.lines[i].amountReturned)}')
                          : null,
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(currency(draft.lines[i].amountGiven)),
                          IconButton(
                            icon: const Icon(Icons.close, size: 18),
                            tooltip: 'Quitar pago',
                            onPressed: () => setSheet(() {
                              draft.removeAt(i);
                              // Al quitar un pago los vueltos cambian: se
                              // recalculan agregando de nuevo en orden.
                              final quedan = List.of(draft.lines);
                              draft.lines.clear();
                              for (final l in quedan) {
                                final pm = methods.firstWhere((x) => x.id == l.paymentMethodId);
                                draft.add(pm, l.amountGiven);
                              }
                              selectCashIfPending();
                            }),
                          ),
                        ],
                      ),
                    ),
                ],

                const SizedBox(height: 12),
                FilledButton.icon(
                  style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                  onPressed: puedeConfirmar ? confirmSale : null,
                  icon: const Icon(Icons.check),
                  label: const Text('Confirmar venta'),
                ),
                const SizedBox(height: 12),
              ],
            ),
          );
        },
      ),
    ),
  );
  // Sin dispose del campo ni del foco: la hoja los usa hasta terminar la
  // animación de cierre, después de que este Future ya se completó.
}
