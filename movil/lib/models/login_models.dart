import '../core/config/app_config.dart';

/// Espejo de LoginRequest / LoginResponse del backend.
///
/// El payload se mantiene alineado con el de la web (`frontend`) para que el
/// backend resuelva el login (y el flujo TOTP) de forma idéntica.
class LoginRequest {
  final String userName;
  final String email;
  final String password;
  final String device;
  final bool withEmail;
  final int loginFrom;
  final int loginWith;

  /// Token de dispositivo de confianza guardado de una verificación TOTP
  /// anterior con "recordar este dispositivo". Vacío si no hay uno guardado
  /// o si es para otro usuario; el backend lo valida contra el usuario que
  /// recién autenticó y lo ignora si no coincide.
  final String deviceTrustToken;

  LoginRequest({
    required this.email,
    required this.password,
    this.userName = '',
    this.device = AppConfig.deviceName,
    this.withEmail = true,
    this.loginFrom = AppConfig.loginFromMovil,
    this.loginWith = 1,
    this.deviceTrustToken = '',
  });

  Map<String, dynamic> toJson() => {
        'UserName': userName,
        'Email': email,
        'Password': password,
        'Device': device,
        'WithEmail': withEmail,
        'LoginFrom': loginFrom,
        'LoginWith': loginWith,
        'DeviceTrustToken': deviceTrustToken,
      };
}

class LoginResponse {
  final int userId;
  final String fullName;
  final String userName;
  final String email;
  final String token;

  /// Sostiene la sesión larga. Solo llega en el login inicial y en cada
  /// rotación; vacío significa "conservar el que ya está guardado".
  final String refreshToken;

  /// 2FA ya configurado: aún NO hay token real, hay que verificar un código.
  final bool requireTotp;

  /// 2FA obligatorio pero sin configurar: el token real ya vino, pero el
  /// usuario debe configurar el 2FA antes de operar.
  final bool totpSetupRequired;

  /// Token temporal usado para verificar el 2FA (no es el JWT real).
  final String totpSessionToken;

  /// Nuevo token de dispositivo de confianza, emitido solo cuando se
  /// verificó el TOTP marcando "recordar este dispositivo". Vacío en
  /// cualquier otro caso, incluido el login que saltó el TOTP por ya tener
  /// uno vigente (ese no se renueva).
  final String deviceTrustToken;

  final int rolId;
  final String rolName;
  final bool changePassword;

  LoginResponse({
    required this.userId,
    required this.fullName,
    required this.userName,
    required this.email,
    required this.token,
    required this.refreshToken,
    required this.requireTotp,
    required this.totpSetupRequired,
    required this.totpSessionToken,
    this.deviceTrustToken = '',
    required this.rolId,
    required this.rolName,
    required this.changePassword,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> j) => LoginResponse(
        userId: j['UserId'] ?? 0,
        fullName: j['FullName'] ?? '',
        userName: j['UserName'] ?? '',
        email: j['Email'] ?? '',
        token: j['Token'] ?? '',
        refreshToken: j['RefreshToken'] ?? '',
        requireTotp: j['RequireTotp'] ?? false,
        totpSetupRequired: j['TotpSetupRequired'] ?? false,
        totpSessionToken: j['TotpSessionToken'] ?? '',
        deviceTrustToken: j['DeviceTrustToken'] ?? '',
        rolId: j['RolId'] ?? 0,
        rolName: j['RolName'] ?? '',
        changePassword: j['ChangePassword'] ?? false,
      );
}

/// Datos para configurar 2FA: QR (PNG en base64) y la clave secreta manual.
class TotpSetupData {
  final String qrCodeBase64;
  final String secretKey;

  TotpSetupData({required this.qrCodeBase64, required this.secretKey});

  factory TotpSetupData.fromJson(Map<String, dynamic> j) => TotpSetupData(
        qrCodeBase64: j['QrCodeBase64'] ?? '',
        secretKey: j['SecretKey'] ?? '',
      );
}

/// Respuesta al activar 2FA: códigos de recuperación de un solo uso.
class MfaEnableData {
  final List<String> recoveryCodes;

  MfaEnableData({required this.recoveryCodes});

  factory MfaEnableData.fromJson(Map<String, dynamic> j) => MfaEnableData(
        recoveryCodes: (j['RecoveryCodes'] as List?)
                ?.map((e) => e.toString())
                .toList() ??
            const [],
      );
}
