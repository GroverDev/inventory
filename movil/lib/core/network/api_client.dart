import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../storage/auth_storage.dart';
import 'api_response.dart';

/// Cliente HTTP central. Adjunta el Bearer token, normaliza errores y expone
/// helpers que devuelven el `ApiResponse<T>` del backend.
class ApiClient {
  ApiClient(this._storage) {
    _dio = Dio(
      BaseOptions(
        baseUrl: AppConfig.apiBaseUrl,
        connectTimeout: const Duration(seconds: 20),
        receiveTimeout: const Duration(seconds: 30),
        headers: {'Content-Type': 'application/json'},
        // Aceptamos cualquier status para mapear el error nosotros mismos.
        validateStatus: (s) => s != null && s < 500,
      ),
    );

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _storage.readToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
      ),
    );
  }

  final AuthStorage _storage;
  late final Dio _dio;

  /// Notificado cuando una llamada devuelve 401 (token vencido/ inválido).
  void Function()? onUnauthorized;

  Future<ApiResponse<T>> get<T>(
    String path,
    T Function(dynamic data) parse, {
    Map<String, dynamic>? query,
  }) =>
      _send<T>(() => _dio.get(path, queryParameters: query), parse);

  Future<ApiResponse<T>> post<T>(
    String path,
    T Function(dynamic data) parse, {
    Object? body,
  }) =>
      _send<T>(() => _dio.post(path, data: body), parse);

  Future<ApiResponse<T>> put<T>(
    String path,
    T Function(dynamic data) parse, {
    Object? body,
  }) =>
      _send<T>(() => _dio.put(path, data: body), parse);

  Future<ApiResponse<T>> delete<T>(
    String path,
    T Function(dynamic data) parse,
  ) =>
      _send<T>(() => _dio.delete(path), parse);

  Future<ApiResponse<T>> _send<T>(
    Future<Response> Function() call,
    T Function(dynamic data) parse,
  ) async {
    try {
      final res = await call();

      if (res.statusCode == 401) {
        onUnauthorized?.call();
        throw ApiException('Sesión expirada. Inicia sesión nuevamente.');
      }

      final body = res.data;
      if (body is Map<String, dynamic>) {
        final parsed = ApiResponse<T>.fromJson(body, parse);
        if (!parsed.ok && parsed.message.isError) {
          throw ApiException(
            parsed.message.description.isNotEmpty
                ? parsed.message.description
                : 'Ocurrió un error en el servidor.',
          );
        }
        return parsed;
      }

      // Respuestas que no envuelven en Response<T> (texto plano, etc.)
      if (res.statusCode != null && res.statusCode! >= 400) {
        throw ApiException(_extractError(body));
      }
      throw ApiException('Respuesta inesperada del servidor.');
    } on DioException catch (e) {
      if (e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.connectionError) {
        throw ApiException('No se pudo conectar con el servidor.');
      }
      throw ApiException(_extractError(e.response?.data));
    }
  }

  String _extractError(dynamic data) {
    if (data is Map && data['Message'] is Map) {
      final d = data['Message']['Description'];
      if (d is String && d.isNotEmpty) return d;
    }
    if (data is String && data.isNotEmpty) return data;
    return 'Ocurrió un error inesperado.';
  }
}
