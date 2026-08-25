import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';
import 'package:inventory_movil/features/sales/sales_screen.dart';
import 'package:inventory_movil/models/sale_history.dart';
import 'package:inventory_movil/services/sale_service.dart';

/// Las tres ventas que el listado tiene que saber distinguir, tal como las
/// manda la API: `SaleStatus` sale de `v_sales_net`, y una devolución parcial
/// deja `IsActive` en true — que es justo lo que antes la hacía pasar por
/// "Activa" y ocultaba la devolución.
Map<String, dynamic> _saleJson({
  required String id,
  required String customer,
  required double total,
  required double returned,
  required String status,
  required bool isActive,
}) =>
    {
      'Id': id,
      'CustomerName': customer,
      'SellerName': 'Ana Quispe',
      'SaleDate': '2026-08-24T18:30:00Z',
      'Subtotal': total,
      'TotalDiscounts': 0,
      'Total': total,
      'IsActive': isActive,
      'TotalReturned': returned,
      'NetTotal': total - returned,
      'SaleStatus': status,
    };

class _FakeSaleService extends SaleService {
  _FakeSaleService(super.api);

  @override
  Future<SalesPage> getSales({
    required String dateInitial,
    required String dateEnd,
    int page = 1,
    int pageSize = 20,
    String? sellerName,
  }) async =>
      SalesPage.fromJson({
        'Items': [
          _saleJson(
            id: 's1',
            customer: 'Farmacia San Miguel',
            total: 300,
            returned: 0,
            status: 'activa',
            isActive: true,
          ),
          _saleJson(
            id: 's2',
            customer: 'Distribuidora Los Andes',
            total: 250,
            returned: 80,
            status: 'con_devolucion',
            isActive: true,
          ),
          _saleJson(
            id: 's3',
            customer: 'Juan Pérez',
            total: 120,
            returned: 120,
            status: 'anulada',
            isActive: false,
          ),
        ],
        'TotalCount': 3,
        'PeriodSubtotal': 670,
        'PeriodDiscounts': 0,
        'PeriodTotal': 670,
        'PeriodReturned': 200,
        'PeriodNet': 470,
      });
}

Widget _app({double textScale = 1.0}) {
  final api = ApiClient(AuthStorage());
  return MultiProvider(
    providers: [
      Provider<SaleService>(create: (_) => _FakeSaleService(api)),
    ],
    child: MaterialApp(
      theme: AppTheme.light(),
      home: Builder(
        builder: (context) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: TextScaler.linear(textScale)),
          child: const SalesScreen(),
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
  testWidgets('el listado distingue activa, devolución parcial y total',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.text('Activa'), findsOneWidget);
    expect(find.text('Devolución parcial'), findsOneWidget);
    expect(find.text('Devuelta total'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('cada venta muestra el neto, y el facturado solo si hubo devolución',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    // Sin devoluciones el importe es el mismo y aparece una sola vez.
    expect(find.text('Bs 300.00'), findsOneWidget);

    // Con devolución parcial: neto 170 y facturado 250 tachado.
    expect(find.text('Bs 170.00'), findsOneWidget);
    expect(find.text('Bs 250.00'), findsOneWidget);

    // Devuelta entera: neto 0 y facturado 120 tachado.
    expect(find.text('Bs 0.00'), findsOneWidget);
    expect(find.text('Bs 120.00'), findsOneWidget);
  });

  testWidgets('el total del período va neto y aclara lo devuelto',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.text('3 venta(s) · Bs 470.00'), findsOneWidget);
    expect(find.text('Facturado Bs 670.00 · devuelto − Bs 200.00'),
        findsOneWidget);
  });

  testWidgets('la tarjeta no desborda en una pantalla angosta',
      (tester) async {
    _useNarrowScreen(tester);

    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.text('Devolución parcial'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('la tarjeta no desborda con el texto ampliado', (tester) async {
    // Accesibilidad al máximo de Android: la columna de la derecha pasa a tres
    // líneas y es donde una altura fija se rompería.
    _useNarrowScreen(tester);

    await tester.pumpWidget(_app(textScale: 2.0));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  test('sin sale_status el estado se deduce de lo que sí llegó', () {
    // API vieja: no manda SaleStatus ni NetTotal.
    final parcial = SaleSummary.fromJson({
      'Id': 's9',
      'CustomerName': 'Cliente',
      'Total': 200,
      'TotalReturned': 50,
      'IsActive': true,
    });
    expect(parcial.status, SaleStatus.conDevolucion);
    expect(parcial.netTotal, 150);

    final anulada = SaleSummary.fromJson({
      'Id': 's10',
      'CustomerName': 'Cliente',
      'Total': 200,
      'TotalReturned': 200,
      'IsActive': false,
    });
    expect(anulada.status, SaleStatus.devueltaTotal);
    expect(anulada.netTotal, 0);
  });

  test('el detalle marca devuelta total cuando no quedan unidades pendientes',
      () {
    // Dos parciales que entre ambas cubren la venta. `sale_status` quedó en
    // `con_devolucion` (dato viejo, is_full_return no miraba lo acumulado):
    // el detalle igual tiene que decir "Devuelta total".
    final sale = SaleFull.fromJson({
      'Id': 'v1',
      'CustomerName': 'Cliente',
      'Total': 200,
      'IsActive': true,
      'SaleStatus': 'con_devolucion',
      'TotalReturned': 200,
      'Detail': [
        {'Id': 'd1', 'ProductId': 'p1', 'Quantity': 2, 'UnitPrice': 100},
      ],
      'Returns': [
        {
          'Id': 'r1',
          'TotalReturned': 100,
          'IsFullReturn': false,
          'Detail': [
            {'SaleDetailId': 'd1', 'ProductId': 'p1', 'QuantityReturned': 1},
          ],
        },
        {
          'Id': 'r2',
          'TotalReturned': 100,
          'IsFullReturn': false,
          'Detail': [
            {'SaleDetailId': 'd1', 'ProductId': 'p1', 'QuantityReturned': 1},
          ],
        },
      ],
    });

    expect(sale.status, SaleStatus.conDevolucion);
    expect(sale.effectiveStatus, SaleStatus.devueltaTotal);
    expect(sale.totalReturned, 200);
    expect(sale.netTotal, 0);
  });
}
