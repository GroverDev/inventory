import '../../core/theme/app_theme.dart';
import '../../models/catalog.dart';
import '../../models/cash_session.dart';
import '../../models/discount.dart';
import '../../models/held_sale.dart';
import '../../models/product.dart';
import '../../models/sale.dart';
import '../../providers/cart_provider.dart';

/// Arma el payload a guardar a partir del carrito en curso. Espejo de lo que
/// arma `holdCurrentSale` en `PointOfSaleView.vue`.
HeldPayload buildHeldPayload(CartProvider cart) => HeldPayload(
      customer: cart.customer == null
          ? null
          : HeldCustomer.fromCustomer(cart.customer!),
      lines: cart.lines
          .map((l) => HeldLine(
                productId: l.product.id,
                productName: l.product.productName,
                unitPrice: l.unitPrice,
                quantity: l.quantity,
                discountId: l.discountId,
                discountLabel: l.discountLabel,
                discountType: l.discountType,
                discountValue: l.discountValue,
                lineSubtotal: l.lineSubtotal,
                lineTotalDiscounts: l.lineTotalDiscounts,
                lineTotal: l.lineTotal,
                serialNumbers: l.serialNumbers,
              ))
          .toList(),
      header: HeldHeaderDiscount(
        id: cart.headerDiscountId,
        label: cart.headerDiscountLabel,
        type: cart.headerDiscountType,
        value: cart.headerDiscountValue,
      ),
    );

/// Resultado de resolver una venta en espera contra el catálogo vigente.
class ResumedSale {
  final List<SaleLine> lines;
  final Customer? customer;
  final String headerDiscountId;
  final String headerDiscountLabel;
  final String headerDiscountType;
  final double headerDiscountValue;

  /// Lo que cambió respecto a lo guardado: precio actualizado, producto que
  /// ya no está, descuento que ya no aplica. Se muestra, no bloquea.
  final List<String> warnings;

  const ResumedSale({
    required this.lines,
    required this.customer,
    required this.headerDiscountId,
    required this.headerDiscountLabel,
    required this.headerDiscountType,
    required this.headerDiscountValue,
    required this.warnings,
  });
}

/// Motivo por el que un descuento guardado ya no se puede aplicar tal cual, o
/// '' si sigue valiendo. Espejo de `invalidDiscount` en `PointOfSaleView.vue`:
/// uno del catálogo dado de baja, uno manual que pide un supervisor (la
/// autorización de entonces no viaja con la venta) o que supera el tope
/// máximo de la sucursal.
String _invalidDiscount({
  required String id,
  required String type,
  required double value,
  required List<Discount> catalog,
  required PosSettings settings,
  required bool isCashier,
}) {
  if (id.isNotEmpty) {
    return catalog.any((d) => d.id == id) ? '' : 'ya no está en el catálogo';
  }
  if (type.isEmpty || value <= 0) return '';
  final pct = type == 'Percentage';
  final max = pct ? settings.maxDiscountPct : settings.maxDiscountAmount;
  if (max != null && value > max) return 'supera el tope máximo';
  final cajero = pct ? settings.maxCashierDiscountPct : settings.maxCashierDiscountAmount;
  if (isCashier && value > cajero) return 'requiere autorizar de nuevo';
  return '';
}

/// Resuelve el carrito guardado contra el catálogo, precios y descuentos
/// vigentes. Espejo de `resumeHeldSale` en `PointOfSaleView.vue`: no reserva
/// nada, así que lo que valía al aparcar la venta puede haber cambiado.
ResumedSale resolveHeldSale(
  HeldPayload payload, {
  required List<Product> catalog,
  required List<Discount> discounts,
  required PosSettings settings,
  required bool isCashier,
}) {
  final warnings = <String>[];
  final lines = <SaleLine>[];

  for (final l in payload.lines) {
    Product? prod;
    for (final p in catalog) {
      if (p.id == l.productId) {
        prod = p;
        break;
      }
    }
    if (prod == null) {
      warnings.add('«${l.productName}» ya no está disponible y se quitó.');
      continue;
    }
    if (prod.salePrice != l.unitPrice) {
      warnings.add(
          '«${l.productName}»: el precio cambió a ${currency(prod.salePrice)}.');
    }

    var discountId = l.discountId;
    var discountLabel = l.discountLabel;
    var discountType = l.discountType;
    var discountValue = l.discountValue;
    final motivo = _invalidDiscount(
      id: discountId,
      type: discountType,
      value: discountValue,
      catalog: discounts,
      settings: settings,
      isCashier: isCashier,
    );
    if (motivo.isNotEmpty) {
      warnings.add('Se quitó el descuento de «${l.productName}»: $motivo.');
      discountId = '';
      discountLabel = '';
      discountType = '';
      discountValue = 0;
    }

    lines.add(SaleLine(
      product: prod,
      quantity: l.quantity,
      discountId: discountId,
      discountLabel: discountLabel,
      discountType: discountType,
      discountValue: discountValue,
      // No se revalida contra el stock disponible: igual que la web, eso lo
      // hace el servidor recién al cobrar (acá tampoco se reserva nada).
      serialNumbers: l.serialNumbers,
    ));
  }

  var headerId = payload.header.id;
  var headerLabel = payload.header.label;
  var headerType = payload.header.type;
  var headerValue = payload.header.value;
  if (headerType.isNotEmpty && headerValue > 0) {
    final motivo = _invalidDiscount(
      id: headerId,
      type: headerType,
      value: headerValue,
      catalog: discounts,
      settings: settings,
      isCashier: isCashier,
    );
    if (motivo.isNotEmpty) {
      warnings.add('Se quitó el descuento global: $motivo.');
      headerId = '';
      headerLabel = '';
      headerType = '';
      headerValue = 0;
    }
  } else {
    headerId = '';
    headerLabel = '';
    headerType = '';
    headerValue = 0;
  }

  return ResumedSale(
    lines: lines,
    customer: payload.customer?.toCustomer(),
    headerDiscountId: headerId,
    headerDiscountLabel: headerLabel,
    headerDiscountType: headerType,
    headerDiscountValue: headerValue,
    warnings: warnings,
  );
}

/// Vuelca el resultado en el carrito activo, de una sola vez.
void applyResumedSale(CartProvider cart, ResumedSale resumed) {
  cart.replaceAll(
    lines: resumed.lines,
    customer: resumed.customer,
    headerDiscountId: resumed.headerDiscountId,
    headerDiscountLabel: resumed.headerDiscountLabel,
    headerDiscountType: resumed.headerDiscountType,
    headerDiscountValue: resumed.headerDiscountValue,
  );
}
