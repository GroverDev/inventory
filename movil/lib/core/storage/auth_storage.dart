import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'theme_storage.dart';

/// Persiste el token JWT y datos básicos del usuario de forma segura.
class AuthStorage {
  static const _storage = FlutterSecureStorage();
  static const _kToken = 'auth_token';
  static const _kRefreshToken = 'auth_refresh_token';
  static const _kUserName = 'auth_user_name';
  static const _kRolName = 'auth_rol_name';
  static const _kUserId = 'auth_user_id';
  static const _kAccessMenu = 'auth_access_menu';

  /// Token de "dispositivo de confianza" (saltar el TOTP en el próximo
  /// login). Va atado al usuario que lo emitió, así que sobrevivir a
  /// [clear] es seguro: si otro usuario inicia sesión en el mismo
  /// dispositivo, el backend lo rechaza por no coincidir su UserId.
  static const _kDeviceTrustToken = 'auth_device_trust_token';

  Future<void> save({
    required String token,
    required String userName,
    required String rolName,
    required int userId,
    String refreshToken = '',
  }) async {
    await _storage.write(key: _kToken, value: token);
    await _storage.write(key: _kUserName, value: userName);
    await _storage.write(key: _kRolName, value: rolName);
    await _storage.write(key: _kUserId, value: userId.toString());
    // El backend solo lo envía en el login inicial y en cada rotación; si
    // llega vacío se conserva el que ya estaba guardado.
    if (refreshToken.isNotEmpty) {
      await _storage.write(key: _kRefreshToken, value: refreshToken);
    }
  }

  /// Guarda el par de tokens tras una rotación exitosa.
  Future<void> saveTokens(String token, String refreshToken) async {
    await _storage.write(key: _kToken, value: token);
    if (refreshToken.isNotEmpty) {
      await _storage.write(key: _kRefreshToken, value: refreshToken);
    }
  }

  /// Menú de accesos (con permisos) serializado en JSON. Se cachea para que
  /// al reabrir la app los permisos estén disponibles antes de la red.
  Future<void> saveAccessMenu(String json) =>
      _storage.write(key: _kAccessMenu, value: json);

  Future<String?> readAccessMenu() => _storage.read(key: _kAccessMenu);

  Future<String?> readToken() => _storage.read(key: _kToken);
  Future<String?> readRefreshToken() => _storage.read(key: _kRefreshToken);
  Future<String?> readUserName() => _storage.read(key: _kUserName);
  Future<String?> readRolName() => _storage.read(key: _kRolName);

  /// Se manda en cada `LoginRequest`; si el backend lo reconoce, salta el
  /// paso de TOTP. Equivalente móvil de la cookie `device_trust` de la web.
  Future<void> saveDeviceTrustToken(String token) =>
      _storage.write(key: _kDeviceTrustToken, value: token);

  Future<String?> readDeviceTrustToken() =>
      _storage.read(key: _kDeviceTrustToken);

  Future<int> readUserId() async {
    final v = await _storage.read(key: _kUserId);
    return int.tryParse(v ?? '') ?? 0;
  }

  /// Borra todo lo de la sesión (token, usuario, menú de accesos y el PIN del
  /// POS). Se conservan la preferencia de tema y el token de dispositivo de
  /// confianza, que son del dispositivo y no del usuario que cierra sesión
  /// (mismo criterio que la cookie `device_trust` en la web, que tampoco se
  /// borra al hacer logout).
  Future<void> clear() async {
    final theme = await _storage.read(key: ThemeStorage.key);
    final deviceTrustToken = await _storage.read(key: _kDeviceTrustToken);
    await _storage.deleteAll();
    if (theme != null) {
      await _storage.write(key: ThemeStorage.key, value: theme);
    }
    if (deviceTrustToken != null) {
      await _storage.write(key: _kDeviceTrustToken, value: deviceTrustToken);
    }
  }
}
