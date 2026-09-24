import '../../models/catalog.dart';
import '../../models/sale.dart';

/// Estado del panel de cobro: qué cifra mostrar y en qué tono.
enum PayPanelTone { neutral, short, ok, error }

class PayPanel {
  const PayPanel(this.label, {this.amount, this.note = '', this.tone = PayPanelTone.neutral});
  final String label;

  /// Cifra a mostrar en grande; null si se muestra [note] en su lugar.
  final double? amount;
  final String note;
  final PayPanelTone tone;
}

/// Cobro en curso: los pagos agregados y las cuentas del modal de cobro.
///
/// Mismas reglas que el servidor (`SalePaymentRules`) y que el POS web:
/// solo los medios que dan vuelto (efectivo) pueden superar lo pendiente; con
/// tarjeta o QR se cobra exacto. El vuelto real lo recalcula el servidor; el
/// de acá es para mostrarlo mientras se cobra.
class PaymentDraft {
  PaymentDraft(this.total);

  final double total;
  final List<SalePayment> lines = [];

  static double _r2(double v) => (v * 100).roundToDouble() / 100;

  double get paid => _r2(lines.fold<double>(0, (s, l) => s + l.amountGiven));
  double get pending => _r2((total - paid).clamp(0, double.infinity).toDouble());
  double get change => _r2(lines.fold<double>(0, (s, l) => s + l.amountReturned));
  bool get covered => paid + 0.004 >= total;

  /// Monto con que arranca el campo al elegir un medio: vacío para el efectivo
  /// (se escribe lo que entregó el cliente), el saldo para tarjeta o QR.
  double? initialAmount(PaymentMethod m) => m.requiresChanges ? null : pending;

  /// Un medio sin vuelto no puede cobrar más de lo pendiente.
  bool exceedsNoChange(PaymentMethod? m, double typed) =>
      m != null && !m.requiresChanges && typed > pending + 0.004;

  bool canAdd(PaymentMethod? m, double typed) =>
      m != null && typed > 0 && pending > 0 && !exceedsNoChange(m, typed);

  /// Agrega el pago. El vuelto sale de este pago si da vuelto y cubre de más.
  void add(PaymentMethod m, double typed) {
    final amount = _r2(typed);
    final returned = m.requiresChanges
        ? _r2((paid + amount - total).clamp(0, amount).toDouble())
        : 0.0;
    lines.add(SalePayment(
      paymentMethodId: m.id,
      paymentMethodName: m.name,
      iconCss: m.iconCss,
      amountGiven: amount,
      amountReturned: returned,
    ));
  }

  void removeAt(int i) => lines.removeAt(i);

  /// Billetes con que suele pagarse el saldo: el siguiente múltiplo de 10,
  /// 50, 100 y 200.
  List<double> quickAmounts() {
    final p = pending;
    if (p <= 0) return const [];
    final set = <double>{
      for (final b in const [10, 50, 100, 200]) (p / b).ceil() * b.toDouble(),
    }..removeWhere((v) => v <= p);
    return (set.toList()..sort()).take(4).toList();
  }

  /// Siempre hay una cifra a la vista: lo que falta mientras no se cubre el
  /// total, y el vuelto cuando se cubre.
  PayPanel panel(PaymentMethod? m, double typed) {
    if (exceedsNoChange(m, typed)) {
      return PayPanel('${m!.name} no da vuelto',
          note: 'No puede superar ${pending.toStringAsFixed(2)}', tone: PayPanelTone.error);
    }
    if (pending <= 0) {
      return change > 0
          ? PayPanel('Vuelto a entregar', amount: change, tone: PayPanelTone.ok)
          : const PayPanel('Cobro completo', note: 'Sin vuelto', tone: PayPanelTone.ok);
    }
    if (typed <= 0) return PayPanel('Por cobrar', amount: pending);

    final falta = _r2(pending - typed);
    if (falta > 0) return PayPanel('Faltan', amount: falta, tone: PayPanelTone.short);
    final vuelto = _r2(typed - pending);
    return vuelto > 0
        ? PayPanel('Vuelto', amount: vuelto, tone: PayPanelTone.ok)
        : const PayPanel('Monto exacto', note: 'Sin vuelto', tone: PayPanelTone.ok);
  }
}
