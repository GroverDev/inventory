import 'package:flutter/material.dart' show Color, Colors;

double _clampPositive(double v) => v < 0 ? 0 : v;

/// Saldo pendiente de cobrar.
double pendingAmount({required double total, required double paid}) =>
    _clampPositive(total - paid);

/// Cálculo del cobro para el monto que se está por agregar: pendiente,
/// vuelto y validaciones, aisladas de la UI para poder probarlas. Espejo de
/// los `computed` de pago en `PointOfSaleView.vue` (`pendingAmount`,
/// `liveChange`, `liveShortfall`, `exceedsNoChange`, `canAddPayment`).
class PaymentCalc {
  final double pending;
  final double typedAmount;

  /// Si el medio elegido entrega vuelto (efectivo). Tarjeta y QR no.
  final bool givesChange;

  const PaymentCalc({
    required this.pending,
    required this.typedAmount,
    required this.givesChange,
  });

  /// Tarjeta o QR no dan vuelto: no se puede cobrar de más con ellos. El
  /// margen de 0.004 absorbe el redondeo de centavos, igual que en la web.
  bool get exceedsNoChange => !givesChange && typedAmount > pending + 0.004;

  /// Vuelto de lo que se está escribiendo, antes de agregarlo como pago.
  double get liveChange =>
      givesChange ? _clampPositive(typedAmount - pending) : 0.0;

  double get liveShortfall => _clampPositive(pending - typedAmount);

  bool get canAdd => typedAmount > 0 && !exceedsNoChange;
}

/// El vuelto que queda grabado en la línea de pago al confirmarla.
double paymentReturned({
  required bool givesChange,
  required double amount,
  required double pending,
}) =>
    givesChange ? _clampPositive(amount - pending) : 0.0;

/// Billetes con los que suele pagarse este saldo: el siguiente múltiplo de
/// 10, 50, 100 y 200. Espejo de `quickAmounts` en `PointOfSaleView.vue`.
List<double> quickAmounts(double pending) {
  if (pending <= 0) return const [];
  final candidates = <double>{};
  for (final b in [10, 50, 100, 200]) {
    final v = ((pending / b).ceil() * b).toDouble();
    if (v > pending) candidates.add(v);
  }
  final sorted = candidates.toList()..sort();
  return sorted.take(4).toList();
}

/// Qué muestra el panel de cobro: siempre hay una cifra a la vista — lo que
/// falta cobrar mientras no se cubre el total, y el vuelto cuando se cubre.
/// Espejo de `payPanel` en `PointOfSaleView.vue`.
({String label, double? amount, String note, Color? color}) payPanel({
  required bool exceedsNoChange,
  required double pending,
  required double typedAmount,
  required double liveShortfall,
  required double liveChange,
  required double totalChangeSoFar,
  required String methodName,
}) {
  if (exceedsNoChange) {
    return (
      label: '$methodName no da vuelto',
      amount: null,
      note: 'No puede superar Bs ${pending.toStringAsFixed(2)}',
      color: Colors.red,
    );
  }
  if (pending <= 0) {
    return totalChangeSoFar > 0
        ? (label: 'Vuelto a entregar', amount: totalChangeSoFar, note: '', color: Colors.green)
        : (label: 'Cobro completo', amount: null, note: 'Sin vuelto', color: Colors.green);
  }
  if (typedAmount <= 0) {
    return (label: 'Por cobrar', amount: pending, note: '', color: null);
  }
  if (liveShortfall > 0) {
    return (label: 'Faltan', amount: liveShortfall, note: '', color: Colors.orange);
  }
  return liveChange > 0
      ? (label: 'Vuelto', amount: liveChange, note: '', color: Colors.green)
      : (label: 'Monto exacto', amount: null, note: 'Sin vuelto', color: Colors.green);
}
