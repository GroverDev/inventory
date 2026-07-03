/// Espejo de CashSessionResponse del backend.
class CashSession {
  final String id;
  final int userId;
  final String userFullName;
  final DateTime openedAt;
  final DateTime? closedAt;
  final double openingAmount;
  final double totalSales;
  final double totalExpenses;
  final double totalWithdrawals;
  final double totalIncome;

  CashSession({
    required this.id,
    required this.userId,
    required this.userFullName,
    required this.openedAt,
    required this.closedAt,
    required this.openingAmount,
    required this.totalSales,
    this.totalExpenses = 0,
    this.totalWithdrawals = 0,
    this.totalIncome = 0,
  });

  bool get isOpen => closedAt == null;

  /// Efectivo esperado en caja al momento del arqueo.
  double get expectedCash => double.parse(
        (openingAmount + totalSales - totalExpenses - totalWithdrawals + totalIncome)
            .toStringAsFixed(2),
      );

  factory CashSession.fromJson(Map<String, dynamic> j) => CashSession(
        id: (j['Id'] ?? '').toString(),
        userId: j['UserId'] ?? 0,
        userFullName: j['UserFullName'] ?? '',
        openedAt: DateTime.tryParse(j['OpenedAt']?.toString() ?? '') ??
            DateTime.now(),
        closedAt: j['ClosedAt'] == null
            ? null
            : DateTime.tryParse(j['ClosedAt'].toString()),
        openingAmount: (j['OpeningAmount'] ?? 0).toDouble(),
        totalSales: (j['TotalSales'] ?? 0).toDouble(),
        totalExpenses: (j['TotalExpenses'] ?? 0).toDouble(),
        totalWithdrawals: (j['TotalWithdrawals'] ?? 0).toDouble(),
        totalIncome: (j['TotalIncome'] ?? 0).toDouble(),
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
