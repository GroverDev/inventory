/// Espejo de CashSessionResponse del backend.
class CashSession {
  final String id;
  final int userId;
  final String userFullName;
  final DateTime openedAt;
  final DateTime? closedAt;
  final double openingAmount;
  final double totalSales;

  /// Lo cobrado por metodos que entran al cajon (payment_methods.affects_cash),
  /// ya sin el vuelto. Es lo unico que suma al efectivo esperado: una venta por
  /// QR o tarjeta no deja plata en la caja.
  final double totalCashSales;
  final double totalExpenses;
  final double totalWithdrawals;
  final double totalIncome;

  /// Efectivo reintegrado por devoluciones en la sesion: sale del cajon.
  final double totalReturns;

  /// Lo que el cajero declaro al cerrar. Null mientras la caja sigue abierta.
  final double? declaredAmount;

  /// Esperado y diferencia tal como quedaron grabados en el cierre. Se prefieren
  /// a los calculados: son la foto del arqueo en el momento en que se hizo.
  final double? expectedAmount;
  final double? difference;

  /// Observacion del cierre (el motivo del faltante o sobrante, normalmente).
  final String notes;

  /// Arqueo del cierre por medio: esperado, declarado y diferencia. Vacío
  /// mientras la sesión sigue abierta.
  final List<CashCount> counts;

  /// Conteo del efectivo por billete y moneda, si se declaró así.
  final List<DenominationCount> denominations;

  /// Solo en un cierre rechazado: qué falta para cerrar — `'note'`
  /// (observación) o `'supervisor'`. Vacío en cualquier otro caso.
  final String closeRequires;

  CashSession({
    required this.id,
    required this.userId,
    required this.userFullName,
    required this.openedAt,
    required this.closedAt,
    required this.openingAmount,
    required this.totalSales,
    this.totalCashSales = 0,
    this.totalExpenses = 0,
    this.totalWithdrawals = 0,
    this.totalIncome = 0,
    this.totalReturns = 0,
    this.declaredAmount,
    this.expectedAmount,
    this.difference,
    this.notes = '',
    this.counts = const [],
    this.denominations = const [],
    this.closeRequires = '',
  });

  bool get isOpen => closedAt == null;

  /// Efectivo esperado en caja al momento del arqueo. En una sesion ya cerrada
  /// se usa el valor grabado en el cierre; en una abierta se calcula.
  double get expectedCash =>
      expectedAmount ??
      double.parse(
        (openingAmount + totalCashSales - totalExpenses - totalWithdrawals + totalIncome - totalReturns)
            .toStringAsFixed(2),
      );

  /// Sobrante (positivo) o faltante (negativo) del arqueo. Null si sigue abierta.
  double? get cashDifference {
    if (difference != null) return difference;
    if (declaredAmount == null) return null;
    return double.parse((declaredAmount! - expectedCash).toStringAsFixed(2));
  }

  factory CashSession.fromJson(Map<String, dynamic> j) => CashSession(
        id: (j['Id'] ?? '').toString(),
        userId: j['UserId'] ?? 0,
        userFullName: j['UserFullName'] ?? '',
        // Instantes: la API los manda en UTC, se pasan a hora local para que
        // coincidan con lo que muestra la web (ver _toInstanteLocal en
        // sale_history.dart). DateTime.now() ya es local.
        openedAt: DateTime.tryParse(j['OpenedAt']?.toString() ?? '')?.toLocal() ??
            DateTime.now(),
        closedAt: j['ClosedAt'] == null
            ? null
            : DateTime.tryParse(j['ClosedAt'].toString())?.toLocal(),
        openingAmount: (j['OpeningAmount'] ?? 0).toDouble(),
        totalSales: (j['TotalSales'] ?? 0).toDouble(),
        totalCashSales: (j['TotalCashSales'] ?? 0).toDouble(),
        totalExpenses: (j['TotalExpenses'] ?? 0).toDouble(),
        totalWithdrawals: (j['TotalWithdrawals'] ?? 0).toDouble(),
        totalIncome: (j['TotalIncome'] ?? 0).toDouble(),
        totalReturns: (j['TotalReturns'] ?? 0).toDouble(),
        declaredAmount: (j['DeclaredAmount'] as num?)?.toDouble(),
        expectedAmount: (j['ExpectedAmount'] as num?)?.toDouble(),
        difference: (j['Difference'] as num?)?.toDouble(),
        notes: j['Notes']?.toString() ?? '',
        counts: ((j['Counts'] as List?) ?? [])
            .map((e) => CashCount.fromJson(e as Map<String, dynamic>))
            .toList(),
        denominations: ((j['Denominations'] as List?) ?? [])
            .map((e) => DenominationCount.fromJson(e as Map<String, dynamic>))
            .toList(),
        closeRequires: j['CloseRequires']?.toString() ?? '',
      );
}

/// Arqueo de un medio de pago en el cierre de caja.
class CashCount {
  final String paymentMethodId;
  final String name;
  final String iconCss;
  final double expected;
  final double declared;

  /// Declarado − esperado: positivo es sobrante, negativo faltante.
  final double difference;

  CashCount({
    required this.paymentMethodId,
    required this.name,
    required this.iconCss,
    required this.expected,
    required this.declared,
    required this.difference,
  });

  factory CashCount.fromJson(Map<String, dynamic> j) => CashCount(
        paymentMethodId: (j['PaymentMethodId'] ?? '').toString(),
        name: j['Name'] ?? '',
        iconCss: j['IconCss'] ?? '',
        expected: (j['Expected'] ?? 0).toDouble(),
        declared: (j['Declared'] ?? 0).toDouble(),
        difference: (j['Difference'] ?? 0).toDouble(),
      );
}

/// Cantidad contada de un billete o moneda.
class DenominationCount {
  /// Valor del billete o moneda (200, 100, …, 0.10).
  final double value;
  final int quantity;

  const DenominationCount({required this.value, required this.quantity});

  factory DenominationCount.fromJson(Map<String, dynamic> j) =>
      DenominationCount(
        value: (j['Value'] ?? 0).toDouble(),
        quantity: (j['Quantity'] as num? ?? 0).toInt(),
      );

  Map<String, dynamic> toJson() => {'Value': value, 'Quantity': quantity};
}

/// Billetes y monedas bolivianos, de mayor a menor.
const List<double> bobDenominations = [200, 100, 50, 20, 10, 5, 2, 1, 0.5, 0.2, 0.1];

/// Configuración del cierre de caja de la empresa (GET api/Settings/cash-close).
class CashCloseSettings {
  /// Diferencia por medio a partir de la cual el cierre exige observación.
  final double noteThreshold;

  /// Diferencia por medio a partir de la cual, además, hace falta un
  /// supervisor. Nulo = nunca.
  final double? supervisorThreshold;

  /// Cierres rechazados a partir de los cuales hace falta un supervisor.
  /// Nulo = sin límite.
  final int? maxAttempts;

  /// El efectivo se declara contando billetes y monedas.
  final bool requireDenominations;

  const CashCloseSettings({
    this.noteThreshold = 10,
    this.supervisorThreshold,
    this.maxAttempts,
    this.requireDenominations = false,
  });

  factory CashCloseSettings.fromJson(Map<String, dynamic> j) =>
      CashCloseSettings(
        noteThreshold: (j['NoteThreshold'] ?? 10).toDouble(),
        supervisorThreshold: (j['SupervisorThreshold'] as num?)?.toDouble(),
        maxAttempts: (j['MaxAttempts'] as num?)?.toInt(),
        requireDenominations: j['RequireDenominations'] ?? false,
      );
}

/// Límites de descuento vigentes en la sucursal activa (GET api/Settings/pos).
class PosSettings {
  /// Tope del cajero: por encima pide autorización de supervisor.
  final double maxCashierDiscountPct;
  final double maxCashierDiscountAmount;

  /// Tope máximo para cualquier rol, ni con autorización. Nulo = sin tope.
  final double? maxDiscountPct;
  final double? maxDiscountAmount;

  PosSettings({
    required this.maxCashierDiscountPct,
    required this.maxCashierDiscountAmount,
    this.maxDiscountPct,
    this.maxDiscountAmount,
  });

  factory PosSettings.fromJson(Map<String, dynamic> j) => PosSettings(
        maxCashierDiscountPct: (j['MaxCashierDiscountPct'] ?? 15).toDouble(),
        maxCashierDiscountAmount:
            (j['MaxCashierDiscountAmount'] ?? 50).toDouble(),
        maxDiscountPct: (j['MaxDiscountPct'] as num?)?.toDouble(),
        maxDiscountAmount: (j['MaxDiscountAmount'] as num?)?.toDouble(),
      );
}
