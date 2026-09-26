namespace Inventory.Infrastructure;

/// <summary>
/// Dónde viven las imágenes de los productos. Hoy es disco; si algún día hay
/// varios servidores basta otra implementación (S3, R2) sin tocar a quien la usa.
/// </summary>
/// <remarks>
/// Las rutas son siempre relativas y las arma la implementación a partir del
/// tenant del request: quien llama nunca decide en qué carpeta se escribe.
/// </remarks>
public interface IImageStorage
{
    /// <summary>
    /// Valida, reduce y guarda la imagen (tamaño completo y miniatura, ambas WebP).
    /// Devuelve la ruta relativa de la imagen completa.
    /// </summary>
    /// <exception cref="Common.Utilities.Exceptions.CustomException">
    /// El archivo no es una imagen válida o excede los límites.
    /// </exception>
    Task<string> SaveProductImageAsync(Guid productId, Stream input, CancellationToken ct = default);

    /// <summary>
    /// Borra la imagen y su miniatura. Nunca lanza: un archivo que no se pudo
    /// borrar no debe deshacer la operación de negocio que lo motivó.
    /// </summary>
    void DeleteQuietly(string? relativePath);

    /// <summary>Borra la carpeta completa del producto. Tampoco lanza.</summary>
    void DeleteProductFolderQuietly(Guid productId);

    /// <summary>Ruta relativa de la miniatura correspondiente a una imagen.</summary>
    static string ThumbPathOf(string relativePath) =>
        relativePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? relativePath[..^5] + "_thumb.webp"
            : relativePath + "_thumb";
}
