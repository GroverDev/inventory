/// Modelos de lectura para el registro de ventas y las devoluciones.
///
/// Espejo de los *Response* del backend (PascalCase). Se mantienen aparte de
/// `sale.dart` (que modela el *request* del POS) para no mezclar
/// responsabilidades.
library;

double _toDouble(dynamic v) => v == null ? 0 : (v as num).toDouble();
int _toInt(dynamic v) => v == null ? 0 : (v as num).toInt();
String _toStr(dynamic v) => v?.toString() ?? '';

/// Instante de la API, pasado a hora local para mostrarlo.
///
/// El backend manda estas marcas en UTC (terminan en "Z") y `DateTime.parse`
/// conserva esa zona; `DateFormat` después usa esos mismos componentes sin
/// desplazar nada, así que sin el `toLocal()` la app mostraba cuatro horas de
/// más y las ventas de la noche aparecían con la fecha del día siguiente.
///
/// Ojo: esto es para INSTANTES. Las fechas puras —vencimientos, fecha de una
/// orden de compra— se guardan como medianoche UTC y no deben convertirse; para
/// esas está `formatApiDate` en `purchase.dart`.
DateTime? _toInstanteLocal(String v) => DateTime.tryParse(v)?.toLocal();

/// Resultado paginado de `GET api/Sales`.
class SalesPage {
  final List<SaleSummary> items;
  final int totalCount;
  final double periodSubtotal;
  final double periodDiscounts;
  final double periodTotal;

  SalesPage({
    required this.items,
    required this.totalCount,
    required this.periodSubtotal,
    required this.periodDiscounts,
    required this.periodTotal,
  });

  factory SalesPage.fromJson(Map<String, dynamic> json) => SalesPage(
        items: ((json['Items'] as List?) ?? [])
            .map((e) => SaleSummary.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalCount: _toInt(json['TotalCount']),
        periodSubtotal: _toDouble(json['PeriodSubtotal']),
        periodDiscounts: _toDouble(json['PeriodDiscounts']),
        periodTotal: _toDouble(json['PeriodTotal']),
      );
}

/// Cabecera de venta para el listado.
class SaleSummary {
  final String id;
  final String customerName;
  final String sellerName;
  final DateTime? saleDate;
  final double subtotal;
  final double totalDiscounts;
  final double total;
  final bool isActive;

  SaleSummary({
    required this.id,
    required this.customerName,
    required this.sellerName,
    required this.saleDate,
    required this.subtotal,
    required this.totalDiscounts,
    required this.total,
    required this.isActive,
  });

  factory SaleSummary.fromJson(Map<String, dynamic> json) => SaleSummary(
        id: _toStr(json['Id']),
        customerName: _toStr(json['CustomerName']),
        sellerName: _toStr(json['SellerName']),
        saleDate: _toInstanteLocal(_toStr(json['SaleDate'])),
        subtotal: _toDouble(json['Subtotal']),
        totalDiscounts: _toDouble(json['TotalDiscounts']),
        total: _toDouble(json['Total']),
        isActive: json['IsActive'] == true,
      );
}

/// Venta completa de `GET api/Sales/{id}`.
class SaleFull {
  final String id;
  final String customerName;
  final String sellerName;
  final DateTime? saleDate;
  final double subtotal;
  final double totalDiscounts;
  final double headerDiscountAmount;
  final double total;
  final bool isActive;
  final List<SaleDetailItem> detail;
  final List<SalePaymentInfo> payments;
  final List<SaleReturnInfo> returns;

  SaleFull({
    required this.id,
    required this.customerName,
    required this.sellerName,
    required this.saleDate,
    required this.subtotal,
    required this.totalDiscounts,
    required this.headerDiscountAmount,
    required this.total,
    required this.isActive,
    required this.detail,
    required this.payments,
    required this.returns,
  });

  factory SaleFull.fromJson(Map<String, dynamic> json) => SaleFull(
        id: _toStr(json['Id']),
        customerName: _toStr(json['CustomerName']),
        sellerName: _toStr(json['SellerName']),
        saleDate: _toInstanteLocal(_toStr(json['SaleDate'])),
        subtotal: _toDouble(json['Subtotal']),
        totalDiscounts: _toDouble(json['TotalDiscounts']),
        headerDiscountAmount: _toDouble(json['HeaderDiscountAmount']),
        total: _toDouble(json['Total']),
        isActive: json['IsActive'] == true,
        detail: ((json['Detail'] as List?) ?? [])
            .map((e) => SaleDetailItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        payments: ((json['Payments'] as List?) ?? [])
            .map((e) => SalePaymentInfo.fromJson(e as Map<String, dynamic>))
            .toList(),
        returns: ((json['Returns'] as List?) ?? [])
            .map((e) => SaleReturnInfo.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  bool get hasReturns => returns.isNotEmpty;

  double get totalReturned =>
      returns.fold(0.0, (s, r) => s + r.totalReturned);

  double get netTotal => total - totalReturned;

  /// Unidades ya devueltas para una línea de detalle concreta.
  int returnedFor(String saleDetailId) => returns
      .expand((r) => r.detail)
      .where((d) => d.saleDetailId == saleDetailId)
      .fold(0, (s, d) => s + d.quantityReturned);
}

class SaleDetailItem {
  final String id;
  final String productId;
  final int quantity;
  final double unitPrice;
  final double lineSubtotal;
  final double lineTotalDiscounts;
  final double lineTotal;
  final String productName;

  /// Precio unitario efectivamente cobrado: el de lista menos el descuento de la
  /// linea y menos la parte prorrateada del descuento global. Lo calcula el
  /// servidor; es el que se reembolsa al devolver.
  final double effectiveUnitPrice;

  SaleDetailItem({
    required this.id,
    required this.productId,
    required this.quantity,
    required this.unitPrice,
    required this.lineSubtotal,
    required this.lineTotalDiscounts,
    required this.lineTotal,
    required this.productName,
    required this.effectiveUnitPrice,
  });

  factory SaleDetailItem.fromJson(Map<String, dynamic> json) => SaleDetailItem(
        id: _toStr(json['Id']),
        productId: _toStr(json['ProductId']),
        quantity: _toInt(json['Quantity']),
        unitPrice: _toDouble(json['UnitPrice']),
        lineSubtotal: _toDouble(json['LineSubtotal']),
        lineTotalDiscounts: _toDouble(json['LineTotalDiscounts']),
        lineTotal: _toDouble(json['LineTotal']),
        productName: _toStr(json['ProductName']),
        effectiveUnitPrice: _toDouble(json['EffectiveUnitPrice']),
      );
}

class SalePaymentInfo {
  final String paymentMethodId;
  final String paymentMethodName;
  final String iconCss;
  final double amountGiven;
  final double amountReturned;

  SalePaymentInfo({
    this.paymentMethodId = '',
    required this.paymentMethodName,
    required this.iconCss,
    required this.amountGiven,
    required this.amountReturned,
  });

  factory SalePaymentInfo.fromJson(Map<String, dynamic> json) => SalePaymentInfo(
        paymentMethodId: _toStr(json['PaymentMethodId']),
        paymentMethodName: _toStr(json['PaymentMethodName']),
        iconCss: _toStr(json['IconCss']),
        amountGiven: _toDouble(json['AmountGiven']),
        amountReturned: _toDouble(json['AmountReturned']),
      );
}

class SaleReturnInfo {
  final String id;
  final DateTime? returnDate;
  final String? reason;
  final String paymentMethodName;
  final double totalReturned;
  final bool isFullReturn;
  final List<SaleReturnDetailInfo> detail;

  SaleReturnInfo({
    required this.id,
    required this.returnDate,
    required this.reason,
    this.paymentMethodName = '',
    required this.totalReturned,
    required this.isFullReturn,
    required this.detail,
  });

  factory SaleReturnInfo.fromJson(Map<String, dynamic> json) => SaleReturnInfo(
        id: _toStr(json['Id']),
        returnDate: _toInstanteLocal(_toStr(json['ReturnDate'])),
        reason: json['Reason'] as String?,
        paymentMethodName: _toStr(json['PaymentMethodName']),
        totalReturned: _toDouble(json['TotalReturned']),
        isFullReturn: json['IsFullReturn'] == true,
        detail: ((json['Detail'] as List?) ?? [])
            .map((e) => SaleReturnDetailInfo.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class SaleReturnDetailInfo {
  final String saleDetailId;
  final String productId;
  final String productName;
  final int quantityReturned;
  final double unitPrice;
  final double lineTotal;

  SaleReturnDetailInfo({
    required this.saleDetailId,
    required this.productId,
    required this.productName,
    required this.quantityReturned,
    required this.unitPrice,
    required this.lineTotal,
  });

  factory SaleReturnDetailInfo.fromJson(Map<String, dynamic> json) =>
      SaleReturnDetailInfo(
        saleDetailId: _toStr(json['SaleDetailId']),
        productId: _toStr(json['ProductId']),
        productName: _toStr(json['ProductName']),
        quantityReturned: _toInt(json['QuantityReturned']),
        unitPrice: _toDouble(json['UnitPrice']),
        lineTotal: _toDouble(json['LineTotal']),
      );
}

// ── Request para POST api/SaleReturn ─────────────────────────
class SaleReturnRequest {
  final String saleId;
  final String? reason;

  /// Medio por el que se reintegra. El servidor decide con el si sale plata del
  /// cajon; si es efectivo exige tener una caja abierta.
  final String? paymentMethodId;
  final List<SaleReturnDetailRequest> detail;

  SaleReturnRequest({
    required this.saleId,
    this.reason,
    this.paymentMethodId,
    required this.detail,
  });

  Map<String, dynamic> toJson() => {
        'SaleId': saleId,
        'Reason': reason,
        'PaymentMethodId': paymentMethodId,
        'Detail': detail.map((d) => d.toJson()).toList(),
      };
}

class SaleReturnDetailRequest {
  final String saleDetailId;
  final String productId;
  final int quantityReturned;
  final double unitPrice;

  SaleReturnDetailRequest({
    required this.saleDetailId,
    required this.productId,
    required this.quantityReturned,
    required this.unitPrice,
  });

  Map<String, dynamic> toJson() => {
        'SaleDetailId': saleDetailId,
        'ProductId': productId,
        'QuantityReturned': quantityReturned,
        'UnitPrice': unitPrice,
      };
}
