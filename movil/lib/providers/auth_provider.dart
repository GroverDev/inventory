import 'package:flutter/foundation.dart';

import '../core/navigation/navigator_key.dart';
import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
import '../core/storage/auth_storage.dart';
import '../models/login_models.dart';
import '../services/auth_service.dart';

enum AuthStatus {
  unknown,
  authenticated,
  unauthenticated,

  /// 2FA ya configurado: el usuario debe ingresar el código TOTP.
  totpRequired,

  /// 2FA obligatorio pero sin configurar: el usuario debe configurarlo.
  totpSetupRequired,
}

class AuthProvider extends ChangeNotifier {
  AuthProvider(this._authService, this._storage, this._api) {
    // Si el cliente HTTP detecta 401, cerramos sesión.
    _api.onUnauthorized = logout;
  }

  final AuthService _authService;
  final AuthStorage _storage;
  final ApiClient _api;

  AuthStatus status = AuthStatus.unknown;
  String userName = '';
  String rolName = '';
  bool loading = false;
  String? error;

  /// Token temporal del flujo 2FA (no es el JWT real).
  String _totpSessionToken = '';

  /// Llamado al iniciar la app para restaurar sesión.
  Future<void> bootstrap() async {
    final token = await _storage.readToken();
    if (token != null && token.isNotEmpty) {
      userName = await _storage.readUserName() ?? '';
      rolName = await _storage.readRolName() ?? '';
      status = AuthStatus.authenticated;
    } else {
      status = AuthStatus.unauthenticated;
    }
    notifyListeners();
  }

  Future<bool> login(String email, String password) async {
    loading = true;
    error = null;
    notifyListeners();
    try {
      final res = await _authService.login(email, password);

      // Caso 1: 2FA ya configurado → aún NO hay token real, verificar código.
      if (res.requireTotp) {
        _totpSessionToken = res.totpSessionToken;
        status = AuthStatus.totpRequired;
        return false;
      }

      if (res.token.isEmpty) {
        error = 'No se recibió un token válido.';
        return false;
      }

      // El token real ya vino: lo persistimos (necesario para Mfa/setup).
      await _persist(res);

      // Caso 2: 2FA obligatorio pero sin configurar → forzar configuración.
      if (res.totpSetupRequired) {
        status = AuthStatus.totpSetupRequired;
        return false;
      }

      status = AuthStatus.authenticated;
      return true;
    } on ApiException catch (e) {
      error = e.message;
      return false;
    } catch (_) {
      error = 'Error inesperado al iniciar sesión.';
      return false;
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  /// Verifica el código TOTP de 6 dígitos durante el login.
  Future<bool> verifyTotp(String code) =>
      _completeWith(() => _authService.verifyTotp(_totpSessionToken, code),
          'Error al verificar el código.');

  /// Verifica con un código de recuperación durante el login.
  Future<bool> verifyRecovery(String recoveryCode) => _completeWith(
      () => _authService.verifyRecovery(_totpSessionToken, recoveryCode),
      'Error al verificar el código de recuperación.');

  Future<bool> _completeWith(
      Future<LoginResponse> Function() action, String fallbackError) async {
    loading = true;
    error = null;
    notifyListeners();
    try {
      final res = await action();
      await _persist(res);
      _totpSessionToken = '';
      status = AuthStatus.authenticated;
      return true;
    } on ApiException catch (e) {
      error = e.message;
      return false;
    } catch (_) {
      error = fallbackError;
      return false;
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  /// Obtiene el QR + clave secreta para configurar 2FA.
  Future<TotpSetupData?> startTotpSetup() async {
    error = null;
    try {
      return await _authService.setupTotp();
    } on ApiException catch (e) {
      error = e.message;
      notifyListeners();
      return null;
    } catch (_) {
      error = 'No se pudo iniciar la configuración 2FA.';
      notifyListeners();
      return null;
    }
  }

  /// Confirma el código y activa 2FA. Devuelve los códigos de recuperación.
  /// NO cambia el estado: la pantalla debe mostrar los códigos y luego llamar
  /// a [finishTotpSetup] para entrar.
  Future<List<String>?> enableTotp(String code) async {
    loading = true;
    error = null;
    notifyListeners();
    try {
      return await _authService.enableTotp(code);
    } on ApiException catch (e) {
      error = e.message;
      return null;
    } catch (_) {
      error = 'No se pudo activar 2FA.';
      return null;
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  /// Completa la configuración 2FA tras mostrar los códigos de recuperación.
  void finishTotpSetup() {
    status = AuthStatus.authenticated;
    notifyListeners();
  }

  Future<void> _persist(LoginResponse res) async {
    final displayName = res.fullName.isNotEmpty ? res.fullName : res.userName;
    await _storage.save(
      token: res.token,
      userName: displayName,
      rolName: res.rolName,
      userId: res.userId,
      refreshToken: res.refreshToken,
    );
    userName = displayName;
    rolName = res.rolName;
  }

  Future<void> logout() async {
    // Se revoca antes de limpiar: así la sesión muere también en el servidor
    // y no queda un refresh token vivo por 30 días.
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken != null && refreshToken.isNotEmpty) {
      await _authService.revokeRefreshToken(refreshToken);
    }

    await _storage.clear();
    _totpSessionToken = '';
    userName = '';
    rolName = '';
    error = null;
    status = AuthStatus.unauthenticated;
    notifyListeners();

    // Cierra cualquier pantalla apilada (Productos, POS, etc.) para volver
    // a la raíz, donde `_Root` ya muestra LoginScreen por el cambio de status.
    navigatorKey.currentState?.popUntil((route) => route.isFirst);
  }
}
