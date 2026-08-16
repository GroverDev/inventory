import 'package:intl/intl.dart';

import 'product.dart';

/// Formato con el que viajan todas las fechas de compras hacia el API.
///
/// El backend las recibe como texto y las convierte con
/// `CultureInfo.InvariantCulture` (InventoryMappingConfig), que espera
/// MM/dd/yyyy o ISO. Mandar dd/MM/yyyy revienta a partir del día 13 y, peor,
/// se acepta con el mes y el día cambiados antes de esa fecha. La webapp manda
/// ISO (viene de un `<input type="date">`); esto hace lo mismo.
final apiDateFormat = DateFormat('yyyy-MM-dd');

/// Estados de una orden de compra (`PurchaseStatusEnum` del backend).
///
/// Los deriva el servidor a partir de los saldos recibidos; el cliente nunca
/// los elige, solo los lee para saber qué acciones ofrecer.
class PurchaseStatusIds {
  PurchaseStatusIds._();

  static const int requested = 1;
  static const int partiallyReceived = 2;
  static const int totallyReceived = 3;
  static const int cancelled = 4;
  static const int closed = 5;

  /// Los cinco estados en el orden en que los recorre el usuario.
  static const List<int> all = [
    requested,
    partiallyReceived,
    totallyReceived,
    closed,
    cancelled,
  ];
}

/// Etiqueta corta de un estado. Se mantiene en el cliente, igual que en la
/// webapp (`statusLabel`), porque el enum es fijo en el backend y así la lista
/// no depende de una llamada extra al catálogo para poder filtrar.
String purchaseStatusLabel(int statusId) => switch (statusId) {
      PurchaseStatusIds.requested => 'Solicitado',
      PurchaseStatusIds.partiallyReceived => 'Parc. recibido',
      PurchaseStatusIds.totallyReceived => 'Recibido',
      PurchaseStatusIds.cancelled => 'Cancelado',
      PurchaseStatusIds.closed => 'Cerrado',
      _ => 'Estado $statusId',
    };

/// Solo estas dos situaciones admiten recibir mercadería
/// (`PurchaseReceiptPolicy.EnsureCanReceive`). Ocultar la acción es comodidad:
/// el servidor vuelve a validarla.
bool canReceivePurchase(int statusId) =>
    statusId == PurchaseStatusIds.requested ||
    statusId == PurchaseStatusIds.partiallyReceived;

/// El cierre con faltante solo aplica sobre una orden que ya recibió algo pero
/// no todo (`PurchaseReceiptPolicy.EnsureCanClose`). Sin recepciones, lo que
/// corresponde es cancelar.
bool canClosePurchase(int statusId) =>
    statusId == PurchaseStatusIds.partiallyReceived;

/// Línea de un pedido / compra (espejo de PurchaseDetailRequest).
class PurchaseLine {
  final Product product;
  int orderedQuantity;
  double orderUnitPrice;

  PurchaseLine({
    required this.product,
    this.orderedQuantity = 1,
    double? orderUnitPrice,
  }) : orderUnitPrice = orderUnitPrice ?? product.salePrice;

  double get orderFinalPrice => orderedQuantity * orderUnitPrice;

  Map<String, dynamic> toJson() => {
        'Id': '',
        'PurchaseId': '',
        'ProductId': product.id,
        'OrderUnitPrice': orderUnitPrice,
        'OrderedQuantity': orderedQuantity,
        'OrderFinalPrice': orderFinalPrice,
        'DeliveryUnitPrice': 0,
        'DeliveredQuantity': 0,
        'DeliveryFinalPrice': 0,
        'PurchaseStatusId': 0,
        'ProductName': product.productName,
      };
}

/// Payload para POST api/Purchases (espejo de PurchaseRequest).
class PurchaseRequest {
  final String providerId;
  final String providerName;
  final int purchaseStatusId;
  final DateTime estimatedDeliveryDate;
  final List<PurchaseLine> detail;

  PurchaseRequest({
    required this.providerId,
    required this.providerName,
    required this.purchaseStatusId,
    required this.estimatedDeliveryDate,
    required this.detail,
  });

  double get total => detail.fold(0, (s, l) => s + l.orderFinalPrice);

  Map<String, dynamic> toJson() => {
        'Id': '',
        'PurchaseDate': apiDateFormat.format(DateTime.now()),
        'Total': total,
        'IsActive': true,
        'ProviderId': providerId,
        'EstimatedDeliveryDate': apiDateFormat.format(estimatedDeliveryDate),
        'PurchaseStatusId': purchaseStatusId,
        'ProviderName': providerName,
        'PurchaseStatusName': '',
        'Detail': detail.map((d) => d.toJson()).toList(),
      };
}

/// Resumen de un pedido leído desde api/Purchases (campos flexibles).
class PurchaseSummary {
  final String id;
  final String providerName;
  final String purchaseDate;
  final double total;
  final String statusName;
  final int statusId;

  PurchaseSummary({
    required this.id,
    required this.providerName,
    required this.purchaseDate,
    required this.total,
    required this.statusName,
    required this.statusId,
  });

  factory PurchaseSummary.fromJson(Map<String, dynamic> j) => PurchaseSummary(
        id: (j['Id'] ?? '').toString(),
        providerName: j['ProviderName'] ?? '',
        purchaseDate: j['PurchaseDate']?.toString() ?? '',
        total: (j['Total'] ?? 0).toDouble(),
        statusName: j['PurchaseStatusName'] ?? j['StatusName'] ?? '',
        statusId: (j['PurchaseStatusId'] ?? 0) as int,
      );
}

/// Línea del detalle de un pedido, tal como la devuelve GET api/Purchases/{id}.
///
/// Los saldos vienen calculados por el servidor: `SqlPurchaseDetail` suma el
/// log de recepciones (`purchases_delivery_detail`) y devuelve el acumulado
/// recibido y el pendiente. El cliente no los deriva, solo los muestra.
class PurchaseOrderLine {
  final String productId;
  final String productName;
  final int orderedQuantity;
  final int receivedQuantity;
  final int pendingQuantity;
  final double orderUnitPrice;

  /// Seguimiento del producto: 'none', 'lot' o 'serial'. Lo manda el servidor
  /// en el detalle del pedido y decide si la recepción tiene que pedir el lote.
  final String trackingMode;

  PurchaseOrderLine({
    required this.productId,
    required this.productName,
    required this.orderedQuantity,
    required this.receivedQuantity,
    required this.pendingQuantity,
    required this.orderUnitPrice,
    this.trackingMode = 'none',
  });

  /// El servidor RECHAZA la recepción de estos productos si no se indica el
  /// lote, y con ella se cae la entrega entera, no solo esta línea.
  bool get usesLot => trackingMode == 'lot';

  /// Una unidad, un número de serie: el servidor exige tantos números como
  /// unidades se reciban.
  bool get usesSerial => trackingMode == 'serial';

  factory PurchaseOrderLine.fromJson(Map<String, dynamic> j) {
    final ordered = (j['OrderedQuantity'] ?? 0) as int;
    final received = (j['ReceivedQuantity'] ?? 0) as int;
    return PurchaseOrderLine(
      productId: (j['ProductId'] ?? '').toString(),
      productName: j['ProductName'] ?? '',
      orderedQuantity: ordered,
      receivedQuantity: received,
      // Si el pendiente no viniera, se deriva en lugar de quedar en cero: una
      // línea con saldo real no puede presentarse como ya completa.
      pendingQuantity: (j['PendingQuantity'] as int?) ??
          (ordered - received).clamp(0, ordered),
      orderUnitPrice: (j['OrderUnitPrice'] ?? 0).toDouble(),
      // Un pedido servido por una API vieja no trae el campo: sin seguimiento
      // es el caso simple y la pantalla se comporta como siempre.
      trackingMode: (j['TrackingMode'] ?? 'none').toString(),
    );
  }
}

/// Pedido completo leído desde GET api/Purchases/{id}.
class PurchaseOrder {
  final String id;
  final String providerName;
  final String purchaseDate;
  final int statusId;
  final String statusName;
  final double total;
  final List<PurchaseOrderLine> detail;

  PurchaseOrder({
    required this.id,
    required this.providerName,
    required this.purchaseDate,
    required this.statusId,
    required this.statusName,
    required this.total,
    required this.detail,
  });

  factory PurchaseOrder.fromJson(Map<String, dynamic> j) => PurchaseOrder(
        id: (j['Id'] ?? '').toString(),
        providerName: j['ProviderName'] ?? '',
        purchaseDate: j['PurchaseDate']?.toString() ?? '',
        statusId: (j['PurchaseStatusId'] ?? 0) as int,
        statusName: j['PurchaseStatusName'] ?? '',
        total: (j['Total'] ?? 0).toDouble(),
        detail: ((j['Detail'] ?? []) as List)
            .map((e) => PurchaseOrderLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  bool get hasPending => detail.any((l) => l.pendingQuantity > 0);
}

/// Línea editable de una recepción: cuánto se recibe ahora y a qué precio.
class PurchaseDeliveryLine {
  final PurchaseOrderLine source;
  int deliveryQuantity;
  double unitPrice;

  /// Lote que llegó, leído de la etiqueta de la caja. Obligatorio si el
  /// producto usa lotes; ignorado por el servidor si no.
  String lotCode;

  /// Vencimiento del lote o de la unidad. Opcional.
  DateTime? expiryDate;

  /// Números de serie recibidos, uno por unidad. Solo se usa cuando el producto
  /// se identifica por serie.
  List<String> serialNumbers;

  PurchaseDeliveryLine(this.source)
      // Se propone recibir el saldo pendiente al precio pactado; ambos se
      // corrigen si el proveedor entregó o facturó otra cosa.
      : deliveryQuantity = source.pendingQuantity,
        unitPrice = source.orderUnitPrice,
        lotCode = '',
        serialNumbers = [];

  double get subtotal => deliveryQuantity * unitPrice;

  bool get usesLot => source.usesLot;

  bool get usesSerial => source.usesSerial;

  /// Línea con series que no declaró tantas como unidades va a recibir. El
  /// servidor la rechaza y se cae la entrega completa.
  bool get serialsMismatch =>
      deliveryQuantity > 0 && usesSerial && serialNumbers.length != deliveryQuantity;

  /// Línea que se va a recibir y todavía no declaró su lote: el servidor la
  /// rechazaría y se perdería la entrega completa.
  bool get missingLot =>
      deliveryQuantity > 0 && usesLot && lotCode.trim().isEmpty;

  Map<String, dynamic> toJson() => {
        'ProductId': source.productId,
        'DeliveryQuantity': deliveryQuantity,
        'UnitPrice': unitPrice,
        // Solo viajan en las líneas con lote: en el resto no hay nada que
        // declarar, y el servidor trata la ausencia igual que el vacío.
        if (usesLot) 'LotCode': lotCode.trim(),
        if ((usesLot || usesSerial) && expiryDate != null)
          'ExpiryDate': apiDateFormat.format(expiryDate!),
        if (usesSerial) 'SerialNumbers': serialNumbers,
      };
}

/// Payload para PUT api/Purchases/reciveOrders/{id}
/// (espejo de PurchaseDeliveryRequest).
class PurchaseDelivery {
  final String purchaseId;
  final DateTime deliveryDate;
  final List<PurchaseDeliveryLine> detail;

  /// Uid de la operación, generado una vez al abrir la pantalla. La BD tiene un
  /// índice único sobre él: si el envío se reintenta (timeout, doble toque), el
  /// segundo choca contra el constraint y el stock no se ingresa dos veces.
  /// Tiene que sobrevivir al reintento, así que no se regenera al reenviar.
  final String operationUid;

  PurchaseDelivery({
    required this.purchaseId,
    required this.deliveryDate,
    required this.detail,
    required this.operationUid,
  });

  /// Solo las líneas con mercadería recibida: el backend descarta el resto y
  /// exige al menos una.
  List<PurchaseDeliveryLine> get receivedLines =>
      detail.where((l) => l.deliveryQuantity > 0).toList();

  double get total => receivedLines.fold(0, (s, l) => s + l.subtotal);

  /// Líneas que van a recibirse sin declarar su lote. Se corta acá el envío:
  /// el servidor rechaza la transacción entera, así que dejarlo pasar obliga
  /// al usuario a recargar la pantalla y recargar todo lo que ya había puesto.
  List<PurchaseDeliveryLine> get linesMissingLot =>
      receivedLines.where((l) => l.missingLot).toList();

  /// Líneas con series cuya lista no coincide con la cantidad recibida.
  List<PurchaseDeliveryLine> get linesWithSerialMismatch =>
      receivedLines.where((l) => l.serialsMismatch).toList();

  /// La entrega es parcial si alguna línea deja saldo sin recibir.
  bool get isPartial =>
      detail.any((l) => l.deliveryQuantity < l.source.pendingQuantity);

  Map<String, dynamic> toJson() => {
        'PurchaseId': purchaseId,
        'IsActive': true,
        'DeliveryDate': apiDateFormat.format(deliveryDate),
        'OperationUid': operationUid,
        // El estado resultante lo deriva el servidor de los saldos: mandarlo
        // no sirve de nada, lo ignora.
        'Detail': receivedLines.map((l) => l.toJson()).toList(),
      };
}
