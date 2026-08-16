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

  /// Cliente aparte para renovar la sesión: no lleva el interceptor de auth,
  /// así que el token vencido no interfiere y no puede reentrar en el flujo
  /// de refresco.
  Dio get _refreshDio => Dio(BaseOptions(
        baseUrl: AppConfig.apiBaseUrl,
        connectTimeout: const Duration(seconds: 20),
        receiveTimeout: const Duration(seconds: 30),
        headers: {'Content-Type': 'application/json'},
        validateStatus: (s) => s != null && s < 500,
      ));

  /// Refresco en curso. Si varias peticiones fallan a la vez, todas esperan
  /// al mismo intento en lugar de rotar el token varias veces (lo que
  /// dispararía la detección de reuso del backend y cerraría la sesión).
  Future<bool>? _refreshing;

  /// Notificado cuando la sesión ya no puede recuperarse.
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
    T Function(dynamic data) parse, {
    bool allowRefresh = true,
  }) async {
    try {
      final res = await call();

      if (res.statusCode == 401) {
        // El access token dura poco a propósito: se renueva con el refresh
        // token y se reintenta la llamada sin que el usuario se entere.
        if (allowRefresh && await _tryRefresh()) {
          return _send<T>(call, parse, allowRefresh: false);
        }
        onUnauthorized?.call();
        throw ApiException('Sesión expirada. Inicia sesión nuevamente.');
      }

      final body = res.data;
      if (body is Map<String, dynamic>) {
        final parsed = ApiResponse<T>.fromJson(body, parse);

        // Cualquier ok:false corta acá, no solo los MessageType `error`. Un
        // servicio que necesite distinguir un caso previsto (una operación
        // idempotente ya aplicada, por ejemplo) atrapa la excepción y mira su
        // messageType; lo que no puede pasar es que nadie se entere.
        final falla = apiFailure(parsed.ok, parsed.message, body);
        if (falla != null) throw falla;

        return parsed;
      }

      // Respuestas que no envuelven en Response<T> (texto plano, etc.)
      if (res.statusCode != null && res.statusCode! >= 400) {
        throw ApiException(extractApiError(body));
      }
      throw ApiException('Respuesta inesperada del servidor.');
    } on DioException catch (e) {
      if (e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.connectionError) {
        throw ApiException('No se pudo conectar con el servidor.');
      }
      throw ApiException(extractApiError(e.response?.data));
    }
  }

  /// Renueva el par de tokens. Devuelve `false` si la sesión ya no es
  /// recuperable (refresh vencido, revocado o inexistente).
  Future<bool> _tryRefresh() {
    return _refreshing ??=
        _doRefresh().whenComplete(() => _refreshing = null);
  }

  Future<bool> _doRefresh() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) return false;

    try {
      final res = await _refreshDio.post('api/Login/refresh', data: {
        'RefreshToken': refreshToken,
        'Device': AppConfig.deviceName,
        'LoginFrom': AppConfig.loginFromReconexionMovil,
      });

      final body = res.data;
      if (body is! Map<String, dynamic> || body['ok'] != true) return false;

      final data = body['Data'];
      if (data is! Map<String, dynamic>) return false;

      final token = (data['Token'] ?? '') as String;
      if (token.isEmpty) return false;

      await _storage.saveTokens(token, (data['RefreshToken'] ?? '') as String);
      return true;
    } catch (_) {
      return false;
    }
  }

}
