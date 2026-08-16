import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/utils/uid.dart';
import 'package:inventory_movil/models/purchase.dart';

PurchaseOrderLine _line({
  String productId = 'x1',
  String name = 'Paracetamol 500 mg',
  int ordered = 10,
  int received = 0,
  double price = 7.5,
}) =>
    PurchaseOrderLine(
      productId: productId,
      productName: name,
      orderedQuantity: ordered,
      receivedQuantity: received,
      pendingQuantity: ordered - received,
      orderUnitPrice: price,
    );

void main() {
  test('la fecha de recepción viaja en ISO yyyy-MM-dd', () {
    // Misma trampa que en la creación del pedido: el backend parsea
    // DeliveryDate con CultureInfo.InvariantCulture, así que dd/MM/yyyy falla
    // el día 18 y el día 5 lo lee como 8 de mayo.
    final delivery = PurchaseDelivery(
      purchaseId: 'ord-1',
      deliveryDate: DateTime(2026, 8, 18),
      detail: [PurchaseDeliveryLine(_line())],
      operationUid: newUid(),
    );

    expect(delivery.toJson()['DeliveryDate'], '2026-08-18');
  });

  test('solo viajan las líneas con mercadería recibida', () {
    // El backend exige al menos una línea con cantidad y descarta el resto;
    // mandar los ceros solo agranda el payload.
    final recibida = PurchaseDeliveryLine(_line(productId: 'x1'));
    final vacia = PurchaseDeliveryLine(_line(productId: 'x2'))
      ..deliveryQuantity = 0;

    final json = PurchaseDelivery(
      purchaseId: 'ord-1',
      deliveryDate: DateTime(2026, 8, 13),
      detail: [recibida, vacia],
      operationUid: newUid(),
    ).toJson();

    final detail = json['Detail'] as List;
    expect(detail, hasLength(1));
    expect(detail.single, {
      'ProductId': 'x1',
      'DeliveryQuantity': 10,
      'UnitPrice': 7.5,
    });
  });

  test('la recepción propone el saldo pendiente al precio pactado', () {
    // Lo más común es recibir todo lo que falta: se propone eso y el usuario
    // solo corrige la diferencia.
    final line = PurchaseDeliveryLine(_line(ordered: 10, received: 4));

    expect(line.deliveryQuantity, 6);
    expect(line.unitPrice, 7.5);
  });

  test('es parcial cuando alguna línea deja saldo', () {
    final completa = PurchaseDeliveryLine(_line(productId: 'x1'));
    final aMedias = PurchaseDeliveryLine(_line(productId: 'x2'))
      ..deliveryQuantity = 3;

    PurchaseDelivery build(List<PurchaseDeliveryLine> lines) => PurchaseDelivery(
          purchaseId: 'ord-1',
          deliveryDate: DateTime(2026, 8, 13),
          detail: lines,
          operationUid: newUid(),
        );

    expect(build([completa]).isPartial, isFalse);
    expect(build([completa, aMedias]).isPartial, isTrue);
  });

  test('el OperationUid tiene forma de GUID', () {
    // Si no parsea con Guid.TryParse el backend genera uno propio y la
    // recepción deja de ser idempotente sin avisar: un reintento duplicaría
    // el ingreso de stock.
    final uid = newUid();

    expect(
      uid,
      matches(
          r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$'),
    );
    expect(newUid(), isNot(uid));
  });

  group('lotes', () {
    PurchaseOrderLine conLote({int ordered = 10}) => PurchaseOrderLine(
          productId: 'x1',
          productName: 'Ibuprofeno 200 mg',
          orderedQuantity: ordered,
          receivedQuantity: 0,
          pendingQuantity: ordered,
          orderUnitPrice: 3,
          trackingMode: 'lot',
        );

    PurchaseDelivery build(List<PurchaseDeliveryLine> lines) =>
        PurchaseDelivery(
          purchaseId: 'ord-1',
          deliveryDate: DateTime(2026, 8, 15),
          detail: lines,
          operationUid: newUid(),
        );

    test('el lote y el vencimiento viajan en la línea con seguimiento', () {
      final line = PurchaseDeliveryLine(conLote())
        ..lotCode = 'IBU-2609-A'
        ..expiryDate = DateTime(2026, 9, 10);

      expect(build([line]).toJson()['Detail'], [
        {
          'ProductId': 'x1',
          'DeliveryQuantity': 10,
          'UnitPrice': 3.0,
          'LotCode': 'IBU-2609-A',
          // ISO, como la fecha de recepción: el backend lo parsea con la
          // cultura del servidor y dd/MM/yyyy se leería al revés.
          'ExpiryDate': '2026-09-10',
        }
      ]);
    });

    test('un producto sin lotes no ensucia el payload', () {
      // El backend ignora los campos vacíos, pero mandarlos en cada línea de
      // un pedido largo solo agranda el envío desde un teléfono.
      final json = build([PurchaseDeliveryLine(_line())]).toJson();

      expect((json['Detail'] as List).single, isNot(contains('LotCode')));
    });

    test('el vencimiento se omite cuando no se conoce', () {
      // Hay mercadería sin fecha impresa: es opcional incluso con lotes.
      final line = PurchaseDeliveryLine(conLote())..lotCode = 'SIN-FECHA';
      final detail = (build([line]).toJson()['Detail'] as List).single
          as Map<String, dynamic>;

      expect(detail['LotCode'], 'SIN-FECHA');
      expect(detail, isNot(contains('ExpiryDate')));
    });

    test('se detecta la línea que va a recibirse sin declarar su lote', () {
      // El servidor rechaza la transacción ENTERA, así que hay que cortar
      // antes de enviar o el usuario pierde todo lo que cargó.
      final sinLote = PurchaseDeliveryLine(conLote());
      final conCodigo = PurchaseDeliveryLine(conLote())..lotCode = 'OK-1';

      expect(build([sinLote]).linesMissingLot, hasLength(1));
      expect(build([conCodigo]).linesMissingLot, isEmpty);
      // Los espacios no son un lote.
      expect(build([PurchaseDeliveryLine(conLote())..lotCode = '   '])
          .linesMissingLot, hasLength(1));
    });

    test('una línea que no se recibe no exige lote', () {
      // Si no se recibe nada de ese producto, el backend descarta la línea:
      // pedirle el lote sería trabar el envío por algo que no viaja.
      final line = PurchaseDeliveryLine(conLote())..deliveryQuantity = 0;

      expect(build([line]).linesMissingLot, isEmpty);
    });

    test('el seguimiento se lee del detalle del pedido', () {
      final line = PurchaseOrderLine.fromJson({
        'ProductId': 'x1',
        'ProductName': 'Ibuprofeno',
        'OrderedQuantity': 10,
        'TrackingMode': 'lot',
      });

      expect(line.usesLot, isTrue);
    });

    test('sin el campo TrackingMode se asume que no hay seguimiento', () {
      // Compatibilidad: contra una API vieja la pantalla se comporta como
      // siempre en lugar de pedir un lote que nadie va a poder cargar.
      final line = PurchaseOrderLine.fromJson({
        'ProductId': 'x1',
        'ProductName': 'Ibuprofeno',
        'OrderedQuantity': 10,
      });

      expect(line.usesLot, isFalse);
    });
  });

  group('series', () {
    PurchaseOrderLine conSeries({int ordered = 3}) => PurchaseOrderLine(
          productId: 'x9',
          productName: 'Tensiómetro digital',
          orderedQuantity: ordered,
          receivedQuantity: 0,
          pendingQuantity: ordered,
          orderUnitPrice: 250,
          trackingMode: 'serial',
        );

    PurchaseDelivery build(List<PurchaseDeliveryLine> lines) =>
        PurchaseDelivery(
          purchaseId: 'ord-9',
          deliveryDate: DateTime(2026, 8, 16),
          detail: lines,
          operationUid: newUid(),
        );

    test('las series viajan en la línea que las usa', () {
      final line = PurchaseDeliveryLine(conSeries())
        ..serialNumbers = ['SN-1', 'SN-2', 'SN-3'];

      final detalle = (build([line]).toJson()['Detail'] as List).single
          as Map<String, dynamic>;

      expect(detalle['SerialNumbers'], ['SN-1', 'SN-2', 'SN-3']);
      // El lote no aplica a un producto identificado por serie.
      expect(detalle, isNot(contains('LotCode')));
    });

    test('un producto sin series no las manda', () {
      final json = build([PurchaseDeliveryLine(_line())]).toJson();
      expect((json['Detail'] as List).single, isNot(contains('SerialNumbers')));
    });

    test('se detecta cuando faltan o sobran números', () {
      // Una unidad, un número: el servidor rechaza la entrega completa si no
      // coinciden, así que hay que cortar antes de enviar.
      final faltan = PurchaseDeliveryLine(conSeries())..serialNumbers = ['SN-1'];
      final sobran = PurchaseDeliveryLine(conSeries())
        ..serialNumbers = ['A', 'B', 'C', 'D'];
      final justas = PurchaseDeliveryLine(conSeries())
        ..serialNumbers = ['A', 'B', 'C'];

      expect(build([faltan]).linesWithSerialMismatch, hasLength(1));
      expect(build([sobran]).linesWithSerialMismatch, hasLength(1));
      expect(build([justas]).linesWithSerialMismatch, isEmpty);
    });

    test('una línea que no se recibe no exige series', () {
      final line = PurchaseDeliveryLine(conSeries())..deliveryQuantity = 0;
      expect(build([line]).linesWithSerialMismatch, isEmpty);
    });

    test('el modo serial se lee del detalle del pedido', () {
      final line = PurchaseOrderLine.fromJson({
        'ProductId': 'x9',
        'ProductName': 'Tensiómetro',
        'OrderedQuantity': 2,
        'TrackingMode': 'serial',
      });

      expect(line.usesSerial, isTrue);
      expect(line.usesLot, isFalse);
    });
  });

  test('el pendiente se deriva si el API no lo manda', () {
    // Defensa: una línea con saldo real no puede presentarse como completa,
    // porque la pantalla desactivaría sus campos.
    final line = PurchaseOrderLine.fromJson({
      'ProductId': 'x1',
      'ProductName': 'Ibuprofeno',
      'OrderedQuantity': 10,
      'ReceivedQuantity': 4,
      'OrderUnitPrice': 3,
    });

    expect(line.pendingQuantity, 6);
  });
}
