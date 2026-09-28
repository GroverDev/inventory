import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/models/held_sale.dart';

void main() {
  group('HeldPayload', () {
    test('ida y vuelta conserva cliente, líneas y descuento global', () {
      const payload = HeldPayload(
        customer: HeldCustomer(
            id: 'c1', fullName: 'Juana Pérez', documentNumber: '123456'),
        lines: [
          HeldLine(
            productId: 'p1',
            productName: 'Paracetamol 500mg',
            unitPrice: 12.5,
            quantity: 2,
            discountId: 'd1',
            discountLabel: '10%',
            discountType: 'Percentage',
            discountValue: 10,
            lineSubtotal: 25,
            lineTotalDiscounts: 2.5,
            lineTotal: 22.5,
          ),
        ],
        header: HeldHeaderDiscount(
            id: '', label: 'Manual 5%', type: 'Percentage', value: 5),
      );

      final decoded = HeldPayload.fromJson(
          jsonDecode(jsonEncode(payload.toJson())) as Map<String, dynamic>);

      expect(decoded.v, 1);
      expect(decoded.customer!.id, 'c1');
      expect(decoded.customer!.fullName, 'Juana Pérez');
      expect(decoded.lines, hasLength(1));
      expect(decoded.lines.first.productId, 'p1');
      expect(decoded.lines.first.quantity, 2);
      expect(decoded.lines.first.discountType, 'Percentage');
      expect(decoded.header.type, 'Percentage');
      expect(decoded.header.value, 5);
    });

    test('el nivel superior y "header" van en minúscula; "customer" y las '
        'líneas en PascalCase — igual que useHeldSales.ts', () {
      const payload = HeldPayload(
        customer: HeldCustomer(id: 'c1', fullName: 'Juana Pérez'),
        lines: [
          HeldLine(
              productId: 'p1', productName: 'X', unitPrice: 1, quantity: 1),
        ],
        header:
            HeldHeaderDiscount(id: 'd1', label: 'Y', type: 'FixedAmount', value: 3),
      );

      final json = payload.toJson();

      expect(json.keys, containsAll(['v', 'customer', 'lines', 'header']));
      expect((json['customer'] as Map).keys, contains('FullName'));
      expect((json['lines'] as List).first, isA<Map>());
      expect(((json['lines'] as List).first as Map).keys,
          containsAll(['ProductId', 'ProductName', 'Quantity']));
      expect((json['header'] as Map).keys,
          containsAll(['id', 'label', 'type', 'value']));
    });

    test('un JSON tal como lo escribe la web se lee sin perder nada', () {
      // Ejemplo real de HeldPayload serializado por holdCurrentSale en
      // PointOfSaleView.vue — si la web cambia este formato, este test avisa.
      const webJson = '''
      {
        "v": 1,
        "customer": { "Id": "c9", "FullName": "Cliente Genérico",
                       "DocumentNumber": "0", "Email": "", "Cellphone": "",
                       "IsActive": true },
        "lines": [
          { "Id": "", "SaleId": "", "ProductId": "p7", "Quantity": 3,
            "UnitPrice": 8.5, "LineSubtotal": 25.5, "LineTotalDiscounts": 0,
            "LineTotal": 25.5, "EffectiveUnitPrice": 0, "ProductName": "Ibuprofeno",
            "LaboratoryName": "", "LotCode": null, "ExpiryDate": null,
            "SerialNumber": null, "SerialNumbers": [], "isSelected": false,
            "DiscountId": "", "DiscountLabel": "", "DiscountType": "", "DiscountValue": 0 }
        ],
        "header": { "id": "", "label": "", "type": "", "value": 0 }
      }
      ''';

      final decoded =
          HeldPayload.fromJson(jsonDecode(webJson) as Map<String, dynamic>);

      expect(decoded.customer!.fullName, 'Cliente Genérico');
      expect(decoded.lines.single.productId, 'p7');
      expect(decoded.lines.single.productName, 'Ibuprofeno');
      expect(decoded.lines.single.quantity, 3);
      expect(decoded.header.type, isEmpty);
    });

    test('sin cliente, "customer" es null', () {
      final decoded = HeldPayload.fromJson({
        'v': 1,
        'customer': null,
        'lines': <dynamic>[],
        'header': {'id': '', 'label': '', 'type': '', 'value': 0},
      });

      expect(decoded.customer, isNull);
      expect(decoded.lines, isEmpty);
    });
  });
}
