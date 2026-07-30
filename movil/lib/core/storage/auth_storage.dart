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

  Future<int> readUserId() async {
    final v = await _storage.read(key: _kUserId);
    return int.tryParse(v ?? '') ?? 0;
  }

  /// Borra todo lo de la sesión (token, usuario, menú de accesos y el PIN del
  /// POS). Solo se conserva la preferencia de tema, que es del dispositivo.
  Future<void> clear() async {
    final theme = await _storage.read(key: ThemeStorage.key);
    await _storage.deleteAll();
    if (theme != null) {
      await _storage.write(key: ThemeStorage.key, value: theme);
    }
  }
}
