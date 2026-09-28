import 'dart:convert';

import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
import '../models/held_sale.dart';

/// Ventas en espera de la sucursal activa (aparcar y retomar).
///
/// Mismo backend que usa la web (`api/HeldSale`, ver `HeldSaleController.cs`):
/// lo que se aparca en un lado se retoma en el otro, porque la venta en
/// espera pertenece a la sucursal, no al dispositivo que la dejó.
class HeldSaleService {
  HeldSaleService(this._api);
  final ApiClient _api;

  Future<List<HeldSale>> getHeld() async {
    final res = await _api.get<List<HeldSale>>(
      'api/HeldSale',
      (data) => (data as List? ?? [])
          .map((e) => HeldSale.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return res.data ?? <HeldSale>[];
  }

  Future<String> hold(
    HeldPayload payload, {
    required String label,
    String? customerId,
    required int itemsCount,
    required double total,
  }) async {
    final res = await _api.post<String>(
      'api/HeldSale',
      (data) => data?.toString() ?? '',
      body: {
        'Label': label,
        'CustomerId': customerId,
        'ItemsCount': itemsCount,
        'Total': total,
        'Payload': jsonEncode(payload.toJson()),
      },
    );
    return res.data ?? '';
  }

  /// La saca de la espera y devuelve su carrito. Lanza [ApiException] si otra
  /// caja la retomó o la descartó antes (`take` es un DELETE...RETURNING
  /// atómico del lado del servidor: no hay forma de que dos cajas la cobren).
  Future<HeldSale> take(String id) async {
    final res = await _api.post<HeldSale>(
      'api/HeldSale/$id/take',
      (data) => HeldSale.fromJson(data as Map<String, dynamic>),
      body: const {},
    );
    if (res.data == null) {
      throw ApiException('No se pudo retomar la venta en espera.');
    }
    return res.data!;
  }

  Future<void> discard(String id) async {
    await _api.delete<bool>('api/HeldSale/$id', (data) => data == true);
  }
}
