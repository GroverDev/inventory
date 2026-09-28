/// Configuración global de la app.
///
/// La URL base del backend y la de medios se inyectan en tiempo de
/// compilación con:
///   flutter run --dart-define=API_URL=https://tu-api.com/ --dart-define=MEDIA_URL=https://tu-cdn.com/
/// Si no se definen, usan los valores por defecto (producción).
class AppConfig {
  /// URL base del backend .NET. Debe terminar en `/`.
  /// 10.0.2.2 es el alias del host desde el emulador Android.
  static const String apiBaseUrl = String.fromEnvironment(
    'API_URL',
    //defaultValue: 'http://10.0.2.2:6001/',
    defaultValue: 'https://api.ideanueva.com/',
  );

  /// Dominio donde sirven las imágenes de producto (mismo criterio que
  /// `VITE_MEDIA_URL` en la web, `frontend/.env.production`): la API solo
  /// devuelve la ruta relativa. Vacío = sin dominio configurado, y
  /// `mediaUrl()` devuelve '' (se muestra el marcador en su lugar).
  static const String mediaBaseUrl = String.fromEnvironment(
    'MEDIA_URL',
    defaultValue: 'https://media.ideanueva.com/',
  );

  static const String appName = 'Vendi2 PdV';

  /// Origen que se envía al backend (campo Device / LoginFrom).
  static const String deviceName = 'mobile';

  /// Valores del enum InicioSesionDesde del backend. Determinan que el
  /// servidor entregue refresh token y un access token de vida corta.
  static const int loginFromMovil = 2;
  static const int loginFromReconexionMovil = 4;
}
