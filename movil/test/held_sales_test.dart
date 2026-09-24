import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/features/pos/held_sales.dart';
import 'package:inventory_movil/models/cash_session.dart';
import 'package:inventory_movil/models/catalog.dart';
import 'package:inventory_movil/models/discount.dart';
import 'package:inventory_movil/models/held_sale.dart';
import 'package:inventory_movil/models/product.dart';
import 'package:inventory_movil/providers/auth_provider.dart';
import 'package:inventory_movil/providers/cart_provider.dart';
import 'package:inventory_movil/services/access_menu_service.dart';
import 'package:inventory_movil/services/auth_service.dart';
import 'package:inventory_movil/services/discount_service.dart';
import 'package:inventory_movil/services/sale_service.dart';
import 'package:provider/provider.dart';

/// Servidor de ventas en espera en memoria.
class FakeSales extends Fake implements SaleService {
  final held = <HeldSale>[];

  @override
  Future<List<HeldSale>> heldSales() async => List.of(held);

  @override
  Future<void> holdSale(
      {required String label, String? customerId, required int itemsCount, required double total, required String payload}) async {
    held.add(HeldSale(
        id: 'h${held.length + 1}', label: label, created: DateTime(2026, 9, 23, 15, 30),
        itemsCount: itemsCount, total: total, payload: payload));
  }

  @override
  Future<HeldSale> takeHeldSale(String id) async {
    final h = held.firstWhere((x) => x.id == id);
    held.remove(h);
    return h;
  }

  @override
  Future<void> discardHeldSale(String id) async => held.removeWhere((x) => x.id == id);

  @override
  Future<PosSettings> posSettings() async => PosSettings(maxCashierDiscountPct: 10, maxCashierDiscountAmount: 20);
}

class FakeDiscounts extends Fake implements DiscountService {
  @override
  Future<List<Discount>> active() async => [];
}

final paracetamol = Product(id: 'p1', productName: 'Paracetamol', salePrice: 5);

Future<(CartProvider, FakeSales)> abrir(WidgetTester t, {String rol = 'Cajero'}) async {
  final cart = CartProvider();
  final sales = FakeSales();
  final api = ApiClient(AuthStorage());
  await t.pumpWidget(MultiProvider(
    providers: [
      Provider<SaleService>.value(value: sales),
      Provider<DiscountService>.value(value: FakeDiscounts()),
      ChangeNotifierProvider.value(value: cart),
      ChangeNotifierProvider(
          create: (_) => AuthProvider(AuthService(api), AuthStorage(), api, AccessMenuService(api))..rolName = rol),
    ],
    child: MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (ctx) => Column(children: [
            TextButton(onPressed: () => holdCurrentSale(ctx), child: const Text('esperar')),
            TextButton(onPressed: () => showHeldSalesSheet(ctx, products: [paracetamol]), child: const Text('lista')),
          ]),
        ),
      ),
    ),
  ));
  return (cart, sales);
}

void main() {
  testWidgets('poner en espera con nota y cliente, y retomarla', (t) async {
    final (cart, sales) = await abrir(t);
    cart
      ..add(paracetamol)
      ..add(paracetamol)
      ..customer = Customer(id: 'c1', fullName: 'Ana', documentNumber: '1');

    await t.tap(find.text('esperar'));
    await t.pumpAndSettle();
    await t.enterText(find.byType(TextField), 'señora de blusa roja');
    await t.tap(find.widgetWithText(FilledButton, 'Poner en espera'));
    await t.pumpAndSettle();

    expect(cart.isEmpty, isTrue);
    expect(cart.customer, isNull);
    final guardada = sales.held.single;
    expect((guardada.label, guardada.itemsCount, guardada.total), ('señora de blusa roja', 2, 10.0));
    expect((jsonDecode(guardada.payload) as Map)['customer']['FullName'], 'Ana');

    await t.tap(find.text('lista'));
    await t.pumpAndSettle();
    expect(find.text('señora de blusa roja'), findsOneWidget);
    expect(find.textContaining('15:30 · 2 producto(s)'), findsOneWidget);

    await t.tap(find.text('señora de blusa roja'));
    await t.pumpAndSettle();
    expect(cart.itemCount, 2);
    expect(cart.customer?.fullName, 'Ana');
    expect(sales.held, isEmpty);
    expect(find.text('Venta retomada con cambios'), findsNothing);   // nada cambió
  });

  testWidgets('retomar avisa lo que cambió y no pisa una venta en curso', (t) async {
    final (cart, sales) = await abrir(t);
    // Guardada en la web con otro precio y un 15 % manual: un cajero no puede.
    await sales.holdSale(label: 'mesa 3', itemsCount: 1, total: 3.4, payload: jsonEncode({
      'v': 1,
      'customer': null,
      'lines': [
        {'ProductId': 'p1', 'ProductName': 'Paracetamol', 'Quantity': 1, 'UnitPrice': 4,
         'DiscountId': '', 'DiscountLabel': 'Manual', 'DiscountType': 'Percentage', 'DiscountValue': 15},
      ],
      'header': {'id': '', 'label': '', 'type': '', 'value': 0},
    }));

    cart.add(paracetamol);
    await t.tap(find.text('lista'));
    await t.pumpAndSettle();
    await t.tap(find.text('mesa 3'));
    await t.pumpAndSettle();
    expect(find.textContaining('Termine o ponga en espera'), findsOneWidget);
    expect(sales.held, hasLength(1));                        // no se sacó de la espera

    cart.clear();
    await t.tap(find.text('mesa 3'));
    await t.pumpAndSettle();
    expect(find.text('Venta retomada con cambios'), findsOneWidget);
    expect(find.textContaining('el precio cambió a 5.00'), findsOneWidget);
    expect(find.textContaining('requiere autorizar de nuevo'), findsOneWidget);
    expect(cart.lines.single.discountValue, 0);
  });

  testWidgets('descartar pide confirmación', (t) async {
    final (_, sales) = await abrir(t);
    await sales.holdSale(label: 'se fue', itemsCount: 1, total: 5, payload: '{"v":1,"lines":[]}');

    await t.tap(find.text('lista'));
    await t.pumpAndSettle();
    await t.tap(find.byTooltip('Descartar'));
    await t.pumpAndSettle();
    await t.tap(find.widgetWithText(FilledButton, 'Descartar'));
    await t.pumpAndSettle();

    expect(sales.held, isEmpty);
    expect(find.text('No hay ventas en espera en esta sucursal.'), findsOneWidget);
  });
}
