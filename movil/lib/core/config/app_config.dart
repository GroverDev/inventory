/// Configuración global de la app.
///
/// La URL base del backend se inyecta en tiempo de compilación con:
///   flutter run --dart-define=API_URL=https://tu-api.com/
/// Si no se define, usa el valor por defecto (emulador Android -> host).
class AppConfig {
  /// URL base del backend .NET. Debe terminar en `/`.
  /// 10.0.2.2 es el alias del host desde el emulador Android.
  static const String apiBaseUrl = String.fromEnvironment(
    'API_URL',
    //defaultValue: 'http://10.0.2.2:6001/',
    defaultValue: 'https://api.ideanueva.com/',
  );

  static const String appName = 'Inventario Móvil';

  /// Origen que se envía al backend (campo Device / LoginFrom).
  static const String deviceName = 'mobile';

  /// Valores del enum InicioSesionDesde del backend. Determinan que el
  /// servidor entregue refresh token y un access token de vida corta.
  static const int loginFromMovil = 2;
  static const int loginFromReconexionMovil = 4;
}
