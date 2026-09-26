using Common.Utilities;
using Common.Utilities.Exceptions;
using Common.Utilities.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Inventory.Infrastructure;

/// <summary>
/// Guarda las imágenes en un directorio del servidor. Nginx las sirve desde ahí,
/// así que la API solo interviene al subir o borrar.
/// </summary>
/// <remarks>
/// Las carpetas se nombran con el <c>public_id</c> del tenant.
/// Configuración (sección <c>Media</c>): <c>RootPath</c> es el directorio raíz y
/// debe ser el mismo que Nginx publica en <c>/media/</c>.
/// </remarks>
public sealed class DiskImageStorage : IImageStorage
{
    private const int FullSize = 800;
    private const int ThumbSize = 200;
    private const int Quality = 80;

    // Tope de la imagen de ENTRADA. Una imagen chica en bytes puede expandirse a
    // gigas en memoria al decodificarla, así que se mira el tamaño declarado en
    // la cabecera antes de decodificar nada.
    private const int MaxInputDimension = 8000;

    private readonly string _root;
    private readonly ITenantContext _tenant;
    private readonly ITenantFolderResolver _folders;
    private readonly ILogger<DiskImageStorage> _logger;

    public DiskImageStorage(IConfiguration configuration, ITenantContext tenant,
                            ITenantFolderResolver folders, ILogger<DiskImageStorage> logger)
    {
        _tenant = tenant;
        _folders = folders;
        _logger = logger;
        _root = Path.GetFullPath(configuration["Media:RootPath"] is { Length: > 0 } p ? p : "media");
    }

    public async Task<string> SaveProductImageAsync(Guid productId, Stream input, CancellationToken ct = default)
    {
        var relativeDir = ProductDir(productId);
        var fileId = Guid.NewGuid().ToString("N");
        var relativePath = $"{relativeDir}/{fileId}.webp";
        var thumbRelative = IImageStorage.ThumbPathOf(relativePath);

        var fullPath = Resolve(relativePath);
        var thumbPath = Resolve(thumbRelative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Se escribe a temporales y se renombra al final: si algo falla a mitad
        // no queda una imagen a medias que la base ya referencie.
        var fullTmp = fullPath + ".tmp";
        var thumbTmp = thumbPath + ".tmp";
        try
        {
            // Decodificar y recodificar es CPU puro; se saca del hilo del request.
            var (full, thumb) = await Task.Run(() => Process(input), ct);

            await File.WriteAllBytesAsync(fullTmp, full, ct);
            await File.WriteAllBytesAsync(thumbTmp, thumb, ct);

            File.Move(fullTmp, fullPath);
            File.Move(thumbTmp, thumbPath);
        }
        catch
        {
            TryDelete(fullTmp); TryDelete(thumbTmp); TryDelete(fullPath); TryDelete(thumbPath);
            throw;
        }

        return relativePath;
    }

    public void DeleteQuietly(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        try
        {
            // Solo se borra dentro de la carpeta de productos del tenant: la ruta
            // viene de la base, pero un valor corrupto no debe poder apuntar afuera.
            // Se compara la ruta YA resuelta: mirar solo el texto dejaba pasar
            // "{tenant}/products/../../{otro}/...", que empieza bien y termina en la
            // carpeta de otra empresa.
            var permitido = Resolve($"{TenantSegment()}/products") + Path.DirectorySeparatorChar;
            var completa = Resolve(relativePath);
            if (!completa.StartsWith(permitido, StringComparison.Ordinal))
            {
                _logger.LogWarning("Ruta de imagen fuera del tenant, no se borra: {Path}", relativePath);
                return;
            }
            TryDelete(completa);
            TryDelete(Resolve(IImageStorage.ThumbPathOf(relativePath)));
        }
        catch (Exception ex) { _logger.LogWarning(ex, "No se pudo borrar la imagen {Path}", relativePath); }
    }

    public void DeleteProductFolderQuietly(Guid productId)
    {
        try
        {
            var dir = Resolve(ProductDir(productId));
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "No se pudo borrar la carpeta de imágenes del producto {ProductId}", productId); }
    }

    /// <summary>
    /// Valida la imagen y devuelve los bytes WebP de la versión completa y de la
    /// miniatura. Volver a codificar descarta los metadatos (EXIF, geolocalización).
    /// </summary>
    private static (byte[] Full, byte[] Thumb) Process(Stream input)
    {
        using var buffer = new MemoryStream();
        input.CopyTo(buffer);
        buffer.Position = 0;

        using var codec = SKCodec.Create(buffer);
        // Sin códec es que los bytes no son una imagen que Skia reconozca (texto,
        // SVG, PDF...). El formato sale de los bytes, no de la extensión ni del
        // Content-Type, que el cliente controla.
        if (codec is null)
            throw new CustomException("El archivo no es una imagen válida. Use JPG, PNG o WebP.", MessageTypes.Warning);

        if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new CustomException("Formato no permitido. Use JPG, PNG o WebP.", MessageTypes.Warning);

        // Se mira el tamaño declarado en la cabecera ANTES de decodificar: una
        // imagen chica en bytes puede expandirse a gigas en memoria.
        if (codec.Info.Width > MaxInputDimension || codec.Info.Height > MaxInputDimension)
            throw new CustomException($"La imagen es demasiado grande (máximo {MaxInputDimension} px por lado).", MessageTypes.Warning);

        var origin = codec.EncodedOrigin;
        using var decoded = SKBitmap.Decode(codec)
            ?? throw new CustomException("La imagen está dañada o no se puede leer.", MessageTypes.Warning);

        // Las fotos del celular traen la orientación en EXIF, no en los píxeles:
        // sin esto se ven de costado.
        using var oriented = ApplyOrigin(decoded, origin);

        return (Encode(oriented, FullSize), Encode(oriented, ThumbSize));
    }

    private static byte[] Encode(SKBitmap source, int maxSide)
    {
        var scale = Math.Min(1.0, (double)maxSide / Math.Max(source.Width, source.Height));   // sin agrandar
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        SKBitmap? resized = null;
        try
        {
            var bitmap = source;
            if (width != source.Width || height != source.Height)
            {
                resized = source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
                    ?? throw new CustomException("No se pudo procesar la imagen.", MessageTypes.Warning);
                bitmap = resized;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Webp, Quality)
                ?? throw new CustomException("No se pudo procesar la imagen.", MessageTypes.Warning);
            return data.ToArray();
        }
        finally { resized?.Dispose(); }
    }

    /// <summary>Aplica la orientación EXIF, devolviendo un bitmap con los píxeles ya derechos.</summary>
    private static SKBitmap ApplyOrigin(SKBitmap src, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            return src.Copy();

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                          or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var w = src.Width;
        var h = src.Height;
        var dst = new SKBitmap(swap ? h : w, swap ? w : h, src.ColorType, src.AlphaType);

        using var canvas = new SKCanvas(dst);
        // Cada matriz lleva el píxel (x, y) de la imagen guardada a su lugar final.
        canvas.SetMatrix(origin switch
        {
            SKEncodedOrigin.TopRight    => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),        // espejo horizontal
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),       // 180°
            SKEncodedOrigin.BottomLeft  => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),        // espejo vertical
            SKEncodedOrigin.LeftTop     => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),         // transpuesta
            SKEncodedOrigin.RightTop    => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),        // 90° horario
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),       // transversa
            SKEncodedOrigin.LeftBottom  => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),        // 90° antihorario
            _ => SKMatrix.Identity
        });
        canvas.DrawBitmap(src, 0, 0);
        return dst;
    }

    /// <summary>Carpeta del tenant: su public_id, nunca el id numérico.</summary>
    private string TenantSegment() =>
        _folders.FolderOf(_tenant.TenantId
            ?? throw new InvalidOperationException("Las imágenes solo se manejan con un tenant resuelto."));

    private string ProductDir(Guid productId) => $"{TenantSegment()}/products/{productId}";

    /// <summary>Ruta absoluta, garantizando que queda dentro de la raíz.</summary>
    private string Resolve(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Ruta fuera del directorio de imágenes.");
        return full;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
