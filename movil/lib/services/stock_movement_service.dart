import '../core/network/api_client.dart';
import '../models/stock_serial.dart';

class StockMovementService {
  StockMovementService(this._api);
  final ApiClient _api;

  /// GET api/StockMovement/serials/{productId} — unidades serializadas
  /// disponibles, para que el mostrador elija cuál entrega.
  Future<List<StockSerial>> availableSerials(String productId) async {
    final res = await _api.get<List<StockSerial>>(
      'api/StockMovement/serials/$productId',
      (data) => (data as List? ?? [])
          .map((e) => StockSerial.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return res.data ?? <StockSerial>[];
  }
}
