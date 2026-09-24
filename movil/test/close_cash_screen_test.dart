import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/network/api_response.dart';
import 'package:inventory_movil/features/cash/close_cash_screen.dart';
import 'package:inventory_movil/models/cash_session.dart';
import 'package:inventory_movil/models/login_models.dart';
import 'package:inventory_movil/services/sale_service.dart';
import 'package:provider/provider.dart';

/// Responde como el servidor: la primera vez falta la observación, la segunda
/// hace falta supervisor, y con su token cierra.
class FakeSales extends Fake implements SaleService {
  FakeSales(this.settings);
  final CashCloseSettings settings;
  final calls = <({Map<String, double> counts, List<DenominationCount> den, String notes, String token})>[];
  bool strict = true;

  @override
  Future<CashCloseSettings> closeSettings() async => settings;

  @override
  Future<CashSession> closeSession(String sessionId,
      {required Map<String, double> counts,
      List<DenominationCount> denominations = const [],
      String notes = '',
      String supervisorAuthToken = ''}) async {
    calls.add((counts: counts, den: denominations, notes: notes, token: supervisorAuthToken));
    if (strict && notes.isEmpty) {
      throw ApiException('Hay diferencia en Efectivo.', messageType: 'warning', data: {'CloseRequires': 'note'});
    }
    if (strict && supervisorAuthToken.isEmpty) {
      throw ApiException('Necesita la autorización de un supervisor.',
          messageType: 'warning', data: {'CloseRequires': 'supervisor'});
    }
    return CashSession.fromJson({
      'Id': sessionId,
      'OpenedAt': '2026-09-23T10:00:00Z',
      'ClosedAt': '2026-09-23T20:00:00Z',
      'OpeningAmount': 100,
      'TotalSales': 0,
      'CloseAuthorizedByName': supervisorAuthToken.isEmpty ? '' : 'Grover',
      'Counts': [
        {'PaymentMethodId': 'e', 'Name': 'Efectivo', 'Expected': 100, 'Declared': counts['e'], 'Difference': counts['e']! - 100},
      ],
    });
  }

  @override
  Future<LoginResponse> supervisorLogin(String email, String password) async => LoginResponse(
        userId: 1, fullName: 'Grover', userName: 'g', email: email, token: 'TOKEN-SUP',
        refreshToken: '', requireTotp: false, totpSetupRequired: false, totpSessionToken: '',
        rolId: 4, rolName: 'SuperAdmin', changePassword: false,
      );
}

final sesion = CashSession.fromJson({'Id': 'caja-1', 'OpenedAt': '2026-09-23T10:00:00Z', 'OpeningAmount': 100, 'TotalSales': 0});

const metodos = [
  CloseMethod(id: 'e', name: 'Efectivo', affectsCash: true, requiresCount: true),
  CloseMethod(id: 'q', name: 'QR', requiresCount: true),
  CloseMethod(id: 't', name: 'Tarjeta'),
];

Future<void> abrir(WidgetTester t, FakeSales fake) async {
  t.view.physicalSize = const Size(1080, 2400);
  t.view.devicePixelRatio = 2.5;
  addTearDown(t.view.reset);
  await t.pumpWidget(Provider<SaleService>.value(
    value: fake,
    child: MaterialApp(home: CloseCashScreen(session: sesion)),
  ));
  await t.pumpAndSettle();
}

Finder campo(String label) => find.widgetWithText(TextField, label);

void main() {
  testWidgets('a ciegas: pide observación, después supervisor, y muestra el resultado', (t) async {
    final fake = FakeSales(const CashCloseSettings(methods: metodos));
    await abrir(t, fake);

    // Solo los medios que se arquean, y ningún esperado a la vista.
    expect(campo('Efectivo'), findsOneWidget);
    expect(campo('QR'), findsOneWidget);
    expect(campo('Tarjeta'), findsNothing);
    expect(find.textContaining('Esperado'), findsNothing);

    await t.enterText(campo('Efectivo'), '30');
    await t.tap(find.text('Cerrar y arquear'));
    await t.pumpAndSettle();
    expect(find.textContaining('Declare QR'), findsOneWidget);   // un medio sin declarar
    expect(fake.calls, isEmpty);

    await t.enterText(campo('QR'), '0');
    await t.tap(find.text('Cerrar y arquear'));
    await t.pumpAndSettle();
    expect(find.text('Hay diferencia en Efectivo.'), findsOneWidget);

    await t.enterText(campo('Observaciones'), 'Faltante por pago a proveedor');
    await t.tap(find.text('Cerrar y arquear'));
    await t.pumpAndSettle();
    expect(find.text('Autorización requerida'), findsOneWidget);

    await t.enterText(find.widgetWithText(TextField, 'Email del supervisor'), 'grover@ideanueva.com');
    await t.enterText(find.widgetWithText(TextField, 'Contraseña'), 'x');
    await t.tap(find.text('Autorizar'));
    await t.pumpAndSettle();

    expect(fake.calls.last.token, 'TOKEN-SUP');
    expect(fake.calls.last.counts, {'e': 30.0, 'q': 0.0});
    expect(find.text('Caja cerrada'), findsOneWidget);
    expect(find.text('Faltante'), findsOneWidget);
    expect(find.text('Autorizó Grover'), findsOneWidget);
  });

  testWidgets('con billetes obligatorios el efectivo es la suma del conteo', (t) async {
    final fake = FakeSales(const CashCloseSettings(methods: metodos, requireDenominations: true))..strict = false;
    await abrir(t, fake);

    final efectivo = t.widget<TextField>(campo('Efectivo'));
    expect(efectivo.readOnly, isTrue);
    expect(efectivo.controller!.text, '0.00');

    await t.enterText(find.byKey(const ValueKey('den-100.0')), '2');
    await t.enterText(find.byKey(const ValueKey('den-0.5')), '3');
    await t.pump();
    expect(efectivo.controller!.text, '201.50');

    await t.enterText(campo('QR'), '0');
    await t.tap(find.text('Cerrar y arquear'));
    await t.pumpAndSettle();

    final enviado = fake.calls.single;
    expect(enviado.counts['e'], 201.5);
    expect(enviado.den.fold<double>(0, (s, d) => s + d.value * d.quantity), 201.5);
  });
}
