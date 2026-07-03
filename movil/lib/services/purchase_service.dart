import '../core/network/api_client.dart';
import '../models/purchase.dart';

class PurchaseService {
  PurchaseService(this._api);
  final ApiClient _api;

  /// GET api/Purchases — lista de pedidos.
  Future<List<PurchaseSummary>> list() async {
    final res = await _api.get<List<PurchaseSummary>>(
      'api/Purchases',
      (data) {
        // Soporta tanto lista directa como objeto paginado { Items: [...] }.
        final list = data is Map && data['Items'] is List
            ? data['Items'] as List
            : data as List;
        return list
            .map((e) => PurchaseSummary.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
    return res.data ?? <PurchaseSummary>[];
  }

  /// POST api/Purchases — crea un pedido.
  Future<String> create(PurchaseRequest req) async {
    final res = await _api.post<String>(
      'api/Purchases',
      (data) => data?.toString() ?? '',
      body: req.toJson(),
    );
    return res.message.description;
  }
}
