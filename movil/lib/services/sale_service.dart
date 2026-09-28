import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
import '../models/cash_session.dart';
import '../models/login_models.dart';
import '../models/sale.dart';
import '../models/sale_history.dart';

/// Id con el que el servidor pide la firma de un supervisor por falta de
/// stock, en vez de rechazar la venta (espejo de
/// `StockSupervisorRequiredException.MessageId` en el backend y de
/// `REQUIRES_SUPERVISOR_STOCK` en `useSales.ts`).
const String kRequiresSupervisorStock = 'requires-supervisor-stock';

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

  /// GET api/CashSession?dateFrom=&dateTo= — sesiones de caja del rango, de la
  /// más reciente a la más vieja. El servidor decide el alcance: si el usuario
  /// es solo Cajero le devuelve únicamente las suyas.
  /// Las fechas van en `yyyy-MM-dd`.
  Future<List<CashSession>> cashSessions({
    required String dateFrom,
    required String dateTo,
  }) async {
    final res = await _api.get<List<CashSession>>(
      'api/CashSession',
      (data) => ((data as List?) ?? [])
          .map((e) => CashSession.fromJson(e as Map<String, dynamic>))
          .toList(),
      query: {'dateFrom': dateFrom, 'dateTo': dateTo},
    );
    return res.data ?? const [];
  }

  /// GET api/CashSession/{id}/sales — las ventas de esa sesión, de la primera a
  /// la última. Se filtra por `cash_session_id`, no por fecha: un turno que
  /// cruza la medianoche, o dos sesiones del mismo cajero en el mismo día,
  /// siguen quedando separados.
  ///
  /// La API no pagina esta lista (son las ventas de un solo turno) y devuelve
  /// cada venta con su detalle y sus cobros; acá alcanza con la cabecera.
  Future<List<SaleSummary>> sessionSales(String sessionId) async {
    final res = await _api.get<List<SaleSummary>>(
      'api/CashSession/$sessionId/sales',
      (data) => ((data as List?) ?? [])
          .map((e) => SaleSummary.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return res.data ?? const [];
  }

  /// POST api/CashSession/open
  Future<void> openSession(double openingAmount) async {
    await _api.post<String>(
      'api/CashSession/open',
      (data) => data?.toString() ?? '',
      body: {'OpeningAmount': openingAmount},
    );
  }

  /// PUT api/CashSession/{id}/close — arqueo de caja, medio por medio y a
  /// ciegas (se declara sin ver lo esperado; el servidor compara y devuelve
  /// la diferencia). [counts] va por id de método de pago.
  ///
  /// Lanza [ApiException] si el cierre se rechaza; su `data` trae el
  /// `CloseRequires` ('note' | 'supervisor') cuando el rechazo es uno de esos
  /// dos casos previstos, no un error real (ver `CashSessionApplication.Rechazo`).
  Future<CashSession> closeSession(
    String sessionId, {
    required Map<String, double> counts,
    List<DenominationCount> denominations = const [],
    String notes = '',
    String? supervisorAuthToken,
  }) async {
    final res = await _api.put<CashSession?>(
      'api/CashSession/$sessionId/close',
      (data) =>
          data == null ? null : CashSession.fromJson(data as Map<String, dynamic>),
      body: {
        // Solo lo usan clientes anteriores al arqueo por medio; con Counts
        // presente el servidor lo ignora.
        'DeclaredAmount': 0,
        'Notes': notes,
        'Counts': counts.entries
            .map((e) => {'PaymentMethodId': e.key, 'Declared': e.value})
            .toList(),
        'Denominations': denominations.map((d) => d.toJson()).toList(),
        if (supervisorAuthToken != null && supervisorAuthToken.isNotEmpty)
          'SupervisorAuthToken': supervisorAuthToken,
      },
    );
    if (res.data == null) throw ApiException('No se pudo cerrar la caja.');
    return res.data!;
  }

  /// GET api/Settings/cash-close — configuración vigente del cierre (puede
  /// haber cambiado desde la última vez: se pide cada vez que se abre la
  /// pantalla de cierre, igual que la web).
  Future<CashCloseSettings> closeSettings() async {
    final res = await _api.get<CashCloseSettings?>(
      'api/Settings/cash-close',
      (data) => data == null
          ? null
          : CashCloseSettings.fromJson(data as Map<String, dynamic>),
    );
    return res.data ?? const CashCloseSettings();
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
