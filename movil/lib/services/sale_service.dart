import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
import '../models/cash_session.dart';
import '../models/login_models.dart';
import '../models/sale.dart';
import '../models/sale_history.dart';

class SaleService {
  SaleService(this._api);
  final ApiClient _api;

  /// POST api/Sales — devuelve el id de la venta creada (o el mensaje).
  Future<String> create(SaleRequest req) async {
    final res = await _api.post<String>(
      'api/Sales',
      (data) => data?.toString() ?? '',
      body: req.toJson(),
    );
    return res.data ?? '';
  }

  /// GET api/Sales — registro de ventas paginado en un rango de fechas.
  /// Las fechas se envían en formato `yyyy-MM-dd`.
  Future<SalesPage> getSales({
    required String dateInitial,
    required String dateEnd,
    int page = 1,
    int pageSize = 20,
    String? sellerName,
  }) async {
    final res = await _api.get<SalesPage>(
      'api/Sales',
      (data) => SalesPage.fromJson(data as Map<String, dynamic>),
      query: {
        'saleDateInitial': dateInitial,
        'saleDateEnd': dateEnd,
        'page': page,
        'pageSize': pageSize,
        if (sellerName != null && sellerName.isNotEmpty) 'sellerName': sellerName,
      },
    );
    return res.data ??
        SalesPage(
          items: const [],
          totalCount: 0,
          periodSubtotal: 0,
          periodDiscounts: 0,
          periodTotal: 0,
        );
  }

  /// GET api/Sales/{id} — venta con detalle, cobros y devoluciones.
  Future<SaleFull?> getSaleById(String id) async {
    final res = await _api.get<SaleFull?>(
      'api/Sales/$id',
      (data) => data == null
          ? null
          : SaleFull.fromJson(data as Map<String, dynamic>),
    );
    return res.data;
  }

  /// POST api/SaleReturn — registra la devolución de una venta.
  Future<String> createReturn(SaleReturnRequest req) async {
    final res = await _api.post<String>(
      'api/SaleReturn',
      (data) => data?.toString() ?? '',
      body: req.toJson(),
    );
    return res.message.description;
  }

  /// GET api/CashSession/active — sesión de caja abierta del usuario.
  Future<CashSession?> activeSession() async {
    final res = await _api.get<CashSession?>(
      'api/CashSession/active',
      (data) =>
          data == null ? null : CashSession.fromJson(data as Map<String, dynamic>),
    );
    return res.data;
  }

  /// POST api/CashSession/open
  Future<void> openSession(double openingAmount) async {
    await _api.post<String>(
      'api/CashSession/open',
      (data) => data?.toString() ?? '',
      body: {'OpeningAmount': openingAmount},
    );
  }

  /// PUT api/CashSession/{id}/close — arqueo de caja.
  Future<void> closeSession(
    String sessionId, {
    required double declaredAmount,
    String notes = '',
  }) async {
    await _api.put<String>(
      'api/CashSession/$sessionId/close',
      (data) => data?.toString() ?? '',
      body: {'DeclaredAmount': declaredAmount, 'Notes': notes},
    );
  }

  /// POST api/CashSession/{id}/movements — gasto / retiro / ingreso.
  Future<void> addMovement(
    String sessionId, {
    required String movementType, // 'expense' | 'withdrawal' | 'income'
    required double amount,
    required String description,
  }) async {
    await _api.post<String>(
      'api/CashSession/$sessionId/movements',
      (data) => data?.toString() ?? '',
      body: {
        'CashSessionId': sessionId,
        'MovementType': movementType,
        'Amount': amount,
        'Description': description,
      },
    );
  }

  /// GET api/Settings/pos — límites de descuento para cajeros.
  Future<PosSettings> posSettings() async {
    final res = await _api.get<PosSettings?>(
      'api/Settings/pos',
      (data) =>
          data == null ? null : PosSettings.fromJson(data as Map<String, dynamic>),
    );
    return res.data ??
        PosSettings(maxCashierDiscountPct: 15, maxCashierDiscountAmount: 50);
  }

  /// POST api/Login — verifica credenciales de un supervisor para autorizar
  /// un descuento por encima del límite. Devuelve el token del supervisor.
  Future<LoginResponse> supervisorLogin(String email, String password) async {
    final res = await _api.post<LoginResponse?>(
      'api/Login',
      (data) =>
          data == null ? null : LoginResponse.fromJson(data as Map<String, dynamic>),
      body: LoginRequest(email: email, password: password).toJson(),
    );
    final data = res.data;
    if (data == null || data.token.isEmpty) {
      throw ApiException(
        res.message.description.isNotEmpty
            ? res.message.description
            : 'Credenciales incorrectas.',
      );
    }
    return data;
  }
}
