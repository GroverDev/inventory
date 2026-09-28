import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/features/pos/held_sale_resume.dart';
import 'package:inventory_movil/models/cash_session.dart';
import 'package:inventory_movil/models/catalog.dart';
import 'package:inventory_movil/models/discount.dart';
import 'package:inventory_movil/models/held_sale.dart';
import 'package:inventory_movil/models/product.dart';
import 'package:inventory_movil/models/sale.dart';
import 'package:inventory_movil/providers/cart_provider.dart';

/// Espejo de `resumeHeldSale` en PointOfSaleView.vue: nada de lo guardado se
/// da por bueno — precio, existencia del producto y validez del descuento se
/// revisan de nuevo contra el estado actual.
void main() {
  final settings = PosSettings(
    maxCashierDiscountPct: 15,
    maxCashierDiscountAmount: 50,
    maxDiscountPct: 30,
    maxDiscountAmount: 200,
  );

  final discountCatalog = [
    Discount(
        id: 'd1',
        name: '10% clientes frecuentes',
        description: '',
        type: 'Percentage',
        value: 10,
        isActive: true),
  ];

  test('el precio vigente reemplaza al guardado y avisa del cambio', () {
    final producto = Product(id: 'p1', productName: 'Amoxicilina', salePrice: 20);
    final payload = HeldPayload(
      lines: [
        HeldLine(
            productId: 'p1',
            productName: 'Amoxicilina',
            unitPrice: 15, // el precio con el que se dejó en espera
            quantity: 2),
      ],
      header: const HeldHeaderDiscount(),
    );

    final resumed = resolveHeldSale(payload,
        catalog: [producto],
        discounts: discountCatalog,
        settings: settings,
        isCashier: false);

    expect(resumed.lines, hasLength(1));
    expect(resumed.lines.single.unitPrice, 20);
    expect(resumed.warnings.single, contains('el precio cambió a Bs 20.00'));
  });

  test('sin cambio de precio, no hay aviso', () {
    final producto = Product(id: 'p1', productName: 'Amoxicilina', salePrice: 20);
    final payload = HeldPayload(
      lines: [
        HeldLine(
            productId: 'p1', productName: 'Amoxicilina', unitPrice: 20, quantity: 1),
      ],
      header: const HeldHeaderDiscount(),
    );

    final resumed = resolveHeldSale(payload,
        catalog: [producto],
        discounts: discountCatalog,
        settings: settings,
        isCashier: false);

    expect(resumed.warnings, isEmpty);
  });

  test('un producto que ya no está en el catálogo se quita con aviso', () {
    final payload = HeldPayload(
      lines: [
        HeldLine(
            productId: 'p-borrado',
            productName: 'Producto discontinuado',
            unitPrice: 5,
            quantity: 1),
      ],
      header: const HeldHeaderDiscount(),
    );

    final resumed = resolveHeldSale(payload,
        catalog: const [],
        discounts: discountCatalog,
        settings: settings,
        isCashier: false);

    expect(resumed.lines, isEmpty);
    expect(resumed.warnings.single, contains('Producto discontinuado» ya no está disponible'));
  });

  test('un descuento de catálogo dado de baja se quita con aviso', () {
    final producto = Product(id: 'p1', productName: 'Ibuprofeno', salePrice: 10);
    final payload = HeldPayload(
      lines: [
        HeldLine(
          productId: 'p1',
          productName: 'Ibuprofeno',
          unitPrice: 10,
          quantity: 1,
          discountId: 'd-inactivo', // ya no está en discountCatalog
          discountType: 'Percentage',
          discountValue: 10,
        ),
      ],
      header: const HeldHeaderDiscount(),
    );

    final resumed = resolveHeldSale(payload,
        catalog: [producto],
        discounts: discountCatalog,
        settings: settings,
        isCashier: false);

    expect(resumed.lines.single.discountId, isEmpty);
    expect(resumed.lines.single.discountValue, 0);
    expect(resumed.warnings.single, contains('ya no está en el catálogo'));
  });

  test('un descuento manual que supera el tope máximo de la sucursal se quita, '
      'sin importar el rol', () {
    final producto = Product(id: 'p1', productName: 'Vitamina C', salePrice: 10);
    final payload = HeldPayload(
      lines: [
        HeldLine(
          productId: 'p1',
          productName: 'Vitamina C',
          unitPrice: 10,
          quantity: 1,
          discountType: 'Percentage',
          discountValue: 40, // > maxDiscountPct (30)
        ),
      ],
      header: const HeldHeaderDiscount(),
    );

    final resumed = resolveHeldSale(payload,
        catalog: [producto],
        discounts: discountCatalog,
        settings: settings,
        isCashier: false); // ni un supervisor lo autoriza

    expect(resumed.lines.single.discountType, isEmpty);
    expect(resumed.warnings.single, contains('supera el tope máximo'));
  });

  test('un descuento manual bajo el tope general pero sobre el de cajero '
      'vuelve a pedir autorización si quien retoma es cajero', () {
    final producto = Product(id: 'p1', productName: 'Loratadina', salePrice: 10);
    final payload = HeldPayload(
      lines: [
        HeldLine(
          productId: 'p1',
          productName: 'Loratadina',
          unitPrice: 10,
          quantity: 1,
          discountType: 'Percentage',
          discountValue: 20, // > maxCashierDiscountPct (15), < maxDiscountPct (30)
        ),
      ],
      header: const HeldHeaderDiscount(),
    );

    final paraCajero = resolveHeldSale(payload,
        catalog: [producto],
        discounts: discountCatalog,
        settings: settings,
        isCashier: true);
    expect(paraCajero.lines.single.discountType, isEmpty);
    expect(paraCajero.warnings.single, contains('requiere autorizar de nuevo'));

    final paraSupervisor = resolveHeldSale(payload,
        catalog: [producto],
        discounts: discountCatalog,
        settings: settings,
        isCashier: false);
    expect(paraSupervisor.lines.single.discountType, 'Percentage');
    expect(paraSupervisor.warnings, isEmpty);
  });

  test('el descuento global sigue las mismas reglas que el de línea', () {
    final producto = Product(id: 'p1', productName: 'Aspirina', salePrice: 10);
    final payload = HeldPayload(
      lines: [
        HeldLine(productId: 'p1', productName: 'Aspirina', unitPrice: 10, quantity: 1),
      ],
      header: const HeldHeaderDiscount(
          id: '', label: 'Manual 50%', type: 'Percentage', value: 50),
    );

    final resumed = resolveHeldSale(payload,
        catalog: [producto],
        discounts: discountCatalog,
        settings: settings,
        isCashier: false);

    expect(resumed.headerDiscountType, isEmpty);
    expect(resumed.warnings.any((w) => w.contains('descuento global')), isTrue);
  });

  test('applyResumedSale vuelca todo en el carrito de una sola vez', () {
    final producto = Product(id: 'p1', productName: 'Dextrometorfano', salePrice: 12);
    final cliente = Customer(id: 'c1', fullName: 'Ana', documentNumber: '1');
    final resumed = ResumedSale(
      lines: [SaleLine(product: producto, quantity: 3)],
      customer: cliente,
      headerDiscountId: '',
      headerDiscountLabel: '',
      headerDiscountType: '',
      headerDiscountValue: 0,
      warnings: const [],
    );
    final cart = CartProvider();

    applyResumedSale(cart, resumed);

    expect(cart.lines, hasLength(1));
    expect(cart.lines.single.quantity, 3);
    expect(cart.customer?.id, 'c1');
  });

  test('buildHeldPayload arma el payload a partir del carrito en curso', () {
    final producto = Product(id: 'p1', productName: 'Cetirizina', salePrice: 7);
    final cart = CartProvider()
      ..add(producto)
      ..setCustomer(Customer(id: 'c2', fullName: 'Luis', documentNumber: '2'));

    final payload = buildHeldPayload(cart);

    expect(payload.customer!.id, 'c2');
    expect(payload.lines.single.productId, 'p1');
    expect(payload.lines.single.quantity, 1);
  });
}
