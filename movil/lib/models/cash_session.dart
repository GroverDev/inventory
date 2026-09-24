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

  /// Arqueo del cierre por medio de pago. Vacío mientras sigue abierta.
  final List<CashCount> counts;

  /// Cierres rechazados por diferencia sin observación antes del definitivo.
  final int closeAttempts;

  /// Supervisor que autorizó el cierre, si lo necesitó.
  final String closeAuthorizedByName;

  /// Conteo del efectivo por billete y moneda, si se declaró.
  final List<DenominationCount> denominations;

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
    this.closeAttempts = 0,
    this.closeAuthorizedByName = '',
    this.denominations = const [],
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
        counts: [
          for (final c in (j['Counts'] as List? ?? const []))
            CashCount.fromJson(c as Map<String, dynamic>)
        ],
        closeAttempts: j['CloseAttempts'] ?? 0,
        closeAuthorizedByName: j['CloseAuthorizedByName']?.toString() ?? '',
        denominations: [
          for (final d in (j['Denominations'] as List? ?? const []))
            DenominationCount.fromJson(d as Map<String, dynamic>)
        ],
      );
}

/// Límites de descuento para cajeros (GET api/Settings/pos).
class PosSettings {
  final double maxCashierDiscountPct;
  final double maxCashierDiscountAmount;

  /// Tope para cualquier rol en la sucursal activa; null = sin tope.
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

/// Arqueo de un medio en el cierre: esperado, declarado y diferencia
/// (declarado − esperado: positivo sobra, negativo falta).
class CashCount {
  final String paymentMethodId;
  final String name;
  final double expected;
  final double declared;
  final double difference;

  const CashCount({
    required this.paymentMethodId,
    required this.name,
    required this.expected,
    required this.declared,
    required this.difference,
  });

  factory CashCount.fromJson(Map<String, dynamic> j) => CashCount(
        paymentMethodId: (j['PaymentMethodId'] ?? '').toString(),
        name: j['Name'] ?? '',
        expected: (j['Expected'] ?? 0).toDouble(),
        declared: (j['Declared'] ?? 0).toDouble(),
        difference: (j['Difference'] ?? 0).toDouble(),
      );
}

/// Cantidad contada de un billete o moneda.
class DenominationCount {
  final double value;
  final int quantity;

  const DenominationCount(this.value, this.quantity);

  factory DenominationCount.fromJson(Map<String, dynamic> j) =>
      DenominationCount((j['Value'] ?? 0).toDouble(), j['Quantity'] ?? 0);

  Map<String, dynamic> toJson() => {'Value': value, 'Quantity': quantity};

  /// "200", "0.50": los billetes sin decimales, las monedas chicas con dos.
  static String label(double v) => v >= 1 ? v.toStringAsFixed(0) : v.toStringAsFixed(2);
}

/// Billetes y monedas bolivianos, de mayor a menor.
const bobDenominations = [200.0, 100.0, 50.0, 20.0, 10.0, 5.0, 2.0, 1.0, 0.5, 0.2, 0.1];

/// Suma del conteo, redondeada a centavos (0.1 × 3 no da 0.3 exacto en double).
double denominationTotal(Map<double, int> qty) =>
    (qty.entries.fold<double>(0, (t, e) => t + e.key * e.value) * 100).roundToDouble() / 100;

/// Un medio de pago en la configuración del cierre.
class CloseMethod {
  final String id;
  final String name;
  final bool affectsCash;
  final bool requiresCount;

  const CloseMethod({required this.id, required this.name, this.affectsCash = false, this.requiresCount = false});

  /// Se declara al cerrar: el efectivo siempre, los demás si se configuró.
  bool get isCounted => affectsCash || requiresCount;

  factory CloseMethod.fromJson(Map<String, dynamic> j) => CloseMethod(
        id: (j['Id'] ?? '').toString(),
        name: j['Name'] ?? '',
        affectsCash: j['AffectsCash'] ?? false,
        requiresCount: j['RequiresCount'] ?? false,
      );
}

/// Configuración del cierre de caja (GET api/Settings/cash-close).
class CashCloseSettings {
  final double noteThreshold;
  final bool requireDenominations;
  final List<CloseMethod> methods;

  const CashCloseSettings({this.noteThreshold = 10, this.requireDenominations = false, this.methods = const []});

  List<CloseMethod> get counted => methods.where((m) => m.isCounted).toList();

  factory CashCloseSettings.fromJson(Map<String, dynamic> j) => CashCloseSettings(
        noteThreshold: (j['NoteThreshold'] ?? 10).toDouble(),
        requireDenominations: j['RequireDenominations'] ?? false,
        methods: [
          for (final m in (j['Methods'] as List? ?? const []))
            CloseMethod.fromJson(m as Map<String, dynamic>)
        ],
      );
}
