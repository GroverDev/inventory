import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Preferencia de tema: claro, oscuro o el del sistema.
///
/// Es una preferencia del dispositivo y no de la sesión, por eso
/// [AuthStorage.clear] la conserva al cerrar sesión.
class ThemeStorage {
  static const _storage = FlutterSecureStorage();

  /// Pública a propósito: `AuthStorage.clear()` necesita respetarla.
  static const key = 'app_theme_mode';

  Future<ThemeMode> read() async {
    switch (await _storage.read(key: key)) {
      case 'light':
        return ThemeMode.light;
      case 'dark':
        return ThemeMode.dark;
      default:
        return ThemeMode.system;
    }
  }

  Future<void> save(ThemeMode mode) =>
      _storage.write(key: key, value: mode.name);
}
