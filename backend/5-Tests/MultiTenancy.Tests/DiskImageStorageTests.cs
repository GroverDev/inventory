using Common.Utilities.Exceptions;
using Common.Utilities.MultiTenancy;
using Inventory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace MultiTenancy.Tests;

/// <summary>
/// Imágenes de productos en disco. No necesitan base: el almacenamiento solo
/// depende del tenant del request y de un directorio.
/// </summary>
public sealed class DiskImageStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "img-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private DiskImageStorage Storage(int tenantId = 7)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(tenantId);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Media:RootPath"] = _root })
            .Build();
        return new DiskImageStorage(config, tenant, NullLogger<DiskImageStorage>.Instance);
    }

    private static byte[] Encoded(int width, int height, SKEncodedImageFormat format)
    {
        using var bmp = new SKBitmap(width, height);
        bmp.Erase(new SKColor(200, 30, 30));
        using var image = SKImage.FromBitmap(bmp);
        return image.Encode(format, 90).ToArray();
    }

    private static MemoryStream Png(int width, int height) => new(Encoded(width, height, SKEncodedImageFormat.Png));

    private (int Width, int Height) SizeOf(string relativePath)
    {
        using var bmp = SKBitmap.Decode(Path.Combine(_root, relativePath));
        return (bmp.Width, bmp.Height);
    }

    /// <summary>JPEG de 40x20 al que se le inyecta EXIF con Orientation = 6 (girar 90° horario).</summary>
    private static MemoryStream JpegConOrientacion6()
    {
        var jpeg = Encoded(40, 20, SKEncodedImageFormat.Jpeg);
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,                          // APP1, longitud 34
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,              // "Exif\0\0"
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,  // TIFF little-endian, IFD en el offset 8
            0x01, 0x00,                                      // una entrada
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, // Orientation (0x0112) = 6
            0x00, 0x00, 0x00, 0x00                           // sin más IFDs
        ];
        var ms = new MemoryStream();
        ms.Write(jpeg, 0, 2);          // SOI
        ms.Write(exif);
        ms.Write(jpeg, 2, jpeg.Length - 2);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task Guarda_imagen_y_miniatura_en_la_carpeta_del_tenant_y_producto()
    {
        var productId = Guid.NewGuid();

        var path = await Storage().SaveProductImageAsync(productId, Png(1600, 1200));

        Assert.StartsWith($"7/products/{productId}/", path);
        Assert.EndsWith(".webp", path);

        // el lado mayor baja al tope conservando la proporción
        Assert.Equal((800, 600), SizeOf(path));
        Assert.Equal((200, 150), SizeOf(IImageStorage.ThumbPathOf(path)));
    }

    [Fact]
    public async Task No_agranda_una_imagen_chica()
    {
        var path = await Storage().SaveProductImageAsync(Guid.NewGuid(), Png(300, 100));

        Assert.Equal((300, 100), SizeOf(path));
    }

    [Fact]
    public async Task Respeta_la_orientacion_EXIF_de_las_fotos_del_celular()
    {
        // Guardada de costado (40x20) con orientación "girar 90° horario": tiene que quedar 20x40.
        var path = await Storage().SaveProductImageAsync(Guid.NewGuid(), JpegConOrientacion6());

        Assert.Equal((20, 40), SizeOf(path));
    }

    [Fact]
    public async Task Convierte_a_WebP()
    {
        var path = await Storage().SaveProductImageAsync(Guid.NewGuid(), Png(100, 100));

        using var codec = SKCodec.Create(Path.Combine(_root, path));
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
    }

    [Fact]
    public async Task No_deja_archivos_temporales()
    {
        var productId = Guid.NewGuid();
        await Storage().SaveProductImageAsync(productId, Png(100, 100));

        var files = Directory.GetFiles(Path.Combine(_root, "7", "products", productId.ToString()));
        Assert.Equal(2, files.Length);
        Assert.DoesNotContain(files, f => f.EndsWith(".tmp"));
    }

    [Fact]
    public async Task Rechaza_un_archivo_que_no_es_imagen()
    {
        var texto = new MemoryStream("esto no es una imagen"u8.ToArray());

        var ex = await Assert.ThrowsAsync<CustomException>(() => Storage().SaveProductImageAsync(Guid.NewGuid(), texto));
        Assert.Contains("imagen válida", ex.Message);
    }

    [Fact]
    public async Task Rechaza_un_SVG()
    {
        var svg = new MemoryStream("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>"u8.ToArray());

        await Assert.ThrowsAsync<CustomException>(() => Storage().SaveProductImageAsync(Guid.NewGuid(), svg));
    }

    [Fact]
    public async Task Rechaza_un_formato_de_imagen_no_permitido()
    {
        // GIF mínimo de 1x1 (Skia no lo codifica, pero sí lo reconoce al leerlo).
        var ms = new MemoryStream(Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"));

        var ex = await Assert.ThrowsAsync<CustomException>(() => Storage().SaveProductImageAsync(Guid.NewGuid(), ms));
        Assert.Contains("Formato no permitido", ex.Message);
    }

    [Fact]
    public async Task Rechaza_dimensiones_excesivas_y_no_deja_rastro()
    {
        var productId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<CustomException>(
            () => Storage().SaveProductImageAsync(productId, Png(8001, 10)));

        Assert.Contains("demasiado grande", ex.Message);
        var dir = Path.Combine(_root, "7", "products", productId.ToString());
        Assert.True(!Directory.Exists(dir) || Directory.GetFiles(dir).Length == 0);
    }

    [Fact]
    public async Task Borrar_quita_imagen_y_miniatura()
    {
        var storage = Storage();
        var path = await storage.SaveProductImageAsync(Guid.NewGuid(), Png(100, 100));

        storage.DeleteQuietly(path);

        Assert.False(File.Exists(Path.Combine(_root, path)));
        Assert.False(File.Exists(Path.Combine(_root, IImageStorage.ThumbPathOf(path))));
    }

    [Fact]
    public async Task No_borra_archivos_de_otro_tenant()
    {
        var ajeno = await Storage(tenantId: 8).SaveProductImageAsync(Guid.NewGuid(), Png(100, 100));

        Storage(tenantId: 7).DeleteQuietly(ajeno);

        Assert.True(File.Exists(Path.Combine(_root, ajeno)));
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("7/products/../../8/products/x/y.webp")]
    [InlineData("/etc/passwd")]
    public void Una_ruta_maliciosa_no_borra_nada_ni_lanza(string ruta)
    {
        Directory.CreateDirectory(_root);
        Storage().DeleteQuietly(ruta);   // no debe lanzar
    }

    [Fact]
    public async Task Borrar_la_carpeta_del_producto_quita_todo()
    {
        var storage = Storage();
        var productId = Guid.NewGuid();
        await storage.SaveProductImageAsync(productId, Png(100, 100));

        storage.DeleteProductFolderQuietly(productId);

        Assert.False(Directory.Exists(Path.Combine(_root, "7", "products", productId.ToString())));
    }
}
