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

/// Estado de una venta frente a sus devoluciones: es `v_sales_net.sale_status`,
/// la misma definición que usan el listado web y el dashboard.
///
/// `anulada` en la vista significa `sales.is_active = false`, que en esta app
/// solo ocurre cuando se devolvió la venta entera (de una vez o sumando varias
/// devoluciones parciales); por eso se muestra como "Devuelta total".
enum SaleStatus { activa, conDevolucion, devueltaTotal }

/// Traduce `sale_status`. Si el backend no lo manda —API vieja— se deduce de lo
/// que sí llegó, para que la pantalla nunca quede sin estado.
SaleStatus _toStatus(dynamic raw, {required bool isActive, required bool hasReturns}) {
  switch (raw?.toString()) {
    case 'anulada':
      return SaleStatus.devueltaTotal;
    case 'con_devolucion':
      return SaleStatus.conDevolucion;
    case 'activa':
      return SaleStatus.activa;
  }
  if (!isActive) return SaleStatus.devueltaTotal;
  return hasReturns ? SaleStatus.conDevolucion : SaleStatus.activa;
}

/// Resultado paginado de `GET api/Sales`.
class SalesPage {
  final List<SaleSummary> items;
  final int totalCount;
  final double periodSubtotal;
  final double periodDiscounts;
  final double periodTotal;
  final double periodReturned;
  final double periodNet;

  SalesPage({
    required this.items,
    required this.totalCount,
    required this.periodSubtotal,
    required this.periodDiscounts,
    required this.periodTotal,
    this.periodReturned = 0,
    this.periodNet = 0,
  });

  factory SalesPage.fromJson(Map<String, dynamic> json) {
    final total = _toDouble(json['PeriodTotal']);
    final returned = _toDouble(json['PeriodReturned']);
    return SalesPage(
      items: ((json['Items'] as List?) ?? [])
          .map((e) => SaleSummary.fromJson(e as Map<String, dynamic>))
          .toList(),
      totalCount: _toInt(json['TotalCount']),
      periodSubtotal: _toDouble(json['PeriodSubtotal']),
      periodDiscounts: _toDouble(json['PeriodDiscounts']),
      periodTotal: total,
      periodReturned: returned,
      // Sin PeriodNet (API vieja) el neto es el bruto menos lo devuelto.
      periodNet: json['PeriodNet'] == null ? total - returned : _toDouble(json['PeriodNet']),
    );
  }
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

  /// Lo devuelto de esta venta y lo que quedó cobrado. Vienen de `v_sales_net`;
  /// el listado tiene que mostrar el neto, no el facturado.
  final double totalReturned;
  final double netTotal;
  final SaleStatus status;

  SaleSummary({
    required this.id,
    required this.customerName,
    required this.sellerName,
    required this.saleDate,
    required this.subtotal,
    required this.totalDiscounts,
    required this.total,
    required this.isActive,
    this.totalReturned = 0,
    required this.netTotal,
    required this.status,
  });

  factory SaleSummary.fromJson(Map<String, dynamic> json) {
    final total = _toDouble(json['Total']);
    final returned = _toDouble(json['TotalReturned']);
    final isActive = json['IsActive'] == true;
    return SaleSummary(
      id: _toStr(json['Id']),
      customerName: _toStr(json['CustomerName']),
      sellerName: _toStr(json['SellerName']),
      saleDate: _toInstanteLocal(_toStr(json['SaleDate'])),
      subtotal: _toDouble(json['Subtotal']),
      totalDiscounts: _toDouble(json['TotalDiscounts']),
      total: total,
      isActive: isActive,
      totalReturned: returned,
      netTotal: json['NetTotal'] == null ? total - returned : _toDouble(json['NetTotal']),
      status: _toStatus(json['SaleStatus'], isActive: isActive, hasReturns: returned > 0),
    );
  }

  bool get hasReturns => status != SaleStatus.activa;
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

  /// `v_sales_net.sale_status`: la misma fuente que usa el listado, para que el
  /// detalle no pueda contradecirlo.
  final SaleStatus status;

  /// Lo devuelto según el servidor. Se usa como respaldo cuando `Returns` llega
  /// vacío (API vieja); si no, manda la suma de las devoluciones.
  final double _serverTotalReturned;

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
    required this.status,
    double serverTotalReturned = 0,
  }) : _serverTotalReturned = serverTotalReturned;

  factory SaleFull.fromJson(Map<String, dynamic> json) {
    final returns = ((json['Returns'] as List?) ?? [])
        .map((e) => SaleReturnInfo.fromJson(e as Map<String, dynamic>))
        .toList();
    final serverReturned = _toDouble(json['TotalReturned']);
    final isActive = json['IsActive'] == true;
    return SaleFull(
      id: _toStr(json['Id']),
      customerName: _toStr(json['CustomerName']),
      sellerName: _toStr(json['SellerName']),
      saleDate: _toInstanteLocal(_toStr(json['SaleDate'])),
      subtotal: _toDouble(json['Subtotal']),
      totalDiscounts: _toDouble(json['TotalDiscounts']),
      headerDiscountAmount: _toDouble(json['HeaderDiscountAmount']),
      total: _toDouble(json['Total']),
      isActive: isActive,
      detail: ((json['Detail'] as List?) ?? [])
          .map((e) => SaleDetailItem.fromJson(e as Map<String, dynamic>))
          .toList(),
      payments: ((json['Payments'] as List?) ?? [])
          .map((e) => SalePaymentInfo.fromJson(e as Map<String, dynamic>))
          .toList(),
      returns: returns,
      serverTotalReturned: serverReturned,
      status: _toStatus(json['SaleStatus'],
          isActive: isActive,
          hasReturns: returns.isNotEmpty || serverReturned > 0),
    );
  }

  bool get hasReturns => returns.isNotEmpty || _serverTotalReturned > 0;

  double get totalReturned => returns.isEmpty
      ? _serverTotalReturned
      : returns.fold(0.0, (s, r) => s + r.totalReturned);

  double get netTotal => total - totalReturned;

  /// Estado a mostrar en el detalle. Si ninguna línea tiene unidades pendientes
  /// la venta está devuelta entera, aunque `sale_status` diga parcial: eso pasa
  /// con devoluciones viejas, de antes de que `is_full_return` mirara lo
  /// acumulado y apagara `is_active`.
  SaleStatus get effectiveStatus {
    if (status != SaleStatus.conDevolucion) return status;
    final todoDevuelto = detail.isNotEmpty &&
        detail.every((d) => d.quantity - returnedFor(d.id) <= 0);
    return todoDevuelto ? SaleStatus.devueltaTotal : SaleStatus.conDevolucion;
  }

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
