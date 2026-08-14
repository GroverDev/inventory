import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';
import 'package:inventory_movil/features/orders/order_form_screen.dart';
import 'package:inventory_movil/models/catalog.dart';
import 'package:inventory_movil/models/product.dart';
import 'package:inventory_movil/services/catalog_service.dart';
import 'package:inventory_movil/services/product_service.dart';
import 'package:inventory_movil/services/purchase_service.dart';

/// Catálogos en memoria: la pantalla no debe tocar la red en la prueba.
class _FakeCatalogService extends CatalogService {
  _FakeCatalogService(super.api);

  @override
  Future<List<NamedItem>> providers() async => [
        NamedItem(id: 'p1', name: 'Droguería Inti S.A.'),
      ];

  @override
  Future<List<PurchaseStatus>> purchaseStatuses() async => [
        PurchaseStatus(id: 1, name: 'Solicitado'),
        PurchaseStatus(id: 2, name: 'Parcialmente Recibido'),
        PurchaseStatus(id: 3, name: 'Totalmente Recibido'),
      ];
}

class _FakeProductService extends ProductService {
  _FakeProductService(super.api);

  @override
  Future<({List<Product> items, int totalCount})> getStockPaged({
    String search = '',
    int page = 1,
    int pageSize = 20,
  }) async =>
      (
        items: [
          Product(
            id: 'x1',
            // Nombre largo a propósito: es lo que estira la fila.
            productName: 'Paracetamol 500 mg caja x 100 comprimidos',
            salePrice: 12345.67,
            currentStock: 42,
          ),
        ],
        totalCount: 1,
      );
}

class _FakePurchaseService extends PurchaseService {
  _FakePurchaseService(super.api);
}

Widget _app({double textScale = 1.0}) {
  final api = ApiClient(AuthStorage());
  return MultiProvider(
    providers: [
      Provider<CatalogService>(create: (_) => _FakeCatalogService(api)),
      Provider<ProductService>(create: (_) => _FakeProductService(api)),
      Provider<PurchaseService>(create: (_) => _FakePurchaseService(api)),
    ],
    child: MaterialApp(
      theme: AppTheme.light(),
      home: Builder(
        builder: (context) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: TextScaler.linear(textScale)),
          child: const OrderFormScreen(),
        ),
      ),
    ),
  );
}

/// 320x568 aprox. un iPhone SE / gama baja: el caso más apretado.
void _useNarrowScreen(WidgetTester tester) {
  tester.view.physicalSize = const Size(320, 568);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);
}

void main() {
  testWidgets('nuevo pedido no desborda en una pantalla angosta',
      (tester) async {
    _useNarrowScreen(tester);

    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.text('Guardar pedido'), findsOneWidget);
    expect(find.text('Solicitado'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('la barra inferior crece con el texto ampliado', (tester) async {
    // Accesibilidad al máximo que ofrece Android: el caso que más estira la
    // barra, donde una altura fija se rompe.
    _useNarrowScreen(tester);

    await tester.pumpWidget(_app(textScale: 2.0));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  testWidgets('una línea de producto no desborda a lo ancho', (tester) async {
    _useNarrowScreen(tester);

    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Producto'));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('Paracetamol').first);
    await tester.pumpAndSettle();

    expect(find.text('Cantidad'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('una línea de producto no desborda con el texto ampliado',
      (tester) async {
    _useNarrowScreen(tester);

    await tester.pumpWidget(_app(textScale: 2.0));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Producto'));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('Paracetamol').first);
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });
}
