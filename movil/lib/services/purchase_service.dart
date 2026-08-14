import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
import '../models/purchase.dart';

/// Desenlace de una recepción enviada al servidor.
///
/// El reintento idempotente no es un error: significa que la mercadería ya
/// entró. Merece un mensaje distinto al del rechazo, no una excepción.
enum PurchaseReceiptOutcome {
  /// La recepción se registró en este envío.
  applied,

  /// El `OperationUid` ya estaba usado: la recepción existía de antes.
  alreadyRegistered,
}

class PurchaseReceiptResult {
  final PurchaseReceiptOutcome outcome;
  final String message;

  PurchaseReceiptResult(this.outcome, this.message);
}

class PurchaseService {
  PurchaseService(this._api);
  final ApiClient _api;

  /// GET api/Purchases — lista de pedidos.
  ///
  /// Los tres filtros son obligatorios en el backend: la consulta hace
  /// `purchase_date BETWEEN` y `purchase_status_id = @PurchaseStatusId`, sin
  /// comodín. Sin ellos la petición no devuelve nada. Es el mismo trío que
  /// manda la webapp.
  Future<List<PurchaseSummary>> list({
    required DateTime from,
    required DateTime to,
    required int statusId,
  }) async {
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
      query: {
        'purchaseDateInitial': apiDateFormat.format(from),
        'purchaseDateEnd': apiDateFormat.format(to),
        'purchaseStatus': statusId,
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

  /// GET api/Purchases/{id} — pedido con su detalle y los saldos por recibir.
  Future<PurchaseOrder> getById(String id) async {
    final res = await _api.get<PurchaseOrder?>(
      'api/Purchases/$id',
      (data) => data is Map<String, dynamic>
          ? PurchaseOrder.fromJson(data)
          : null,
    );
    final order = res.data;
    if (order == null || order.id.isEmpty) {
      throw ApiException(_reason(res, 'No se encontró el pedido.'));
    }
    return order;
  }

  /// PUT api/Purchases/reciveOrders/{id} — registra una recepción.
  ///
  /// El servidor revalida todo contra los saldos reales dentro de la
  /// transacción, así que un rechazo acá es la última palabra.
  Future<PurchaseReceiptResult> receive(PurchaseDelivery delivery) async {
    final res = await _api.put<bool>(
      'api/Purchases/reciveOrders/${delivery.purchaseId}',
      (data) => data == true,
      body: delivery.toJson(),
    );

    if (res.ok) {
      return PurchaseReceiptResult(
        PurchaseReceiptOutcome.applied,
        res.message.description,
      );
    }

    // Reintento de una recepción ya aplicada: el uid chocó contra el índice
    // único y el backend responde con MessageType `info`. El ApiClient solo
    // lanza ante `error`, así que sin esto el caso pasaría mudo.
    if (res.message.messageType.toLowerCase() == 'info') {
      return PurchaseReceiptResult(
        PurchaseReceiptOutcome.alreadyRegistered,
        res.message.description.isNotEmpty
            ? res.message.description
            : 'Esta recepción ya fue registrada.',
      );
    }

    throw ApiException(_reason(res, 'No se pudo registrar la recepción.'));
  }

  /// PUT api/Purchases/close/{id} — cierra con faltante una orden que el
  /// proveedor no completará. No mueve stock: solo impide nuevas recepciones.
  Future<String> close(String purchaseId) async {
    final res = await _api.put<bool>(
      'api/Purchases/close/$purchaseId',
      (data) => data == true,
      body: const <String, dynamic>{},
    );
    if (!res.ok) {
      throw ApiException(_reason(res, 'No se pudo cerrar la orden.'));
    }
    return res.message.description;
  }

  /// Las reglas de negocio del backend responden `ok:false` con MessageType
  /// `warning`, que el ApiClient no convierte en excepción. Sin rescatar el
  /// texto acá, el motivo del rechazo se perdería.
  String _reason(ApiResponse<dynamic> res, String fallback) =>
      res.message.description.isNotEmpty ? res.message.description : fallback;
}
