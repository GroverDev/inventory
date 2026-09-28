import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/features/pos/cash_close_calc.dart';

/// Espejo de `denominationTotal` / `syncCashFromDenominations` /
/// `countingByDenomination` en `PointOfSaleView.vue`: el arqueo por medio es
/// a ciegas, y el efectivo declarado tiene que coincidir con lo que suman los
/// billetes y monedas cuando se cuenta así.
void main() {
  group('denominationTotal', () {
    test('suma valor × cantidad de cada denominación', () {
      final total = denominationTotal({
        200: 1, // 200
        50: 2, // 100
        10: 1, // 10
        0.5: 4, // 2
      });
      expect(total, 312.0);
    });

    test('las denominaciones sin cargar (null) no aportan', () {
      final total = denominationTotal({200: 1, 100: null, 10: 3});
      expect(total, 230.0);
    });

    test('sin nada contado, el total es cero', () {
      expect(denominationTotal(const {}), 0);
    });
  });

  group('buildDenominations', () {
    test('solo viajan las denominaciones con cantidad mayor a cero', () {
      final list = buildDenominations({200: 2, 100: 0, 50: null, 10: 5});
      expect(list, hasLength(2));
      expect(list.map((d) => d.value), containsAll([200.0, 10.0]));
      expect(list.firstWhere((d) => d.value == 200).quantity, 2);
    });

    test('sin nada cargado, la lista va vacía', () {
      expect(buildDenominations(const {}), isEmpty);
    });
  });

  group('countingByDenomination', () {
    test('la política de la empresa lo exige aunque no se haya cargado nada', () {
      expect(
        countingByDenomination(requiredByPolicy: true, toggledOn: false, total: 0),
        isTrue,
      );
    });

    test('el cajero lo activó pero todavía no cargó nada: no cuenta como activo', () {
      expect(
        countingByDenomination(requiredByPolicy: false, toggledOn: true, total: 0),
        isFalse,
      );
    });

    test('el cajero lo activó y ya cargó algo', () {
      expect(
        countingByDenomination(requiredByPolicy: false, toggledOn: true, total: 50),
        isTrue,
      );
    });

    test('sin política ni activación manual, no se cuenta por billetes', () {
      expect(
        countingByDenomination(requiredByPolicy: false, toggledOn: false, total: 50),
        isFalse,
      );
    });
  });

  group('buildCounts', () {
    test('solo van los medios que ya se declararon', () {
      final counts = buildCounts({'m1': 100, 'm2': null, 'm3': 0});
      expect(counts, {'m1': 100.0, 'm3': 0.0});
      expect(counts.containsKey('m2'), isFalse);
    });
  });

  test('el efectivo declarado por billetes coincide con lo que se manda al cerrar', () {
    // Escenario de punta a punta de la pantalla: el cajero cuenta 1 billete
    // de 100, 2 de 50 y 3 monedas de 2 — el efectivo declarado y las
    // denominaciones enviadas tienen que ser consistentes entre sí.
    final denomQty = <double, int?>{100: 1, 50: 2, 2: 3};
    final total = denominationTotal(denomQty);
    expect(total, 206.0);

    final declared = <String, double?>{'cash-id': total, 'card-id': 40};
    final counts = buildCounts(declared);
    expect(counts['cash-id'], 206.0);

    final denominations = buildDenominations(denomQty);
    final sumFromDenominations = denominations.fold<double>(
        0, (s, d) => s + d.value * d.quantity);
    expect(sumFromDenominations, counts['cash-id']);
  });
}
