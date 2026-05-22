namespace Seguridad.Domain;

public class TotpSetupResponse
{
    public string SecretKey { get; set; } = "";
    public string QrCodeUri { get; set; } = "";
    public string QrCodeBase64 { get; set; } = "";
}
