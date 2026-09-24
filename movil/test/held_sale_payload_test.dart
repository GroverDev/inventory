import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/models/cash_session.dart';
import 'package:inventory_movil/models/catalog.dart';
import 'package:inventory_movil/models/held_sale.dart';
import 'package:inventory_movil/models/product.dart';
import 'package:inventory_movil/models/sale.dart';

final paracetamol = Product(id: 'p1', productName: 'Paracetamol', salePrice: 5);
final ibuprofeno = Product(id: 'p2', productName: 'Ibuprofeno', salePrice: 8);
final productos = {'p1': paracetamol, 'p2': ibuprofeno};
final topes = PosSettings(maxCashierDiscountPct: 10, maxCashierDiscountAmount: 20, maxDiscountPct: 30);

RestoredCart retomar(String payload, {bool cajero = false, Set<String> catalogo = const {'d-cat'}}) =>
    HeldSalePayload.restore(payload,
        products: productos, activeDiscountIds: catalogo, settings: topes, isCashier: cajero);

/// Así lo guarda el POS web (holdCurrentSale): SaleDetail completo y cabecera en minúsculas.
final deLaWeb = jsonEncode({
  'v': 1,
  'customer': {'Id': 'c9', 'FullName': 'Ana Pérez', 'DocumentNumber': '123', 'Email': '', 'Cellphone': '', 'IsActive': true},
  'lines': [
    {
      'Id': '', 'SaleId': '', 'ProductId': 'p1', 'ProductName': 'Paracetamol', 'Quantity': 3, 'UnitPrice': 4.5,
      'LineSubtotal': 13.5, 'LineTotalDiscounts': 2.7, 'LineTotal': 10.8, 'EffectiveUnitPrice': 0,
      'LaboratoryName': '', 'LotCode': null, 'ExpiryDate': null, 'SerialNumber': null, 'SerialNumbers': [],
      'isSelected': false, 'DiscountId': '', 'DiscountLabel': 'Manual 20%', 'DiscountType': 'Percentage', 'DiscountValue': 20,
    },
    {'ProductId': 'p-baja', 'ProductName': 'Jarabe', 'Quantity': 1, 'UnitPrice': 12},
  ],
  'header': {'id': 'd-cat', 'label': 'Jubilado', 'type': 'Percentage', 'value': 5},
});

void main() {
  test('retoma una venta puesta en espera en la web', () {
    final r = retomar(deLaWeb);

    expect(r.customer?.fullName, 'Ana Pérez');
    expect(r.lines.single.product, same(paracetamol));
    expect(r.lines.single.quantity, 3);
    expect(r.lines.single.unitPrice, 5);                 // precio de hoy, no el guardado
    expect(r.lines.single.discountValue, 20);            // 20 % manual: bajo el tope de 30
    expect(r.header, (id: 'd-cat', label: 'Jubilado', type: 'Percentage', value: 5.0));
    expect(r.notices, [
      '«Paracetamol»: el precio cambió a 5.00.',
      '«Jarabe» ya no está disponible y se quitó.',
    ]);
  });

  test('para un cajero se quitan los descuentos que pedían supervisor o salieron del catálogo', () {
    final r = retomar(deLaWeb, cajero: true, catalogo: {});

    expect(r.lines.single.discountType, '');
    expect(r.header, isNull);
    expect(r.notices, containsAll([
      'Se quitó el descuento de «Paracetamol»: requiere autorizar de nuevo.',
      'Se quitó el descuento global: ya no está en el catálogo.',
    ]));
  });

  test('ida y vuelta: lo que guarda el móvil se retoma igual', () {
    final lineas = [
      SaleLine(product: paracetamol, quantity: 2),
      SaleLine(product: ibuprofeno, discountType: 'FixedAmount', discountValue: 1, discountLabel: 'Manual'),
    ];
    final payload = HeldSalePayload.build(
      lines: lineas,
      customer: Customer(id: 'c1', fullName: 'Juan', documentNumber: '9'),
      header: (id: '', label: 'Manual', type: 'FixedAmount', value: 2),
    );

    // La web lee estos campos: tienen que estar con esos nombres.
    final guardado = jsonDecode(payload) as Map<String, dynamic>;
    expect(guardado['v'], 1);
    expect((guardado['lines'] as List).first.keys,
        containsAll(['ProductId', 'ProductName', 'Quantity', 'UnitPrice', 'LineTotal', 'DiscountType', 'SerialNumbers']));
    expect(guardado['header'], {'id': '', 'label': 'Manual', 'type': 'FixedAmount', 'value': 2.0});

    final r = retomar(payload);
    expect(r.notices, isEmpty);
    expect(r.customer?.id, 'c1');
    expect(r.lines.map((l) => (l.product.id, l.quantity, l.discountValue)), [('p1', 2, 0.0), ('p2', 1, 1.0)]);
    expect(r.header?.value, 2);
  });

  test('un formato desconocido no se retoma a medias', () {
    expect(() => retomar(jsonEncode({'v': 2, 'lines': []})), throwsFormatException);
  });
}
