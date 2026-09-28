import 'dart:typed_data';

import 'package:dio/dio.dart';

import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
import '../models/product.dart';

class ProductService {
  ProductService(this._api);
  final ApiClient _api;

  /// GET api/Product/stock?productName=&page=&pageSize= (paginado)
  Future<({List<Product> items, int totalCount})> getStockPaged({
    String search = '',
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await _api.get<List<Product>>(
      'api/Product/stock',
      (data) => (data as List)
          .map((e) => Product.fromJson(e as Map<String, dynamic>))
          .toList(),
      query: {'productName': search, 'page': page, 'pageSize': pageSize},
    );
    return (items: res.data ?? <Product>[], totalCount: res.totalCount);
  }

  /// GET api/Product?productName= — catálogo completo (sin paginar), igual
  /// que la web. Se usa en el POS para cargar todo y filtrar en memoria.
  Future<List<Product>> getAll({String search = ''}) async {
    final res = await _api.get<List<Product>>(
      'api/Product',
      (data) => (data as List? ?? [])
          .map((e) => Product.fromJson(e as Map<String, dynamic>))
          .toList(),
      query: {'productName': search},
    );
    return res.data ?? <Product>[];
  }

  /// GET api/Product/{id}/validate — precio y stock vigentes, sin traer todo
  /// el producto. Lo usa la ficha del POS para no mostrar datos que
  /// cambiaron mientras la tarjeta seguía en pantalla (otra caja vendió, se
  /// corrigió el precio).
  Future<({double salePrice, int currentStock})?> validateSelection(
      String productId) async {
    final res = await _api.get<({double salePrice, int currentStock})?>(
      'api/Product/$productId/validate',
      (data) {
        if (data == null) return null;
        final m = data as Map<String, dynamic>;
        return (
          salePrice: (m['SalePrice'] ?? 0).toDouble(),
          currentStock: (m['CurrentStock'] as num? ?? 0).toInt(),
        );
      },
    );
    return res.data;
  }

  /// GET api/Product/{id}
  Future<Product> getById(String id) async {
    final res = await _api.get<Product>(
      'api/Product/$id',
      (data) => Product.fromJson(data as Map<String, dynamic>),
    );
    if (res.data == null) throw ApiException('Producto no encontrado.');
    return res.data!;
  }

  /// POST api/Product — devuelve el id del producto recién creado (lo
  /// necesita quien tenga una imagen pendiente de subir).
  Future<String> create(Product p) async {
    final res = await _api.post<String>(
      'api/Product',
      (data) => data?.toString() ?? '',
      body: p.toRequest(),
    );
    return res.data ?? '';
  }

  /// PUT api/Product/{id}
  Future<void> update(Product p) async {
    await _api.put<bool>(
      'api/Product/${p.id}',
      (data) => data == true,
      body: p.toRequest(),
    );
  }

  /// DELETE api/Product/{id}
  Future<void> delete(String id) async {
    await _api.delete<bool>('api/Product/$id', (data) => data == true);
  }

  /// POST api/Product/{id}/image (multipart, campo "file") — sube o
  /// reemplaza la imagen. Devuelve la ruta relativa que quedó guardada.
  Future<String> uploadImage(
      String productId, Uint8List bytes, String fileName) async {
    final form = FormData.fromMap({
      'file': MultipartFile.fromBytes(bytes, filename: fileName),
    });
    final res = await _api.post<String>(
      'api/Product/$productId/image',
      (data) => data?.toString() ?? '',
      body: form,
    );
    if (res.data == null || res.data!.isEmpty) {
      throw ApiException('No se pudo subir la imagen.');
    }
    return res.data!;
  }

  /// DELETE api/Product/{id}/image
  Future<void> deleteImage(String productId) async {
    await _api.delete<bool>(
        'api/Product/$productId/image', (data) => data == true);
  }
}
