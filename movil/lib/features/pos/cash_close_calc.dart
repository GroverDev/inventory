import '../../models/cash_session.dart';

/// Suma del efectivo contado por billete y moneda. Espejo de
/// `denominationTotal` en `PointOfSaleView.vue`.
double denominationTotal(Map<double, int?> qty) =>
    bobDenominations.fold(0.0, (s, v) => s + v * (qty[v] ?? 0));

/// Lista de denominaciones a enviar al servidor: solo las que tienen
/// cantidad mayor a cero (el resto no aporta nada y solo ensucia el payload).
List<DenominationCount> buildDenominations(Map<double, int?> qty) => [
      for (final v in bobDenominations)
        if ((qty[v] ?? 0) > 0) DenominationCount(value: v, quantity: qty[v]!),
    ];

/// Si el efectivo se cuenta por billetes: obligatorio por configuración de
/// la empresa, o porque el cajero activó el desglose y ya cargó algo.
/// Espejo de `countingByDenomination` en `PointOfSaleView.vue`.
bool countingByDenomination({
  required bool requiredByPolicy,
  required bool toggledOn,
  required double total,
}) =>
    requiredByPolicy || (toggledOn && total > 0);

/// El `Counts` que viaja a `PUT api/CashSession/{id}/close`: lo declarado por
/// cada medio arqueado, por id. Los que no se completaron no deberían llegar
/// acá — se valida antes, en la pantalla.
Map<String, double> buildCounts(Map<String, double?> declared) => {
      for (final e in declared.entries)
        if (e.value != null) e.key: e.value!,
    };
