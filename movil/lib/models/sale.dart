import 'dart:math' as math;

import 'product.dart';

double _round(double v) => double.parse(v.toStringAsFixed(2));

/// Línea del carrito / detalle de venta (espejo de SaleDetailRequest).
class SaleLine {
  final Product product;
  int quantity;

  // ── Descuento por línea ──────────────────────────────────
  String discountId;
  String discountLabel;

  /// '' | 'Percentage' | 'FixedAmount'
  String discountType;
  double discountValue;

  SaleLine({
    required this.product,
    this.quantity = 1,
    this.discountId = '',
    this.discountLabel = '',
    this.discountType = '',
    this.discountValue = 0,
  });

  double get unitPrice => product.salePrice;
  double get lineSubtotal => _round(unitPrice * quantity);

  double get lineTotalDiscounts {
    if (discountValue <= 0) return 0;
    if (discountType == 'Percentage') {
      return _round(math.min(lineSubtotal * discountValue / 100, lineSubtotal));
    }
    if (discountType == 'FixedAmount') {
      return _round(math.min(discountValue, lineSubtotal));
    }
    return 0;
  }

  double get lineTotal => _round(lineSubtotal - lineTotalDiscounts);
  bool get hasDiscount => lineTotalDiscounts > 0;

  void clearDiscount() {
    discountId = '';
    discountLabel = '';
    discountType = '';
    discountValue = 0;
  }

  Map<String, dynamic> toJson() => {
        'Id': '',
        'SaleId': '',
        'ProductId': product.id,
        'Quantity': quantity,
        'UnitPrice': unitPrice,
        'LineSubtotal': lineSubtotal,
        'LineTotalDiscounts': lineTotalDiscounts,
        'LineTotal': lineTotal,
        'DiscountId': discountId,
        'DiscountType': discountType,
        'DiscountValue': discountValue,
      };
}

/// Pago de la venta (espejo de SalePaymentRequest).
class SalePayment {
  final String paymentMethodId;
  final String paymentMethodName;
  final String iconCss;
  final double amountGiven;
  final double amountReturned;

  SalePayment({
    required this.paymentMethodId,
    required this.paymentMethodName,
    this.iconCss = '',
    required this.amountGiven,
    this.amountReturned = 0,
  });

  Map<String, dynamic> toJson() => {
        'PaymentMethodId': paymentMethodId,
        'PaymentMethodName': paymentMethodName,
        'AmountGiven': amountGiven,
        'AmountReturned': amountReturned,
      };
}

/// Payload para POST api/Sales (espejo de SaleRequest).
class SaleRequest {
  final String customerId;
  final String cashSessionId;
  final List<SaleLine> detail;
  final List<SalePayment> payments;

  // ── Descuento global (cabecera) ──────────────────────────
  final String headerDiscountId;
  final String headerDiscountType;
  final double headerDiscountValue;
  final double headerDiscountAmount;

  /// Token de un supervisor que autorizó un descuento sobre el límite.
  final String supervisorAuthToken;

  SaleRequest({
    required this.customerId,
    required this.cashSessionId,
    required this.detail,
    required this.payments,
    this.headerDiscountId = '',
    this.headerDiscountType = '',
    this.headerDiscountValue = 0,
    this.headerDiscountAmount = 0,
    this.supervisorAuthToken = '',
  });

  double get subtotal =>
      _round(detail.fold(0.0, (s, l) => s + l.lineSubtotal));
  double get totalLineDiscounts =>
      _round(detail.fold(0.0, (s, l) => s + l.lineTotalDiscounts));
  double get totalDiscounts =>
      _round(totalLineDiscounts + headerDiscountAmount);
  double get total => _round(subtotal - totalDiscounts);

  Map<String, dynamic> toJson() => {
        'Id': '',
        'CustomerId': customerId,
        // En UTC y en ISO 8601 (con la "Z"), igual que el POS web. Antes se
        // mandaba 'dd/MM/yyyy HH:mm:ss' con la hora local: sin marca de zona el
        // backend la tomaba como si ya fuera UTC y la venta quedaba guardada 4
        // horas antes de haber ocurrido.
        'SaleDate': DateTime.now().toUtc().toIso8601String(),
        'Subtotal': subtotal,
        'TotalDiscounts': totalDiscounts,
        'Total': total,
        'IsActive': true,
        'CashSessionId': cashSessionId,
        'HeaderDiscountId': headerDiscountId,
        'HeaderDiscountAmount': headerDiscountAmount,
        'HeaderDiscountType': headerDiscountType,
        'HeaderDiscountValue': headerDiscountValue,
        'SupervisorAuthToken': supervisorAuthToken,
        'Detail': detail.map((d) => d.toJson()).toList(),
        'Payments': payments.map((p) => p.toJson()).toList(),
      };
}
