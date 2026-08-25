import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';
import 'package:inventory_movil/features/home/main_shell.dart';
import 'package:inventory_movil/models/cash_session.dart';
import 'package:inventory_movil/models/product.dart';
import 'package:inventory_movil/models/sale_history.dart';
import 'package:inventory_movil/providers/auth_provider.dart';
import 'package:inventory_movil/providers/cart_provider.dart';
import 'package:inventory_movil/services/access_menu_service.dart';
import 'package:inventory_movil/services/auth_service.dart';
import 'package:inventory_movil/services/catalog_service.dart';
import 'package:inventory_movil/services/product_service.dart';
import 'package:inventory_movil/services/purchase_service.dart';
import 'package:inventory_movil/services/sale_service.dart';

/// Cuántas veces pidió datos cada pestaña. `IndexedStack` construye todos sus
/// hijos de una; el contador es lo que prueba que la construcción perezosa
/// funciona.
int productHits = 0;
int salesHits = 0;

Map<String, dynamic> _sessionJson({required String openedAt}) => {
      'Id': 'c1',
      'UserId': 7,
      'UserFullName': 'Ana Quispe',
      'OpenedAt': openedAt,
      'ClosedAt': null,
      'OpeningAmount': 500.0,
      'TotalSales': 310.66,
      'TotalCashSales': 310.66,
      'TotalExpenses': 0.0,
      'TotalWithdrawals': 0.0,
      'TotalIncome': 0.0,
      'TotalReturns': 33.40,
    };

class _FakeSaleService extends SaleService {
  _FakeSaleService(super.api, {this.session});

  final CashSession? session;

  @override
  Future<CashSession?> activeSession() async => session;

  @override
  Future<SalesPage> getSales({
    required String dateInitial,
    required String dateEnd,
    int page = 1,
    int pageSize = 20,
    String? sellerName,
  }) async {
    salesHits++;
    return SalesPage.fromJson({
      'Items': [],
      'TotalCount': 6,
      'PeriodSubtotal': 310.66,
      'PeriodDiscounts': 0,
      'PeriodTotal': 310.66,
      'PeriodReturned': 33.40,
      'PeriodNet': 277.26,
    });
  }
}

class _FakeProductService extends ProductService {
  _FakeProductService(super.api);

  @override
  Future<({List<Product> items, int totalCount})> getStockPaged({
    String search = '',
    int page = 1,
    int pageSize = 20,
  }) async {
    productHits++;
    return (items: <Product>[], totalCount: 0);
  }
}

class _FakeCatalogService extends CatalogService {
  _FakeCatalogService(super.api);
}

class _FakePurchaseService extends PurchaseService {
  _FakePurchaseService(super.api);
}

Widget _app({CashSession? session}) {
  final storage = AuthStorage();
  final api = ApiClient(storage);
  return MultiProvider(
    providers: [
      Provider<SaleService>(
          create: (_) => _FakeSaleService(api, session: session)),
      Provider<ProductService>(create: (_) => _FakeProductService(api)),
      Provider<CatalogService>(create: (_) => _FakeCatalogService(api)),
      Provider<PurchaseService>(create: (_) => _FakePurchaseService(api)),
      ChangeNotifierProvider(create: (_) => CartProvider()),
      ChangeNotifierProvider(
        create: (_) => AuthProvider(
          AuthService(api),
          storage,
          api,
          AccessMenuService(api),
        )..userName = 'vendedor demo',
      ),
    ],
    child: MaterialApp(
      theme: AppTheme.light(),
      home: const MainShell(),
    ),
  );
}

void main() {
  setUp(() {
    productHits = 0;
    salesHits = 0;
  });

  testWidgets('arranca en Inicio con las cuatro secciones en la barra',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.text('Hola, vendedor'), findsOneWidget);
    expect(find.text('Vender'), findsOneWidget);
    // Etiquetas de la barra inferior.
    expect(find.text('Inicio'), findsOneWidget);
    expect(find.text('Productos'), findsOneWidget);
    expect(find.text('Ventas'), findsOneWidget);
    expect(find.text('Más'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('las pestañas no visitadas no piden datos al arrancar',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    // Solo la home cargó: Productos y Ventas siguen sin construirse.
    expect(productHits, 0);
    expect(salesHits, 1);

    await tester.tap(find.text('Productos'));
    await tester.pumpAndSettle();
    expect(productHits, 1);

    // Volver a Inicio y regresar no vuelve a pedir: la pestaña queda viva.
    await tester.tap(find.text('Inicio'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Productos'));
    await tester.pumpAndSettle();
    expect(productHits, 1);
  });

  testWidgets('la pestaña Más lleva a lo que no entra en la barra',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Más'));
    await tester.pumpAndSettle();

    expect(find.text('Sesiones de caja'), findsOneWidget);
    expect(find.text('Pedidos'), findsOneWidget);
    expect(find.text('Ajustes'), findsOneWidget);
  });

  testWidgets('el botón atrás vuelve a Inicio en vez de cerrar la app',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Más'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(AppBar, 'Más'), findsOneWidget);

    // Gesto/botón atrás del sistema.
    final widgetsBinding = tester.binding;
    await widgetsBinding.handlePopRoute();
    await tester.pumpAndSettle();

    // Volvió a Inicio y la app sigue en pie.
    expect(find.text('Vender'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('sin caja abierta avisa y no muestra un saldo inventado',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.text('Sin caja abierta · tocá para abrirla'), findsOneWidget);
    expect(find.text('Abrí la caja para empezar'), findsOneWidget);
    // Las cifras del día se muestran igual.
    expect(find.text('Bs 277.26'), findsOneWidget);
    expect(find.text('6'), findsOneWidget);
  });

  testWidgets('la caja del día se muestra sin aviso', (tester) async {
    final hoy = DateTime.now().toUtc().subtract(const Duration(hours: 2));
    await tester.pumpWidget(_app(
      session: CashSession.fromJson(
          _sessionJson(openedAt: hoy.toIso8601String())),
    ));
    await tester.pumpAndSettle();

    // 500 + 310.66 − 33.40 = 777.26
    expect(find.textContaining('Caja abierta · Bs 777.26'), findsOneWidget);
    expect(find.textContaining('La caja lleva'), findsNothing);
    expect(find.text('Abrí la caja para empezar'), findsNothing);
  });

  testWidgets('una caja de ayer dispara el aviso', (tester) async {
    final ayer = DateTime.now().subtract(const Duration(days: 1));
    await tester.pumpWidget(_app(
      session: CashSession.fromJson(
          _sessionJson(openedAt: ayer.toUtc().toIso8601String())),
    ));
    await tester.pumpAndSettle();

    expect(find.text('La caja lleva 1 día abierta'), findsOneWidget);
    expect(find.text('Conviene arquear y abrir una nueva'), findsOneWidget);
    expect(find.textContaining('desde ayer'), findsOneWidget);
  });

  testWidgets('con el texto ampliado la home no desborda', (tester) async {
    tester.view.physicalSize = const Size(320, 568);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    final ayer = DateTime.now().subtract(const Duration(days: 2));
    await tester.pumpWidget(_app(
      session: CashSession.fromJson(
          _sessionJson(openedAt: ayer.toUtc().toIso8601String())),
    ));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });
}
