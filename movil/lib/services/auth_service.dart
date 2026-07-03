import '../core/network/api_client.dart';
import '../core/network/api_response.dart';
import '../models/login_models.dart';

class AuthService {
  AuthService(this._api);
  final ApiClient _api;

  /// POST api/Login
  Future<LoginResponse> login(String email, String password) async {
    final res = await _api.post<LoginResponse>(
      'api/Login',
      (data) => LoginResponse.fromJson(data as Map<String, dynamic>),
      body: LoginRequest(email: email, password: password).toJson(),
    );
    final data = res.data;
    if (data == null) {
      throw ApiException(
        res.message.description.isNotEmpty
            ? res.message.description
            : 'Credenciales inválidas.',
      );
    }
    return data;
  }

  /// POST api/Mfa/verify — verifica el código TOTP durante el login.
  /// Devuelve el `LoginResponse` con el JWT real.
  Future<LoginResponse> verifyTotp(String sessionToken, String code) async {
    final res = await _api.post<LoginResponse>(
      'api/Mfa/verify',
      (data) => LoginResponse.fromJson(data as Map<String, dynamic>),
      body: {'TotpSessionToken': sessionToken, 'TotpCode': code},
    );
    final data = res.data;
    if (data == null || data.token.isEmpty) {
      throw ApiException(
        res.message.description.isNotEmpty
            ? res.message.description
            : 'Código inválido.',
      );
    }
    return data;
  }

  /// POST api/Mfa/verify-recovery — verifica con un código de recuperación.
  Future<LoginResponse> verifyRecovery(
      String sessionToken, String recoveryCode) async {
    final res = await _api.post<LoginResponse>(
      'api/Mfa/verify-recovery',
      (data) => LoginResponse.fromJson(data as Map<String, dynamic>),
      body: {'TotpSessionToken': sessionToken, 'RecoveryCode': recoveryCode},
    );
    final data = res.data;
    if (data == null || data.token.isEmpty) {
      throw ApiException(
        res.message.description.isNotEmpty
            ? res.message.description
            : 'Código de recuperación inválido.',
      );
    }
    return data;
  }

  /// GET api/Mfa/setup — obtiene el QR y la clave secreta para configurar 2FA.
  /// Requiere el JWT real (que llega en el login cuando `totpSetupRequired`).
  Future<TotpSetupData> setupTotp() async {
    final res = await _api.get<TotpSetupData>(
      'api/Mfa/setup',
      (data) => TotpSetupData.fromJson(data as Map<String, dynamic>),
    );
    final data = res.data;
    if (data == null) {
      throw ApiException(
        res.message.description.isNotEmpty
            ? res.message.description
            : 'No se pudo iniciar la configuración 2FA.',
      );
    }
    return data;
  }

  /// POST api/Mfa/enable — confirma el código y activa 2FA.
  /// Devuelve los códigos de recuperación de un solo uso.
  Future<List<String>> enableTotp(String code) async {
    final res = await _api.post<MfaEnableData>(
      'api/Mfa/enable',
      (data) => MfaEnableData.fromJson(data as Map<String, dynamic>),
      body: {'Code': code},
    );
    final data = res.data;
    if (data == null) {
      throw ApiException(
        res.message.description.isNotEmpty
            ? res.message.description
            : 'No se pudo activar 2FA.',
      );
    }
    return data.recoveryCodes;
  }
}
