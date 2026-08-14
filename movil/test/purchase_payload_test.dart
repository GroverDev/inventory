import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/models/product.dart';
import 'package:inventory_movil/models/purchase.dart';

void main() {
  // El backend convierte estas fechas con CultureInfo.InvariantCulture, que
  // solo entiende MM/dd/yyyy o ISO. Con dd/MM/yyyy, un día 18 falla y un día 8
  // pasa como agosto. ISO es lo que ya manda la webapp.
  test('las fechas del pedido viajan en ISO yyyy-MM-dd', () {
    final req = PurchaseRequest(
      providerId: 'p1',
      providerName: 'Droguería Inti S.A.',
      purchaseStatusId: 1,
      // Día 18: bajo InvariantCulture no existe el mes 18, así que este es el
      // caso que hacía fallar al API.
      estimatedDeliveryDate: DateTime(2026, 8, 18),
      detail: [PurchaseLine(product: Product(id: 'x1', salePrice: 10))],
    );

    final json = req.toJson();

    expect(json['EstimatedDeliveryDate'], '2026-08-18');
    expect(json['PurchaseDate'], matches(r'^\d{4}-\d{2}-\d{2}$'));
  });

  test('un día menor a 13 tampoco sale ambiguo', () {
    final req = PurchaseRequest(
      providerId: 'p1',
      providerName: 'Droguería Inti S.A.',
      purchaseStatusId: 1,
      // 5 de agosto. Este es el caso silencioso: como "05/08/2026" el API no
      // se quejaba, lo leía como 8 de mayo y guardaba una fecha equivocada.
      estimatedDeliveryDate: DateTime(2026, 8, 5),
      detail: [PurchaseLine(product: Product(id: 'x1', salePrice: 10))],
    );

    expect(req.toJson()['EstimatedDeliveryDate'], '2026-08-05');
  });
}
