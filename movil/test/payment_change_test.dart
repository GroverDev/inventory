import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/features/pos/payment_calc.dart';

/// Corrige el bug del cobro móvil: antes el vuelto se calculaba como
/// `pagado − total` sobre TODO lo pagado, así que cobrar de más con tarjeta
/// o QR mostraba un vuelto que nadie iba a entregar. Espejo de los tests de
/// `PointOfSaleView.vue` (`liveChange`, `exceedsNoChange`, `payPanel`).
void main() {
  group('PaymentCalc — efectivo (da vuelto)', () {
    test('el pago exacto no da vuelto', () {
      final calc = PaymentCalc(pending: 50, typedAmount: 50, givesChange: true);
      expect(calc.liveChange, 0);
      expect(calc.liveShortfall, 0);
      expect(calc.exceedsNoChange, isFalse);
      expect(calc.canAdd, isTrue);
    });

    test('pagar de más da vuelto por la diferencia', () {
      final calc = PaymentCalc(pending: 50, typedAmount: 100, givesChange: true);
      expect(calc.liveChange, 50);
      expect(calc.exceedsNoChange, isFalse); // el efectivo sí puede cobrar de más
    });

    test('pagar de menos deja un faltante, no vuelto', () {
      final calc = PaymentCalc(pending: 50, typedAmount: 30, givesChange: true);
      expect(calc.liveChange, 0);
      expect(calc.liveShortfall, 20);
    });
  });

  group('PaymentCalc — tarjeta o QR (no dan vuelto)', () {
    test('el pago exacto se puede agregar', () {
      final calc = PaymentCalc(pending: 50, typedAmount: 50, givesChange: false);
      expect(calc.exceedsNoChange, isFalse);
      expect(calc.canAdd, isTrue);
      expect(calc.liveChange, 0); // nunca da vuelto, aunque sobrara
    });

    test('cobrar de más queda bloqueado: no hay vuelto que entregar', () {
      final calc = PaymentCalc(pending: 50, typedAmount: 60, givesChange: false);
      expect(calc.exceedsNoChange, isTrue);
      expect(calc.canAdd, isFalse);
      expect(calc.liveChange, 0);
    });

    test('un centavo de más por redondeo no bloquea (margen de 0.004)', () {
      final calc =
          PaymentCalc(pending: 49.999, typedAmount: 50, givesChange: false);
      expect(calc.exceedsNoChange, isFalse);
    });
  });

  group('paymentReturned', () {
    test('efectivo: vuelto = pagado - pendiente', () {
      expect(paymentReturned(givesChange: true, amount: 100, pending: 50), 50);
    });

    test('tarjeta: nunca hay vuelto aunque se pague de más', () {
      expect(paymentReturned(givesChange: false, amount: 100, pending: 50), 0);
    });
  });

  group('pago mixto (una parte con tarjeta, el resto en efectivo)', () {
    test('el total pagado combina ambos medios y el vuelto es solo del efectivo', () {
      const total = 100.0;
      // Primer pago: tarjeta por 60, exacto para esa parte del saldo.
      final pendienteInicial = pendingAmount(total: total, paid: 0);
      final tarjeta = PaymentCalc(
          pending: pendienteInicial, typedAmount: 60, givesChange: false);
      expect(tarjeta.canAdd, isTrue);
      final vueltoTarjeta =
          paymentReturned(givesChange: false, amount: 60, pending: pendienteInicial);
      expect(vueltoTarjeta, 0);

      // Segundo pago: efectivo por el resto, con algo de más.
      final pendienteRestante = pendingAmount(total: total, paid: 60);
      expect(pendienteRestante, 40);
      final efectivo = PaymentCalc(
          pending: pendienteRestante, typedAmount: 50, givesChange: true);
      final vueltoEfectivo = paymentReturned(
          givesChange: true, amount: 50, pending: pendienteRestante);
      expect(vueltoEfectivo, 10);
      expect(efectivo.canAdd, isTrue);

      // Vuelto total de la venta: solo lo que dio el efectivo, no 110-100.
      final vueltoTotal = vueltoTarjeta + vueltoEfectivo;
      expect(vueltoTotal, 10);
    });
  });

  group('quickAmounts', () {
    test('propone el siguiente múltiplo de 10, 50, 100 y 200 sobre el pendiente', () {
      expect(quickAmounts(37), [40, 50, 100, 200]);
    });

    test('sin pendiente no hay montos rápidos', () {
      expect(quickAmounts(0), isEmpty);
    });

    test('un pendiente que ya es múltiplo exacto no se ofrece a sí mismo', () {
      // 100 es múltiplo de 10, 50 y 100 a la vez: el "siguiente" múltiplo de
      // cada uno sería el propio 100, que no sirve como atajo (no es "más"
      // que lo pendiente) — solo queda el de 200.
      expect(quickAmounts(100), [200]);
    });
  });

  group('payPanel', () {
    test('sin nada escrito, muestra lo que falta cobrar', () {
      final p = payPanel(
        exceedsNoChange: false,
        pending: 50,
        typedAmount: 0,
        liveShortfall: 50,
        liveChange: 0,
        totalChangeSoFar: 0,
        methodName: 'Efectivo',
      );
      expect(p.label, 'Por cobrar');
      expect(p.amount, 50);
    });

    test('escribiendo de menos, muestra cuánto falta', () {
      final p = payPanel(
        exceedsNoChange: false,
        pending: 50,
        typedAmount: 30,
        liveShortfall: 20,
        liveChange: 0,
        totalChangeSoFar: 0,
        methodName: 'Efectivo',
      );
      expect(p.label, 'Faltan');
      expect(p.amount, 20);
    });

    test('escribiendo de más en efectivo, muestra el vuelto', () {
      final p = payPanel(
        exceedsNoChange: false,
        pending: 50,
        typedAmount: 100,
        liveShortfall: 0,
        liveChange: 50,
        totalChangeSoFar: 0,
        methodName: 'Efectivo',
      );
      expect(p.label, 'Vuelto');
      expect(p.amount, 50);
    });

    test('tarjeta que excede el pendiente: bloqueado, nunca "vuelto"', () {
      final p = payPanel(
        exceedsNoChange: true,
        pending: 50,
        typedAmount: 60,
        liveShortfall: 0,
        liveChange: 0,
        totalChangeSoFar: 0,
        methodName: 'Tarjeta',
      );
      expect(p.label, 'Tarjeta no da vuelto');
      expect(p.amount, isNull);
    });

    test('cubierto sin vuelto pendiente (ya se pagó exacto): cobro completo', () {
      final p = payPanel(
        exceedsNoChange: false,
        pending: 0,
        typedAmount: 0,
        liveShortfall: 0,
        liveChange: 0,
        totalChangeSoFar: 0,
        methodName: '',
      );
      expect(p.label, 'Cobro completo');
      expect(p.amount, isNull);
      expect(p.note, 'Sin vuelto');
    });

    test('cubierto con vuelto ya registrado en pagos anteriores (pago mixto)', () {
      final p = payPanel(
        exceedsNoChange: false,
        pending: 0,
        typedAmount: 0,
        liveShortfall: 0,
        liveChange: 0,
        totalChangeSoFar: 10,
        methodName: '',
      );
      expect(p.label, 'Vuelto a entregar');
      expect(p.amount, 10);
    });
  });
}
