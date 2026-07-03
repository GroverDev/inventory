import '../core/network/api_client.dart';
import '../models/discount.dart';

/// Catálogo de descuentos predefinidos (GET api/Discounts).
class DiscountService {
  DiscountService(this._api);
  final ApiClient _api;

  Future<List<Discount>> active() async {
    final res = await _api.get<List<Discount>>(
      'api/Discounts',
      (data) => (data as List? ?? [])
          .map((e) => Discount.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return (res.data ?? <Discount>[]).where((d) => d.isActive).toList();
  }
}
