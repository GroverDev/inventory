/// Espejo del `Response<T>` del backend.
///
/// El backend serializa con PascalCase (PropertyNamingPolicy = null), por eso
/// las claves son `ok`, `Data`, `Message`, etc.
class ApiMessage {
  final String description;
  final String messageType;
  final String id;

  ApiMessage({this.description = '', this.messageType = 'nothing', this.id = '0'});

  factory ApiMessage.fromJson(Map<String, dynamic>? json) {
    if (json == null) return ApiMessage();
    return ApiMessage(
      description: json['Description'] ?? '',
      messageType: json['MessageType'] ?? 'nothing',
      id: (json['Id'] ?? '0').toString(),
    );
  }

  bool get isError => messageType.toLowerCase() == 'error';
}

class ApiResponse<T> {
  final bool ok;
  final T? data;
  final ApiMessage message;

  /// Solo presentes en respuestas paginadas.
  final int totalCount;
  final int page;
  final int pageSize;

  ApiResponse({
    required this.ok,
    required this.data,
    required this.message,
    this.totalCount = 0,
    this.page = 1,
    this.pageSize = 0,
  });

  /// [parse] transforma el nodo `Data` en el tipo deseado.
  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(dynamic data) parse,
  ) {
    return ApiResponse<T>(
      ok: json['ok'] == true,
      data: json['Data'] == null ? null : parse(json['Data']),
      message: ApiMessage.fromJson(json['Message'] as Map<String, dynamic>?),
      totalCount: json['TotalCount'] ?? 0,
      page: json['Page'] ?? 1,
      pageSize: json['PageSize'] ?? 0,
    );
  }
}

/// Excepción de dominio para errores de API ya formateados para el usuario.
class ApiException implements Exception {
  final String message;

  /// `error`, `warning`, `info`… tal como lo mandó el backend. Sirve para que
  /// un servicio distinga un caso previsto (por ejemplo, una operación
  /// idempotente que ya se había aplicado) de una falla real.
  final String messageType;

  ApiException(this.message, {this.messageType = 'error'});

  bool get isInfo => messageType.toLowerCase() == 'info';

  @override
  String toString() => message;
}

/// Saca un texto presentable de un cuerpo de error.
///
/// Contempla el sobre del backend y también las respuestas que NO vienen
/// envueltas: la validación de modelo de ASP.NET responde un ProblemDetails con
/// el detalle en `errors`, y sin leerlo el usuario solo veía "error inesperado".
String extractApiError(dynamic data) {
  if (data is Map) {
    if (data['Message'] is Map) {
      final d = data['Message']['Description'];
      if (d is String && d.isNotEmpty) return d;
    }

    final errors = data['errors'];
    if (errors is Map && errors.isNotEmpty) {
      final primero = errors.values.first;
      if (primero is List && primero.isNotEmpty) return primero.first.toString();
      if (primero is String && primero.isNotEmpty) return primero;
    }

    final title = data['title'];
    if (title is String && title.isNotEmpty) return title;
  }
  if (data is String && data.isNotEmpty) return data;
  return 'Ocurrió un error inesperado.';
}

/// Traduce una respuesta del backend en excepción cuando la operación no
/// prosperó. Devuelve `null` si salió bien.
///
/// **Cualquier** `ok:false` es un rechazo, no solo los de `MessageType: error`.
/// Antes se lanzaba únicamente ante `error`, así que los rechazos de regla de
/// negocio —que el backend responde como `warning`— volvían como si todo
/// hubiera salido bien: la pantalla se quedaba muda y el usuario creía que su
/// acción se había guardado.
///
/// Vive suelta y no dentro del cliente para poder probar la regla sin levantar
/// HTTP: es la que decide si el usuario se entera o no de lo que pasó.
ApiException? apiFailure(bool ok, ApiMessage message, dynamic rawBody) {
  if (ok) return null;

  return ApiException(
    message.description.isNotEmpty
        ? message.description
        : extractApiError(rawBody),
    messageType: message.messageType,
  );
}
