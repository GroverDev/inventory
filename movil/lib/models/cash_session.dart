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
      );
}

/// Límites de descuento para cajeros (GET api/Settings/pos).
class PosSettings {
  final double maxCashierDiscountPct;
  final double maxCashierDiscountAmount;

  PosSettings({
    required this.maxCashierDiscountPct,
    required this.maxCashierDiscountAmount,
  });

  factory PosSettings.fromJson(Map<String, dynamic> j) => PosSettings(
        maxCashierDiscountPct: (j['MaxCashierDiscountPct'] ?? 15).toDouble(),
        maxCashierDiscountAmount:
            (j['MaxCashierDiscountAmount'] ?? 50).toDouble(),
      );
}
