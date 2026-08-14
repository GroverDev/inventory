import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';
import 'package:inventory_movil/features/orders/order_filters_sheet.dart';
import 'package:inventory_movil/features/orders/orders_screen.dart';
import 'package:inventory_movil/models/purchase.dart';
import 'package:inventory_movil/services/purchase_service.dart';

/// Anota con qué filtros se consultó, sin salir a la red.
class _FakePurchaseService extends PurchaseService {
  _FakePurchaseService(super.api);

  final List<({DateTime from, DateTime to, int statusId})> calls = [];

  @override
  Future<List<PurchaseSummary>> list({
    required DateTime from,
    required DateTime to,
    required int statusId,
  }) async {
    calls.add((from: from, to: to, statusId: statusId));
    return [];
  }
}

Widget _app(_FakePurchaseService svc, {double textScale = 1.0}) {
  return Provider<PurchaseService>.value(
    value: svc,
    child: MaterialApp(
      theme: AppTheme.light(),
      home: Builder(
        builder: (context) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: TextScaler.linear(textScale)),
          child: const OrdersScreen(),
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
  test('el resumen del filtro nombra estado y periodo', () {
    // Es lo que impide leer un filtro activo como ausencia de datos.
    final filters = PurchaseFilters(
      statusId: PurchaseStatusIds.partiallyReceived,
      from: DateTime(2026, 8, 1),
      to: DateTime(2026, 8, 13),
    );

    expect(filters.label, 'Parc. recibido · 01/08 – 13/08');
  });

  test('un rango a caballo de dos años muestra el año', () {
    final filters = PurchaseFilters(
      statusId: PurchaseStatusIds.requested,
      from: DateTime(2025, 12, 20),
      to: DateTime(2026, 1, 10),
    );

    expect(filters.rangeLabel, '20/12/25 – 10/01/26');
  });

  test('el filtro inicial es Solicitado del primero del mes a hoy', () {
    // El mismo default con el que abre la webapp.
    final filters = PurchaseFilters.initial();
    final now = DateTime.now();

    expect(filters.statusId, PurchaseStatusIds.requested);
    expect(filters.from, DateTime(now.year, now.month, 1));
  });

  testWidgets('la lista muestra el filtro vigente en la barra', (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()));

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.textContaining('Solicitado ·'), findsOneWidget);
    expect(svc.calls.single.statusId, PurchaseStatusIds.requested);
    expect(tester.takeException(), isNull);
  });

  testWidgets('la hoja muestra los cinco estados completos', (tester) async {
    // El motivo de existir de la hoja: como fila de chips horizontales, dos de
    // los cinco quedaban fuera de pantalla sin ninguna pista.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()));

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.tune).first);
    await tester.pumpAndSettle();

    for (final id in PurchaseStatusIds.all) {
      expect(find.text(purchaseStatusLabel(id)), findsWidgets,
          reason: 'falta el estado ${purchaseStatusLabel(id)}');
    }
    expect(tester.takeException(), isNull);
  });

  testWidgets('aplicar un estado recarga la lista con ese filtro',
      (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()));

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.tune).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text(
        purchaseStatusLabel(PurchaseStatusIds.partiallyReceived)));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Aplicar'));
    await tester.pumpAndSettle();

    expect(svc.calls, hasLength(2));
    expect(svc.calls.last.statusId, PurchaseStatusIds.partiallyReceived);
    expect(find.textContaining('Parc. recibido ·'), findsOneWidget);
  });

  testWidgets('descartar la hoja no cambia nada', (tester) async {
    // El borrador solo se aplica con el botón: elegir estado y fechas cuesta
    // una consulta, no tres.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()));

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.tune).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text(
        purchaseStatusLabel(PurchaseStatusIds.cancelled)));
    await tester.pumpAndSettle();
    // Descarta tocando fuera de la hoja.
    await tester.tapAt(const Offset(10, 10));
    await tester.pumpAndSettle();

    expect(svc.calls, hasLength(1));
    expect(find.textContaining('Solicitado ·'), findsOneWidget);
  });

  testWidgets('un preset mueve el rango sin tocar el estado', (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()));

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.tune).first);
    await tester.pumpAndSettle();
    // Los presets viven en la zona que scrollea: hay que llegar hasta ellos.
    await tester.ensureVisible(find.text('Este año'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Este año'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Aplicar'));
    await tester.pumpAndSettle();

    final now = DateTime.now();
    expect(svc.calls.last.from, DateTime(now.year, 1, 1));
    expect(svc.calls.last.statusId, PurchaseStatusIds.requested);
  });

  testWidgets('la hoja no desborda con el texto ampliado', (tester) async {
    // Accesibilidad al máximo que ofrece Android, en la pantalla más angosta.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()));

    await tester.pumpWidget(_app(svc, textScale: 2.0));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.tune).first);
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  testWidgets('el estado vacío nombra el filtro y ofrece cambiarlo',
      (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()));

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.textContaining('Sin pedidos en "Solicitado"'), findsOneWidget);
    expect(find.text('Cambiar filtros'), findsOneWidget);
  });
}
