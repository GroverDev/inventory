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

  /// GET api/Product/{id}
  Future<Product> getById(String id) async {
    final res = await _api.get<Product>(
      'api/Product/$id',
      (data) => Product.fromJson(data as Map<String, dynamic>),
    );
    if (res.data == null) throw ApiException('Producto no encontrado.');
    return res.data!;
  }

  /// POST api/Product — devuelve el id del producto creado.
  Future<String> create(Product p) async {
    final res = await _api.post<String>(
      'api/Product',
      (data) => data?.toString() ?? '',
      body: p.toRequest(),
    );
    return res.data ?? '';
  }

  /// POST api/Product/{id}/image — sube (o reemplaza) la imagen. Devuelve la
  /// ruta relativa que quedó guardada. El servidor la reduce y valida formato
  /// y tamaño (5 MB); un rechazo llega como [ApiException].
  Future<String> uploadImage(String productId, String filePath, String fileName) async {
    final res = await _api.postForm<String>(
      'api/Product/$productId/image',
      (data) => data?.toString() ?? '',
      buildForm: () => FormData.fromMap({
        'file': MultipartFile.fromFileSync(filePath, filename: fileName),
      }),
    );
    return res.data ?? '';
  }

  /// DELETE api/Product/{id}/image
  Future<void> deleteImage(String productId) async {
    await _api.delete<bool>('api/Product/$productId/image', (data) => data == true);
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
}
