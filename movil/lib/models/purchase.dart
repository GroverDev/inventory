import 'package:intl/intl.dart';

import 'product.dart';

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
        'PurchaseDate': DateFormat('dd/MM/yyyy').format(DateTime.now()),
        'Total': total,
        'IsActive': true,
        'ProviderId': providerId,
        'EstimatedDeliveryDate':
            DateFormat('dd/MM/yyyy').format(estimatedDeliveryDate),
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

  PurchaseSummary({
    required this.id,
    required this.providerName,
    required this.purchaseDate,
    required this.total,
    required this.statusName,
  });

  factory PurchaseSummary.fromJson(Map<String, dynamic> j) => PurchaseSummary(
        id: (j['Id'] ?? '').toString(),
        providerName: j['ProviderName'] ?? '',
        purchaseDate: j['PurchaseDate']?.toString() ?? '',
        total: (j['Total'] ?? 0).toDouble(),
        statusName: j['PurchaseStatusName'] ?? j['StatusName'] ?? '',
      );
}
