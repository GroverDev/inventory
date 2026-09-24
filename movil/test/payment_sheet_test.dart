import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/features/pos/payment_sheet.dart';
import 'package:inventory_movil/models/catalog.dart';
import 'package:inventory_movil/models/sale.dart';

final metodos = [
  PaymentMethod(id: 'q', name: 'QR'),
  PaymentMethod(id: 'e', name: 'Efectivo', requiresChanges: true, affectsCash: true),
];

/// Abre el modal de cobro por [total] y deja el resultado en [out].
Future<void> abrir(WidgetTester t, double total, List<List<SalePayment>?> out) async {
  await t.pumpWidget(MaterialApp(
    home: Builder(
      builder: (ctx) => Scaffold(
        body: TextButton(
          onPressed: () async => out.add(await showPaymentSheet(ctx, total: total, methods: metodos)),
          child: const Text('cobrar'),
        ),
      ),
    ),
  ));
  await t.tap(find.text('cobrar'));
  await t.pumpAndSettle();
}

ChoiceChip chip(WidgetTester t, String nombre) =>
    t.widget<ChoiceChip>(find.widgetWithText(ChoiceChip, nombre));

void main() {
  testWidgets('efectivo elegido, billete sugerido, vuelto y confirmar', (t) async {
    final out = <List<SalePayment>?>[];
    await abrir(t, 37.5, out);

    expect(chip(t, 'Efectivo').selected, isTrue);          // efectivo preseleccionado
    expect(find.text('Por cobrar'), findsOneWidget);
    expect(find.text('Bs 37.50'), findsOneWidget);

    await t.tap(find.widgetWithText(ActionChip, 'Bs. 50'));
    await t.pump();
    expect(find.text('Vuelto'), findsOneWidget);
    expect(find.text('Bs 12.50'), findsOneWidget);

    // Confirmar sin tocar "Agregar": lo escrito cuenta.
    await t.tap(find.text('Confirmar venta'));
    await t.pumpAndSettle();
    final pagos = out.single!;
    expect(pagos.single.amountGiven, 50);
    expect(pagos.single.amountReturned, 12.5);
  });

  testWidgets('el QR arranca con el saldo y no deja cobrar de más', (t) async {
    final out = <List<SalePayment>?>[];
    await abrir(t, 37.5, out);

    await t.tap(find.widgetWithText(ChoiceChip, 'QR'));
    await t.pump();
    expect(find.widgetWithText(TextField, '37.50'), findsOneWidget);

    await t.enterText(find.byType(TextField), '40');
    await t.pump();
    expect(find.text('QR no da vuelto'), findsOneWidget);
    expect(t.widget<FilledButton>(find.widgetWithText(FilledButton, 'Confirmar venta')).onPressed, isNull);
  });

  testWidgets('pago mixto: QR parcial y el resto vuelve a efectivo', (t) async {
    final out = <List<SalePayment>?>[];
    await abrir(t, 37.5, out);

    await t.tap(find.widgetWithText(ChoiceChip, 'QR'));
    await t.pump();
    await t.enterText(find.byType(TextField), '20');
    await t.tap(find.text('Agregar'));
    await t.pump();

    expect(chip(t, 'Efectivo').selected, isTrue);
    expect(find.text('Bs 17.50'), findsOneWidget);          // lo que falta

    await t.enterText(find.byType(TextField), '20');
    await t.testTextInput.receiveAction(TextInputAction.done);   // Enter confirma
    await t.pumpAndSettle();
    final pagos = out.single!;
    expect(pagos.map((p) => (p.paymentMethodId, p.amountGiven, p.amountReturned)),
        [('q', 20.0, 0.0), ('e', 20.0, 2.5)]);
  });
}
