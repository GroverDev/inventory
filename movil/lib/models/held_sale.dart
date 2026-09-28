import 'catalog.dart';

/// Venta en espera tal como la lista el servidor (GET api/HeldSale).
///
/// Vive por sucursal (`current_branch()`), no por caja: cualquier caja la ve
/// y la puede retomar. Espejo de `HeldSale` en
/// `frontend/src/modules/inventory/composables/useHeldSales.ts`.
class HeldSale {
  final String id;
  final String label;
  final String customerName;
  final String userName;
  final DateTime created;
  final int itemsCount;
  final double total;

  /// Solo viene al retomarla (POST .../take): el carrito guardado, en JSON.
  final String? payload;

  HeldSale({
    required this.id,
    required this.label,
    required this.customerName,
    required this.userName,
    required this.created,
    required this.itemsCount,
    required this.total,
    this.payload,
  });

  factory HeldSale.fromJson(Map<String, dynamic> j) => HeldSale(
        id: (j['Id'] ?? '').toString(),
        label: j['Label'] ?? '',
        customerName: j['CustomerName'] ?? '',
        userName: j['UserName'] ?? '',
        created: DateTime.tryParse(j['Created']?.toString() ?? '')?.toLocal() ??
            DateTime.now(),
        itemsCount: j['ItemsCount'] ?? 0,
        total: (j['Total'] ?? 0).toDouble(),
        payload: j['Payload']?.toString(),
      );
}

/// ── Payload guardado en el carrito en espera ──────────────────
///
/// Mismo formato que escribe y lee la web (`HeldPayload` en
/// `PointOfSaleView.vue`). El servidor no lo interpreta (`HeldSaleRequest.Payload`
/// es JSON opaco), así que el formato es un contrato entre clientes, no con el
/// backend: una venta aparcada en la web se retoma en el móvil y viceversa
/// SOLO si se respeta esta forma exacta, mayúsculas incluidas — el nivel
/// superior y `header` van en minúscula, `customer` y cada línea van en
/// PascalCase porque son el JSON de las clases `Customer` y `SaleDetail` de la
/// web serializadas tal cual.
class HeldPayload {
  final int v;
  final HeldCustomer? customer;
  final List<HeldLine> lines;
  final HeldHeaderDiscount header;

  const HeldPayload({
    this.v = 1,
    this.customer,
    required this.lines,
    required this.header,
  });

  factory HeldPayload.fromJson(Map<String, dynamic> j) => HeldPayload(
        v: (j['v'] as num?)?.toInt() ?? 1,
        customer: j['customer'] == null
            ? null
            : HeldCustomer.fromJson(j['customer'] as Map<String, dynamic>),
        lines: ((j['lines'] as List?) ?? [])
            .map((e) => HeldLine.fromJson(e as Map<String, dynamic>))
            .toList(),
        header: HeldHeaderDiscount.fromJson(
            (j['header'] as Map<String, dynamic>?) ?? const {}),
      );

  Map<String, dynamic> toJson() => {
        'v': v,
        'customer': customer?.toJson(),
        'lines': lines.map((l) => l.toJson()).toList(),
        'header': header.toJson(),
      };
}

/// Cliente dentro del payload (PascalCase: es el `Customer` de la web).
class HeldCustomer {
  final String id;
  final String fullName;
  final String documentNumber;

  const HeldCustomer({
    required this.id,
    required this.fullName,
    this.documentNumber = '',
  });

  factory HeldCustomer.fromJson(Map<String, dynamic> j) => HeldCustomer(
        id: (j['Id'] ?? '').toString(),
        fullName: j['FullName'] ?? '',
        documentNumber: j['DocumentNumber'] ?? '',
      );

  Map<String, dynamic> toJson() =>
      {'Id': id, 'FullName': fullName, 'DocumentNumber': documentNumber};

  Customer toCustomer() =>
      Customer(id: id, fullName: fullName, documentNumber: documentNumber);

  factory HeldCustomer.fromCustomer(Customer c) => HeldCustomer(
      id: c.id, fullName: c.fullName, documentNumber: c.documentNumber);
}

/// Línea del carrito dentro del payload (PascalCase: es el `SaleDetail` de la
/// web). Los totales de línea se guardan solo por completitud del contrato:
/// quien retoma la venta los vuelve a calcular contra el precio y el
/// descuento vigentes, nunca confía en lo guardado.
class HeldLine {
  final String productId;
  final String productName;
  final double unitPrice;
  final int quantity;
  final String discountId;
  final String discountLabel;
  final String discountType;
  final double discountValue;
  final double lineSubtotal;
  final double lineTotalDiscounts;
  final double lineTotal;
  final List<String> serialNumbers;

  const HeldLine({
    required this.productId,
    required this.productName,
    required this.unitPrice,
    required this.quantity,
    this.discountId = '',
    this.discountLabel = '',
    this.discountType = '',
    this.discountValue = 0,
    this.lineSubtotal = 0,
    this.lineTotalDiscounts = 0,
    this.lineTotal = 0,
    this.serialNumbers = const [],
  });

  factory HeldLine.fromJson(Map<String, dynamic> j) => HeldLine(
        productId: (j['ProductId'] ?? '').toString(),
        productName: j['ProductName'] ?? '',
        unitPrice: (j['UnitPrice'] ?? 0).toDouble(),
        quantity: (j['Quantity'] as num? ?? 0).toInt(),
        discountId: j['DiscountId'] ?? '',
        discountLabel: j['DiscountLabel'] ?? '',
        discountType: j['DiscountType'] ?? '',
        discountValue: (j['DiscountValue'] ?? 0).toDouble(),
        lineSubtotal: (j['LineSubtotal'] ?? 0).toDouble(),
        lineTotalDiscounts: (j['LineTotalDiscounts'] ?? 0).toDouble(),
        lineTotal: (j['LineTotal'] ?? 0).toDouble(),
        serialNumbers: ((j['SerialNumbers'] as List?) ?? [])
            .map((e) => e.toString())
            .toList(),
      );

  Map<String, dynamic> toJson() => {
        'ProductId': productId,
        'ProductName': productName,
        'Quantity': quantity,
        'UnitPrice': unitPrice,
        'LineSubtotal': lineSubtotal,
        'LineTotalDiscounts': lineTotalDiscounts,
        'LineTotal': lineTotal,
        'DiscountId': discountId,
        'DiscountLabel': discountLabel,
        'DiscountType': discountType,
        'DiscountValue': discountValue,
        'SerialNumbers': serialNumbers,
      };
}

/// Descuento global dentro del payload. A diferencia de `customer` y `lines`,
/// la web lo escribe en minúscula (`{ id, label, type, value }`): no es el
/// JSON de una clase, es un objeto literal. Hay que respetar esa asimetría
/// para que el payload se lea igual en los dos lados.
class HeldHeaderDiscount {
  final String id;
  final String label;
  final String type;
  final double value;

  const HeldHeaderDiscount({
    this.id = '',
    this.label = '',
    this.type = '',
    this.value = 0,
  });

  factory HeldHeaderDiscount.fromJson(Map<String, dynamic> j) =>
      HeldHeaderDiscount(
        id: j['id'] ?? '',
        label: j['label'] ?? '',
        type: j['type'] ?? '',
        value: (j['value'] ?? 0).toDouble(),
      );

  Map<String, dynamic> toJson() =>
      {'id': id, 'label': label, 'type': type, 'value': value};
}
