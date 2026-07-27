import 'dart:convert';
import 'dart:math';
import 'dart:typed_data';

import 'package:crypto/crypto.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// PIN local que protege el ingreso al POS. Nunca se guarda en claro: se
/// persiste como `$pin-sha256$<iteraciones>$<salt>$<hash>`, el mismo formato
/// que usa el backend para las contraseñas.
///
/// Vive en el mismo secure storage que el token, así que `AuthStorage.clear()`
/// lo borra al cerrar sesión: cada usuario define su propio PIN.
class PinStorage {
  static const _storage = FlutterSecureStorage();
  static const _kPin = 'pos_pin';
  static const _kFailedAttempts = 'pos_pin_failed';

  /// Intentos fallidos antes de forzar el cierre de sesión completo.
  static const maxAttempts = 5;

  static const _iterations = 10000;
  static const _saltSize = 16;

  Future<bool> hasPin() async {
    final stored = await _storage.read(key: _kPin);
    return stored != null && stored.isNotEmpty;
  }

  Future<void> setPin(String pin) async {
    final salt = _randomSalt();
    final hash = _hash(pin, salt, _iterations);
    final encoded =
        '\$pin-sha256\$$_iterations\$${base64Encode(salt)}\$$hash';
    await _storage.write(key: _kPin, value: encoded);
    await _resetAttempts();
  }

  /// Devuelve `true` si el PIN coincide. Lleva la cuenta de intentos fallidos.
  Future<bool> verifyPin(String pin) async {
    final stored = await _storage.read(key: _kPin);
    if (stored == null || stored.isEmpty) return false;

    final parts = stored.split('\$');
    // ['', 'pin-sha256', iteraciones, salt, hash]
    if (parts.length != 5 || parts[1] != 'pin-sha256') return false;

    final iterations = int.tryParse(parts[2]) ?? _iterations;
    final salt = base64Decode(parts[3]);
    final ok = _hash(pin, salt, iterations) == parts[4];

    if (ok) {
      await _resetAttempts();
    } else {
      await _storage.write(
          key: _kFailedAttempts, value: '${await failedAttempts() + 1}');
    }
    return ok;
  }

  Future<int> failedAttempts() async {
    final v = await _storage.read(key: _kFailedAttempts);
    return int.tryParse(v ?? '') ?? 0;
  }

  Future<void> _resetAttempts() => _storage.delete(key: _kFailedAttempts);

  Uint8List _randomSalt() {
    final rnd = Random.secure();
    return Uint8List.fromList(
        List<int>.generate(_saltSize, (_) => rnd.nextInt(256)));
  }

  String _hash(String pin, List<int> salt, int iterations) {
    var digest = sha256.convert([...salt, ...utf8.encode(pin)]);
    for (var i = 1; i < iterations; i++) {
      digest = sha256.convert([...salt, ...digest.bytes]);
    }
    return base64Encode(digest.bytes);
  }
}
