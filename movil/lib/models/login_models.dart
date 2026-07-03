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

  LoginRequest({
    required this.email,
    required this.password,
    this.userName = '',
    this.device = '',
    this.withEmail = true,
    this.loginFrom = 5,
    this.loginWith = 1,
  });

  Map<String, dynamic> toJson() => {
        'UserName': userName,
        'Email': email,
        'Password': password,
        'Device': device,
        'WithEmail': withEmail,
        'LoginFrom': loginFrom,
        'LoginWith': loginWith,
      };
}

class LoginResponse {
  final int userId;
  final String fullName;
  final String userName;
  final String email;
  final String token;

  /// 2FA ya configurado: aún NO hay token real, hay que verificar un código.
  final bool requireTotp;

  /// 2FA obligatorio pero sin configurar: el token real ya vino, pero el
  /// usuario debe configurar el 2FA antes de operar.
  final bool totpSetupRequired;

  /// Token temporal usado para verificar el 2FA (no es el JWT real).
  final String totpSessionToken;

  final int rolId;
  final String rolName;
  final bool changePassword;

  LoginResponse({
    required this.userId,
    required this.fullName,
    required this.userName,
    required this.email,
    required this.token,
    required this.requireTotp,
    required this.totpSetupRequired,
    required this.totpSessionToken,
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
        requireTotp: j['RequireTotp'] ?? false,
        totpSetupRequired: j['TotpSetupRequired'] ?? false,
        totpSessionToken: j['TotpSessionToken'] ?? '',
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
