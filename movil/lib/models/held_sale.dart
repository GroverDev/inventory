import 'dart:convert';
import 'dart:math' as math;

import 'cash_session.dart';
import 'catalog.dart';
import 'product.dart';
import 'sale.dart';

/// Venta en espera, como la lista el servidor (GET api/HeldSale).
class HeldSale {
  final String id;
  final String label;
  final String customerName;
  final String userName;
  final DateTime created;
  final int itemsCount;
  final double total;

  /// Solo al retomarla: el carrito guardado, en JSON.
  final String payload;

  const HeldSale({
    required this.id,
    this.label = '',
    this.customerName = '',
    this.userName = '',
    required this.created,
    this.itemsCount = 0,
    this.total = 0,
    this.payload = '',
  });

  factory HeldSale.fromJson(Map<String, dynamic> j) => HeldSale(
        id: (j['Id'] ?? '').toString(),
        label: j['Label'] ?? '',
        customerName: j['CustomerName'] ?? '',
        userName: j['UserName'] ?? '',
        created: DateTime.tryParse(j['Created']?.toString() ?? '')?.toLocal() ?? DateTime.now(),
        itemsCount: j['ItemsCount'] ?? 0,
        total: (j['Total'] ?? 0).toDouble(),
        payload: j['Payload'] ?? '',
      );
}

/// Descuento global guardado con la venta.
typedef HeldHeader = ({String id, String label, String type, double value});

/// Carrito reconstruido de una venta en espera, y lo que cambió al retomarla.
class RestoredCart {
  final List<SaleLine> lines;
  final Customer? customer;
  final HeldHeader? header;

  /// Qué se ajustó (precio nuevo, descuento quitado, producto que ya no está),
  /// para avisarle al cajero antes de cobrar.
  final List<String> notices;

  const RestoredCart(this.lines, this.customer, this.header, this.notices);
}

/// Formato del carrito en espera, compartido con el POS web.
///
/// `v: 1` con el cliente, las líneas como el `SaleDetail` de la web y el
/// descuento global. Una venta puesta en espera en la web se retoma en el
/// móvil y al revés, así que las dos escriben y leen exactamente esto.
class HeldSalePayload {
  static const version = 1;

  static String build({required List<SaleLine> lines, Customer? customer, HeldHeader? header}) =>
      jsonEncode({
        'v': version,
        'customer': customer == null
            ? null
            : {
                'Id': customer.id,
                'FullName': customer.fullName,
                'DocumentNumber': customer.documentNumber,
                'Email': '',
                'Cellphone': '',
                'IsActive': true,
              },
        'lines': [for (final l in lines) _line(l)],
        'header': {
          'id': header?.id ?? '',
          'label': header?.label ?? '',
          'type': header?.type ?? '',
          'value': header?.value ?? 0,
        },
      });

  /// Una línea con todos los campos del `SaleDetail` web: la web la usa tal
  /// cual al retomarla.
  static Map<String, dynamic> _line(SaleLine l) => {
        'Id': '',
        'SaleId': '',
        'ProductId': l.product.id,
        'ProductName': l.product.productName,
        'LaboratoryName': l.product.laboratoryName,
        'Quantity': l.quantity,
        'UnitPrice': l.unitPrice,
        'LineSubtotal': l.lineSubtotal,
        'LineTotalDiscounts': l.lineTotalDiscounts,
        'LineTotal': l.lineTotal,
        'EffectiveUnitPrice': 0,
        'LotCode': null,
        'ExpiryDate': null,
        'SerialNumber': null,
        'SerialNumbers': <String>[],
        'isSelected': false,
        'DiscountId': l.discountId,
        'DiscountLabel': l.discountLabel,
        'DiscountType': l.discountType,
        'DiscountValue': l.discountValue,
      };

  /// Motivo por el que un descuento guardado ya no se aplica tal cual, o '' si
  /// sigue valiendo. Mismas reglas que la web: uno del catálogo dado de baja,
  /// o uno manual que supera el tope de la sucursal o que pide supervisor (la
  /// autorización de entonces no viaja con la venta).
  static String invalidDiscount(String id, String type, double value,
      {required Set<String> activeDiscountIds, required PosSettings settings, required bool isCashier}) {
    if (id.isNotEmpty) return activeDiscountIds.contains(id) ? '' : 'ya no está en el catálogo';
    if (type.isEmpty || value <= 0) return '';
    final pct = type == 'Percentage';
    final max = pct ? settings.maxDiscountPct : settings.maxDiscountAmount;
    if (max != null && value > max) return 'supera el tope máximo';
    final cajero = pct ? settings.maxCashierDiscountPct : settings.maxCashierDiscountAmount;
    if (isCashier && value > cajero) return 'requiere autorizar de nuevo';
    return '';
  }

  /// Reconstruye el carrito con lo vigente: el precio de hoy, y sin los
  /// descuentos que ya no valen. Al cobrar, el servidor valida todo de nuevo.
  ///
  /// [products] son los del POS (activos, de la sucursal), por id.
  static RestoredCart restore(
    String payload, {
    required Map<String, Product> products,
    required Set<String> activeDiscountIds,
    required PosSettings settings,
    required bool isCashier,
  }) {
    final saved = jsonDecode(payload) as Map<String, dynamic>;
    if (saved['v'] != version) {
      throw const FormatException('La venta en espera tiene un formato que esta versión de la app no conoce.');
    }

    String invalid(String id, String type, double value) => invalidDiscount(id, type, value,
        activeDiscountIds: activeDiscountIds, settings: settings, isCashier: isCashier);

    final notices = <String>[];
    final lines = <SaleLine>[];
    for (final raw in (saved['lines'] as List? ?? const [])) {
      final l = raw as Map<String, dynamic>;
      final name = l['ProductName'] ?? '';
      final prod = products[(l['ProductId'] ?? '').toString()];
      if (prod == null) {
        notices.add('«$name» ya no está disponible y se quitó.');
        continue;
      }
      final savedPrice = (l['UnitPrice'] as num? ?? 0).toDouble();
      if ((prod.salePrice - savedPrice).abs() > 0.004) {
        notices.add('«$name»: el precio cambió a ${prod.salePrice.toStringAsFixed(2)}.');
      }
      if ((l['SerialNumbers'] as List? ?? const []).isNotEmpty) {
        notices.add('«$name»: las series elegidas no se conservan; se asignan al cobrar.');
      }

      var id = (l['DiscountId'] ?? '').toString();
      var label = (l['DiscountLabel'] ?? '').toString();
      var type = (l['DiscountType'] ?? '').toString();
      var value = (l['DiscountValue'] as num? ?? 0).toDouble();
      final motivo = invalid(id, type, value);
      if (motivo.isNotEmpty) {
        notices.add('Se quitó el descuento de «$name»: $motivo.');
        id = label = type = '';
        value = 0.0;
      }
      lines.add(SaleLine(
        product: prod,
        quantity: math.max(1, (l['Quantity'] as num?)?.toInt() ?? 1),
        discountId: id,
        discountLabel: label,
        discountType: type,
        discountValue: value,
      ));
    }

    HeldHeader? header;
    final h = saved['header'];
    if (h is Map && (h['type'] ?? '').toString().isNotEmpty && (h['value'] ?? 0) > 0) {
      final hid = (h['id'] ?? '').toString();
      final htype = h['type'].toString();
      final hvalue = (h['value'] as num).toDouble();
      final motivo = invalid(hid, htype, hvalue);
      if (motivo.isNotEmpty) {
        notices.add('Se quitó el descuento global: $motivo.');
      } else {
        header = (id: hid, label: (h['label'] ?? '').toString(), type: htype, value: hvalue);
      }
    }

    final c = saved['customer'];
    final customer = c is Map<String, dynamic> && (c['Id'] ?? '').toString().isNotEmpty
        ? Customer.fromJson(c)
        : null;

    return RestoredCart(lines, customer, header, notices);
  }
}
