import '../core/network/api_client.dart';
import '../models/catalog.dart';

/// Lecturas de catálogos auxiliares para formularios y POS.
/// Rutas según convención `api/[controller]` del backend.
class CatalogService {
  CatalogService(this._api);
  final ApiClient _api;

  Future<List<NamedItem>> _named(String path, List<String> nameKeys) async {
    final res = await _api.get<List<NamedItem>>(
      path,
      (data) => (data as List)
          .map((e) => NamedItem.from(e as Map<String, dynamic>, nameKeys))
          .toList(),
    );
    return res.data ?? <NamedItem>[];
  }

  Future<List<NamedItem>> categories() =>
      _named('api/Category', ['CategoryName', 'Name']);

  Future<List<NamedItem>> laboratories() =>
      _named('api/Laboratory', ['LaboratoryName', 'Name']);

  Future<List<NamedItem>> unitsOfMeasurement() =>
      _named('api/UnitOfMeasurement', ['UnitName', 'Name', 'Description']);

  Future<List<NamedItem>> providers() =>
      _named('api/Provider', ['ProviderName', 'Name', 'BusinessName']);

  Future<List<NamedItem>> customers() =>
      _named('api/Customers', ['CustomerName', 'FullName', 'Name']);

  /// Búsqueda de clientes por nombre para el POS (GET api/Customers?CustomerName=).
  Future<List<Customer>> searchCustomers(String name) async {
    final res = await _api.get<List<Customer>>(
      'api/Customers',
      (data) => (data as List? ?? [])
          .map((e) => Customer.fromJson(e as Map<String, dynamic>))
          .toList(),
      query: {'CustomerName': name},
    );
    return res.data ?? <Customer>[];
  }

  Future<List<PaymentMethod>> paymentMethods() async {
    final res = await _api.get<List<PaymentMethod>>(
      'api/PaymentMethod',
      (data) => (data as List)
          .map((e) => PaymentMethod.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return res.data ?? <PaymentMethod>[];
  }

  Future<List<PurchaseStatus>> purchaseStatuses() async {
    final res = await _api.get<List<PurchaseStatus>>(
      'api/PurchaseStatus',
      (data) => (data as List)
          .map((e) => PurchaseStatus.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return res.data ?? <PurchaseStatus>[];
  }
}
