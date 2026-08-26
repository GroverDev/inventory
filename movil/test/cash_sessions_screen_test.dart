import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';
import 'package:inventory_movil/features/cash/cash_sessions_screen.dart';
import 'package:inventory_movil/models/cash_session.dart';
import 'package:inventory_movil/models/sale_history.dart';
import 'package:inventory_movil/services/sale_service.dart';

/// Las cuatro formas en que puede quedar un turno: en curso, cuadrado, con
/// sobrante y con faltante. Tal como las manda `CashSessionResponse`.
Map<String, dynamic> _sessionJson({
  required String id,
  required String openedAt,
  String? closedAt,
  required double opening,
  double? declared,
  double? expected,
  double? difference,
  String notes = '',
}) =>
    {
      'Id': id,
      'UserId': 7,
      'UserFullName': 'Ana Quispe',
      'OpenedAt': openedAt,
      'ClosedAt': closedAt,
      'OpeningAmount': opening,
      'DeclaredAmount': declared,
      'ExpectedAmount': expected,
      'Difference': difference,
      'Notes': notes,
      'TotalSales': 310.66,
      'TotalCashSales': 277.26,
      'TotalExpenses': 15.0,
      'TotalWithdrawals': 30.0,
      'TotalIncome': 0.0,
      'TotalReturns': 33.40,
    };

class _FakeSaleService extends SaleService {
  _FakeSaleService(super.api);

  @override
  Future<List<CashSession>> cashSessions({
    required String dateFrom,
    required String dateTo,
  }) async =>
      [
        // En curso: sin ClosedAt ni Declared.
        CashSession.fromJson(_sessionJson(
          id: 'c1',
          openedAt: '2026-08-25T11:58:00Z',
          opening: 50,
        )),
        // Cuadró exacto.
        CashSession.fromJson(_sessionJson(
          id: 'c2',
          openedAt: '2026-08-24T12:00:00Z',
          closedAt: '2026-08-24T23:15:00Z',
          opening: 100,
          declared: 298.86,
          expected: 298.86,
          difference: 0,
        )),
        // Sobrante.
        CashSession.fromJson(_sessionJson(
          id: 'c3',
          openedAt: '2026-08-23T12:00:00Z',
          closedAt: '2026-08-23T23:00:00Z',
          opening: 100,
          declared: 348.60,
          expected: 298.86,
          difference: 49.74,
          notes: 'sobró plata, revisar vueltos',
        )),
        // Faltante.
        CashSession.fromJson(_sessionJson(
          id: 'c4',
          openedAt: '2026-08-22T12:00:00Z',
          closedAt: '2026-08-22T23:00:00Z',
          opening: 100,
          declared: 290.00,
          expected: 298.86,
          difference: -8.86,
        )),
      ];

  /// Las ventas del turno, tal como las manda `GET api/CashSession/{id}/sales`.
  /// Solo c3 tiene ventas: c2 sirve para el caso de la sesión sin ninguna.
  @override
  Future<List<SaleSummary>> sessionSales(String sessionId) async {
    if (sessionId != 'c3') return const [];
    return [
      SaleSummary.fromJson({
        'Id': 'v1',
        'CustomerName': 'Juan Pérez',
        'SellerName': 'Ana Quispe',
        'SaleDate': '2026-08-23T14:30:00Z',
        'Subtotal': 110.0,
        'TotalDiscounts': 0.0,
        'Total': 110.0,
        'IsActive': true,
        'TotalReturned': 0.0,
        'NetTotal': 110.0,
        'SaleStatus': 'activa',
      }),
      SaleSummary.fromJson({
        'Id': 'v2',
        'CustomerName': 'María Luna',
        'SellerName': 'Ana Quispe',
        'SaleDate': '2026-08-23T18:05:00Z',
        'Subtotal': 190.66,
        'TotalDiscounts': 0.0,
        'Total': 190.66,
        'IsActive': true,
        'TotalReturned': 33.40,
        'NetTotal': 157.26,
        'SaleStatus': 'con_devolucion',
      }),
    ];
  }
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
          child: const CashSessionsScreen(),
        ),
      ),
    ),
  );
}

void _useNarrowScreen(WidgetTester tester) {
  tester.view.physicalSize = const Size(320, 568);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);
}

void main() {
  testWidgets('el listado distingue abierta, cuadró, sobrante y faltante',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.text('Abierta'), findsOneWidget);
    expect(find.text('Cuadró'), findsOneWidget);
    expect(find.text('Sobrante Bs 49.74'), findsOneWidget);
    expect(find.text('Faltante Bs 8.86'), findsOneWidget);
    expect(find.text('4 sesión(es)'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('cada tarjeta dice con cuánto abrió y con cuánto cerró',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    // En curso: solo la apertura, sin "cerró con".
    expect(find.text('Abrió con Bs 50.00'), findsOneWidget);
    // Cerrada: apertura y cierre en la misma línea.
    expect(find.text('Abrió con Bs 100.00  ·  cerró con Bs 348.60'),
        findsOneWidget);
  });

  testWidgets('el detalle abre en otra pantalla, con el desglose y la diferencia',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Sobrante Bs 49.74'));
    await tester.pumpAndSettle();

    // Pantalla propia, no un bottom sheet: el historial queda atrás.
    expect(find.text('Sesión del 23/08/2026'), findsOneWidget);
    expect(find.text('Faltante Bs 8.86'), findsNothing);

    expect(find.text('Fondo inicial'), findsOneWidget);
    expect(find.text('Devoluciones'), findsOneWidget);
    expect(find.text('− Bs 33.40'), findsOneWidget);
    expect(find.text('Esperado'), findsOneWidget);
    expect(find.text('Bs 298.86'), findsOneWidget);
    expect(find.text('Declarado'), findsOneWidget);
    expect(find.text('Diferencia'), findsOneWidget);
    expect(find.text('+ Bs 49.74'), findsOneWidget);
    // La observación del cierre se muestra tal cual.
    expect(find.text('sobró plata, revisar vueltos'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('el detalle de una caja abierta no inventa un declarado',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Abierta'));
    await tester.pumpAndSettle();

    expect(find.text('Esperado en caja'), findsOneWidget);
    expect(find.text('Declarado'), findsNothing);
    expect(find.text('Diferencia'), findsNothing);
    expect(find.text('en curso'), findsOneWidget);
  });

  testWidgets('las tarjetas no desbordan con pantalla angosta y texto ampliado',
      (tester) async {
    _useNarrowScreen(tester);

    await tester.pumpWidget(_app(textScale: 2.0));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  test('sin Difference la diferencia se calcula del declarado', () {
    // API que no grabó difference: igual hay que poder mostrar el faltante.
    final s = CashSession.fromJson(_sessionJson(
      id: 'c9',
      openedAt: '2026-08-20T12:00:00Z',
      closedAt: '2026-08-20T23:00:00Z',
      opening: 100,
      declared: 290,
      expected: 298.86,
    ));
    expect(s.expectedCash, 298.86);
    expect(s.cashDifference, -8.86);
  });

  test('una caja abierta calcula el esperado con los movimientos', () {
    // Sin ExpectedAmount grabado: 50 + 277.26 − 15 − 30 + 0 − 33.40 = 248.86
    final s = CashSession.fromJson(_sessionJson(
      id: 'c10',
      openedAt: '2026-08-25T11:58:00Z',
      opening: 50,
    ));
    expect(s.isOpen, isTrue);
    expect(s.expectedCash, 248.86);
    expect(s.cashDifference, isNull);
  });

  testWidgets('el detalle lista las ventas del turno con su neto',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Sobrante Bs 49.74'));
    await tester.pumpAndSettle();

    // El arqueo ocupa la primera pantalla: las ventas están más abajo.
    await tester.scrollUntilVisible(find.text('María Luna'), 200,
        scrollable: find.byType(Scrollable).first);
    await tester.pumpAndSettle();

    expect(find.text('Ventas del turno'), findsOneWidget);
    expect(find.text('2 venta(s) · Bs 267.26'), findsOneWidget);
    expect(find.text('Juan Pérez'), findsOneWidget);
    // La devuelta muestra el neto, no lo facturado.
    expect(find.text('Bs 157.26'), findsOneWidget);
    expect(find.text('Devolución parcial'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('una sesión sin ventas lo dice, no queda cargando',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Cuadró'));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(
        find.text('No se registraron ventas en esta sesión.'), 200,
        scrollable: find.byType(Scrollable).first);

    expect(find.byType(CircularProgressIndicator), findsNothing);
    expect(find.text('2 venta(s) · Bs 267.26'), findsNothing);
  });
}
