import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/network/api_client.dart';
import 'package:inventory_movil/core/network/api_response.dart';
import 'package:inventory_movil/core/storage/auth_storage.dart';
import 'package:inventory_movil/services/purchase_service.dart';

/// Anota la petición en vez de emitirla, para poder mirar la query que salió.
class _CapturingApiClient extends ApiClient {
  _CapturingApiClient() : super(AuthStorage());

  String? path;
  Map<String, dynamic>? query;

  @override
  Future<ApiResponse<T>> get<T>(
    String path,
    T Function(dynamic data) parse, {
    Map<String, dynamic>? query,
  }) async {
    this.path = path;
    this.query = query;
    return ApiResponse<T>(ok: true, data: parse([]), message: ApiMessage());
  }
}

void main() {
  test('la lista de pedidos manda los tres filtros que exige el backend',
      () async {
    // GET api/Purchases los declara obligatorios y su SQL hace
    // `purchase_status_id = @PurchaseStatusId` sin comodín: sin estos
    // parámetros la pantalla queda vacía y sin ningún error visible.
    final api = _CapturingApiClient();

    await PurchaseService(api).list(
      from: DateTime(2026, 8, 1),
      to: DateTime(2026, 8, 11),
      statusId: 1,
    );

    expect(api.path, 'api/Purchases');
    expect(api.query, {
      'purchaseDateInitial': '2026-08-01',
      'purchaseDateEnd': '2026-08-11',
      // El backend le concatena " 00:00:01" / " 23:59:59" y lo parsea, así que
      // las fechas van en ISO igual que en el POST.
      'purchaseStatus': 1,
    });
  });
}
