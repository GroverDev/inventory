import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/network/api_response.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/core/utils/uid.dart';
import 'package:inventory_movil/models/purchase.dart';
import 'package:inventory_movil/services/purchase_service.dart';

/// Responde lo que se le diga, sin salir a la red.
class _StubApiClient extends ApiClient {
  _StubApiClient(this._response) : super(AuthStorage());

  final ApiResponse<dynamic> _response;
  String? path;
  Object? body;

  @override
  Future<ApiResponse<T>> put<T>(
    String path,
    T Function(dynamic data) parse, {
    Object? body,
  }) async {
    this.path = path;
    this.body = body;
    return ApiResponse<T>(
      ok: _response.ok,
      data: _response.ok ? parse(_response.data) : null,
      message: _response.message,
    );
  }
}

PurchaseDelivery _delivery() => PurchaseDelivery(
      purchaseId: 'ord-1',
      deliveryDate: DateTime(2026, 8, 13),
      detail: [
        PurchaseDeliveryLine(PurchaseOrderLine(
          productId: 'x1',
          productName: 'Paracetamol 500 mg',
          orderedQuantity: 10,
          receivedQuantity: 0,
          pendingQuantity: 10,
          orderUnitPrice: 7.5,
        )),
      ],
      operationUid: newUid(),
    );

void main() {
  test('una recepción aceptada se reporta como aplicada', () async {
    final api = _StubApiClient(
      ApiResponse<dynamic>(ok: true, data: true, message: ApiMessage()),
    );

    final result = await PurchaseService(api).receive(_delivery());

    expect(result.outcome, PurchaseReceiptOutcome.applied);
    expect(api.path, 'api/Purchases/reciveOrders/ord-1');
  });

  test('el reintento ya aplicado no se trata como error', () async {
    // El uid chocó contra el índice único: la mercadería ya entró. El backend
    // lo responde con ok:false y MessageType `info`, que el ApiClient no
    // convierte en excepción — sin este manejo, la pantalla se quedaba muda
    // justo en el caso que el uid existe para cubrir.
    final api = _StubApiClient(ApiResponse<dynamic>(
      ok: false,
      data: null,
      message: ApiMessage(
        description: 'Esta recepción ya fue registrada.',
        messageType: 'info',
      ),
    ));

    final result = await PurchaseService(api).receive(_delivery());

    expect(result.outcome, PurchaseReceiptOutcome.alreadyRegistered);
    expect(result.message, 'Esta recepción ya fue registrada.');
  });

  test('un rechazo de negocio llega al usuario con su motivo', () async {
    // PurchaseReceiptPolicy responde con MessageType `warning`, que tampoco
    // dispara excepción en el ApiClient: rescatarlo acá es lo que evita que un
    // "no puede recibir más de X" se vea como si todo hubiera salido bien.
    final api = _StubApiClient(ApiResponse<dynamic>(
      ok: false,
      data: null,
      message: ApiMessage(
        description: "No se puede recibir 12 de 'Paracetamol': el pendiente es 10.",
        messageType: 'warning',
      ),
    ));

    expect(
      () => PurchaseService(api).receive(_delivery()),
      throwsA(isA<ApiException>().having((e) => e.message, 'message',
          contains('el pendiente es 10'))),
    );
  });

  test('un cierre rechazado no se confunde con un cierre exitoso', () async {
    final api = _StubApiClient(ApiResponse<dynamic>(
      ok: false,
      data: null,
      message: ApiMessage(
        description: 'La orden no tiene recepciones: corresponde cancelarla.',
        messageType: 'warning',
      ),
    ));

    expect(
      () => PurchaseService(api).close('ord-1'),
      throwsA(isA<ApiException>()),
    );
  });
}
