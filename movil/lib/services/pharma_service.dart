import '../core/network/api_client.dart';
import '../models/pharma.dart';

/// Datos farmacéuticos de un producto, solo lectura (la ficha se llena desde
/// la web). Todo se pide al abrir la ficha del POS y no con la lista de
/// productos: son varias consultas por producto, y la grilla muestra cientos.
class PharmaService {
  PharmaService(this._api);
  final ApiClient _api;

  Future<ProductPharma?> getByProduct(String productId) async {
    final res = await _api.get<ProductPharma?>(
      'api/Pharma/product/$productId',
      (data) =>
          data == null ? null : ProductPharma.fromJson(data as Map<String, dynamic>),
    );
    return res.data;
  }

  /// Las automáticas (misma composición) y las manuales de la farmacia, en
  /// una sola lista — cada una marcada con `isManual`.
  Future<List<ProductEquivalent>> getEquivalents(String productId) async {
    final res = await _api.get<List<ProductEquivalent>>(
      'api/Pharma/product/$productId/equivalents',
      (data) => (data as List? ?? [])
          .map((e) => ProductEquivalent.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return res.data ?? <ProductEquivalent>[];
  }

  /// Prospecto del producto, si lo tiene cargado. La mayoría no lo tiene,
  /// por eso va aparte de la ficha.
  Future<String> getLeaflet(String productId) async {
    final res = await _api.get<String?>(
      'api/Pharma/product/$productId/leaflet',
      (data) => data?.toString(),
    );
    return res.data ?? '';
  }
}
