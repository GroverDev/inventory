import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
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

  /// El cliente genérico del tenant (GET api/Customers/default), que el POS
  /// precarga para no bloquear una venta sin cliente identificado.
  Future<Customer?> getDefaultCustomer() async {
    final res = await _api.get<Customer>(
      'api/Customers/default',
      (data) => Customer.fromJson(data as Map<String, dynamic>),
    );
    return res.data;
  }

  /// POST api/Customers — alta rápida desde el cobro. Devuelve el cliente
  /// recién creado, listo para seleccionar en la venta.
  Future<Customer> createCustomer({
    required String fullName,
    required String documentNumber,
    String cellphone = '',
    String email = '',
  }) async {
    final res = await _api.post<String>(
      'api/Customers',
      (data) => data as String,
      body: {
        'FullName': fullName,
        'DocumentNumber': documentNumber,
        'Cellphone': cellphone,
        'Email': email,
        'IsActive': true,
      },
    );
    final id = res.data;
    if (!res.ok || id == null || id.isEmpty) {
      throw ApiException(
        res.message.description.isNotEmpty
            ? res.message.description
            : 'No se pudo crear el cliente.',
      );
    }
    return Customer(id: id, fullName: fullName, documentNumber: documentNumber);
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
