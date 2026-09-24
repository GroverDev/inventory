import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/features/pos/payment_draft.dart';
import 'package:inventory_movil/models/catalog.dart';

final efectivo = PaymentMethod(id: 'e', name: 'Efectivo', requiresChanges: true, affectsCash: true);
final qr = PaymentMethod(id: 'q', name: 'QR');

void main() {
  test('el efectivo arranca vacío y el QR con el saldo', () {
    final d = PaymentDraft(37.5);
    expect(d.initialAmount(efectivo), isNull);
    expect(d.initialAmount(qr), 37.5);
  });

  test('el QR no puede cobrar más de lo pendiente; el efectivo sí', () {
    final d = PaymentDraft(37.5);
    expect(d.canAdd(qr, 40), isFalse);
    expect(d.panel(qr, 40).tone, PayPanelTone.error);
    expect(d.canAdd(efectivo, 40), isTrue);
  });

  test('pago mixto: QR parcial y efectivo con vuelto', () {
    final d = PaymentDraft(37.5)..add(qr, 20);
    expect(d.pending, 17.5);
    expect(d.panel(efectivo, 10).label, 'Faltan');
    expect(d.panel(efectivo, 10).amount, 7.5);
    expect(d.panel(efectivo, 20).amount, 2.5);

    d.add(efectivo, 20);
    expect(d.covered, isTrue);
    expect(d.lines.last.amountReturned, 2.5);
    expect(d.panel(null, 0).label, 'Vuelto a entregar');
  });

  test('billetes sugeridos: el siguiente múltiplo, sin repetir ni el exacto', () {
    expect(PaymentDraft(37.5).quickAmounts(), [40, 50, 100, 200]);
    expect(PaymentDraft(50).quickAmounts(), [100, 200]);   // 50 exacto ya es "Exacto"
    expect(PaymentDraft(180).quickAmounts(), [200]);
  });
}
