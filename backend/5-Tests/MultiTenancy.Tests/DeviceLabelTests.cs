using Common.Utilities;

namespace MultiTenancy.Tests;

/// <summary>
/// Etiqueta de dispositivo a partir del User-Agent. No necesita base de datos.
/// </summary>
public class DeviceLabelTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36", "Chrome en Windows")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0", "Edge en Windows")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0", "Firefox en Windows")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 OPR/111.0.0.0", "Opera en Windows")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15", "Safari en macOS")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1", "Safari en iOS")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Mobile Safari/537.36", "Chrome en Android")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36", "Chrome en Linux")]
    public void Reconoce_navegador_y_sistema(string userAgent, string esperado) =>
        Assert.Equal(esperado, DeviceLabel.FromUserAgent(userAgent));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("curl/8.4.0")]
    public void Sin_nada_reconocible_da_una_etiqueta_generica(string? userAgent) =>
        Assert.Equal(DeviceLabel.Unknown, DeviceLabel.FromUserAgent(userAgent));

    [Fact]
    public void Solo_sistema_si_el_navegador_no_se_reconoce() =>
        Assert.Equal("Windows", DeviceLabel.FromUserAgent("MiCliente/1.0 (Windows NT 10.0)"));
}
