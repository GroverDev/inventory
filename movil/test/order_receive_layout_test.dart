import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';
import 'package:inventory_movil/features/orders/order_receive_screen.dart';
import 'package:inventory_movil/models/purchase.dart';
import 'package:inventory_movil/services/purchase_service.dart';

/// Pedido en memoria: la pantalla no debe tocar la red en la prueba.
class _FakePurchaseService extends PurchaseService {
  _FakePurchaseService(super.api,
      {required this.statusId, this.trackingMode = 'none'});

  final int statusId;

  /// Seguimiento de la primera línea: con 'lot' la tarjeta suma los campos de
  /// lote y vencimiento, que es lo que puede desbordar en un teléfono angosto.
  final String trackingMode;
  PurchaseDelivery? received;

  @override
  Future<PurchaseOrder> getById(String id) async => PurchaseOrder(
        id: id,
        providerName: 'Droguería Inti S.A.',
        purchaseDate: '2026-08-01',
        statusId: statusId,
        statusName: purchaseStatusLabel(statusId),
        total: 1234.5,
        detail: [
          PurchaseOrderLine(
            productId: 'x1',
            // Nombre largo a propósito: es lo que estira la tarjeta.
            productName: 'Paracetamol 500 mg caja x 100 comprimidos',
            orderedQuantity: 10,
            receivedQuantity: 4,
            pendingQuantity: 6,
            orderUnitPrice: 12345.67,
            trackingMode: trackingMode,
          ),
          PurchaseOrderLine(
            productId: 'x2',
            productName: 'Ibuprofeno 400 mg',
            orderedQuantity: 5,
            receivedQuantity: 5,
            pendingQuantity: 0,
            orderUnitPrice: 8.25,
          ),
        ],
      );

  @override
  Future<PurchaseReceiptResult> receive(PurchaseDelivery delivery) async {
    received = delivery;
    return PurchaseReceiptResult(PurchaseReceiptOutcome.applied, '');
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
          child: const OrderReceiveScreen(purchaseId: 'ord-1'),
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
  testWidgets('la recepción no desborda en una pantalla angosta',
      (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived);

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.text('Confirmar recepción'), findsOneWidget);
    // Los tres saldos de la línea con pendiente.
    expect(find.text('Ordenado: 10'), findsOneWidget);
    expect(find.text('Recibido: 4'), findsOneWidget);
    expect(find.text('Pendiente: 6'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('no desborda con el texto ampliado', (tester) async {
    // Accesibilidad al máximo que ofrece Android: donde una altura fija se
    // rompe.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.requested);

    await tester.pumpWidget(_app(svc, textScale: 2.0));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  testWidgets('no se puede recibir más de lo pendiente', (tester) async {
    // El servidor lo rechaza igual; recortarlo acá evita el viaje y le dice al
    // usuario cuánto queda en realidad.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived);

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField).first, '99');
    await tester.pumpAndSettle();

    expect(find.widgetWithText(TextField, '6'), findsOneWidget);
    expect(find.textContaining('solo quedan 6'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('cerrar con faltante se ofrece en parcialmente recibido',
      (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived);

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.byType(PopupMenuButton<String>), findsOneWidget);
  });

  testWidgets('cerrar con faltante no se ofrece en solicitado', (tester) async {
    // EnsureCanClose lo exige: sin recepciones corresponde cancelar, no cerrar.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.requested);

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.byType(PopupMenuButton<String>), findsNothing);
  });

  testWidgets('los campos de lote no desbordan en pantalla angosta',
      (tester) async {
    // La tarjeta con lote es la más cargada: nombre largo, chip, saldos, dos
    // campos numéricos, el lote y la fila del vencimiento.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived, trackingMode: 'lot');

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.text('Lote'), findsOneWidget);
    expect(find.widgetWithText(TextField, 'Lote recibido *'), findsOneWidget);
    expect(find.text('Vencimiento (opcional)'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('los campos de lote no desbordan con el texto ampliado',
      (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived, trackingMode: 'lot');

    await tester.pumpWidget(_app(svc, textScale: 2.0));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
  });

  testWidgets('un producto sin lotes no pide lote', (tester) async {
    // La mayoría de los productos no lleva seguimiento: sumarles los campos
    // sería ruido en cada tarjeta.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived);

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.text('Lote'), findsNothing);
    expect(find.widgetWithText(TextField, 'Lote recibido *'), findsNothing);
  });

  testWidgets('no se envía la recepción si falta el lote', (tester) async {
    // El servidor rechazaría la entrega completa: cortar acá evita que el
    // usuario tenga que volver a cargar todas las cantidades.
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived, trackingMode: 'lot');

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Confirmar recepción'));
    await tester.pumpAndSettle();

    expect(find.textContaining('se maneja por lotes'), findsOneWidget);
    expect(svc.received, isNull);
  });

  testWidgets('con el lote cargado la recepción viaja con su código',
      (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.partiallyReceived, trackingMode: 'lot');

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    await tester.enterText(
        find.widgetWithText(TextField, 'Lote recibido *'), 'IBU-2609-A');
    await tester.pumpAndSettle();

    await tester.tap(find.text('Confirmar recepción'));
    await tester.pumpAndSettle();
    // El diálogo de confirmación se acepta antes de que la entrega salga.
    await tester.tap(find.text('Confirmar').last);
    await tester.pumpAndSettle();

    final detail = svc.received!.toJson()['Detail'] as List;
    expect((detail.first as Map)['LotCode'], 'IBU-2609-A');
  });

  testWidgets('un pedido ya recibido lo dice y no ofrece confirmar',
      (tester) async {
    _useNarrowScreen(tester);
    final svc = _FakePurchaseService(ApiClient(AuthStorage()),
        statusId: PurchaseStatusIds.totallyReceived);

    await tester.pumpWidget(_app(svc));
    await tester.pumpAndSettle();

    expect(find.textContaining('ya no admite recepciones'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
