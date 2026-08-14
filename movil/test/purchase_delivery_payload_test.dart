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
