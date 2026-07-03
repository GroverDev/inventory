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
  ApiException(this.message);
  @override
  String toString() => message;
}
