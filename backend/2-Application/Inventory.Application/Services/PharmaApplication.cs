using System.Globalization;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class PharmaApplication(IPharmaRepository _pharmaRepository) : IPharmaApplication
{
    public async Task<Response<List<PharmaCatalogItem>>> GetForms() =>
        await Listar(_pharmaRepository.GetForms);

    public async Task<Response<List<PharmaCatalogItem>>> GetRoutes() =>
        await Listar(_pharmaRepository.GetRoutes);

    private static async Task<Response<List<PharmaCatalogItem>>> Listar(Func<Task<List<PharmaCatalogItem>>> consulta)
    {
        var resp = new Response<List<PharmaCatalogItem>> { Data = [] };
        try
        {
            resp.Data = await consulta();
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<List<PharmaSubstance>>> SearchSubstances(string nombre)
    {
        var resp = new Response<List<PharmaSubstance>> { Data = [] };
        try
        {
            resp.Data = await _pharmaRepository.SearchSubstances(nombre ?? "");
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<ProductPharma>> GetByProduct(string productId)
    {
        var resp = new Response<ProductPharma> { Data = new() };
        try
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty)
                throw new CustomException("El identificador del producto no es válido.", MessageTypes.Warning);

            // Que no haya datos NO es un error: la mayoría de los productos de
            // una farmacia (shampoo, pañales) no tiene ficha farmacéutica.
            resp.Data = await _pharmaRepository.GetByProduct(id) ?? new ProductPharma { ProductId = id };
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> Save(string productId, ProductPharmaRequest request, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty)
                throw new CustomException("El identificador del producto no es válido.", MessageTypes.Warning);

            DateTime? vigencia = null;
            if (!string.IsNullOrWhiteSpace(request.SanitaryRegistryExpiry))
            {
                if (!DateTime.TryParse(request.SanitaryRegistryExpiry, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var parsed))
                    throw new CustomException(
                        $"La vigencia del registro sanitario «{request.SanitaryRegistryExpiry}» no es válida.",
                        MessageTypes.Warning);
                vigencia = parsed.Date;
            }

            var datos = new ProductPharma
            {
                ProductId = id,
                FormId  = Guid.TryParse(request.FormId,  out var f) && f != Guid.Empty ? f : null,
                RouteId = Guid.TryParse(request.RouteId, out var r) && r != Guid.Empty ? r : null,
                Presentation     = Limpiar(request.Presentation),
                DosageReference  = Limpiar(request.DosageReference),
                ProductType      = Limpiar(request.ProductType),
                SanitaryRegistry = Limpiar(request.SanitaryRegistry),
                SanitaryRegistryExpiry = vigencia,
            };

            var componentes = request.Components
                .Select(c => (
                    SubstanceId: Guid.TryParse(c.SubstanceId, out var s) && s != Guid.Empty ? (Guid?)s : null,
                    SubstanceName: (c.SubstanceName ?? "").Trim(),
                    Value: c.ConcentrationValue,
                    Unit: Limpiar(c.ConcentrationUnit),
                    EsActivo: c.IsActiveIngredient))
                .Where(c => c.SubstanceId is not null || c.SubstanceName.Length > 0)
                .ToList();

            await _pharmaRepository.Save(id, datos, componentes, userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(ex.messageType == MessageTypes.Nothing ? MessageTypes.Warning : ex.messageType, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    /// <summary>
    /// Prospecto del producto. Se pide aparte de la ficha a propósito: son
    /// varios KB y la mayoría de los productos no lo tiene, así que solo se
    /// trae cuando alguien lo va a leer.
    /// </summary>
    public async Task<Response<string>> GetLeaflet(string productId)
    {
        var resp = new Response<string> { Data = "" };
        try
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty)
                throw new CustomException("El identificador del producto no es válido.", MessageTypes.Warning);

            resp.Data = await _pharmaRepository.GetLeaflet(id) ?? "";
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> SaveLeaflet(string productId, string? contenido, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty)
                throw new CustomException("El identificador del producto no es válido.", MessageTypes.Warning);

            await _pharmaRepository.SaveLeaflet(id, contenido, userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<List<ProductEquivalentResponse>>> GetEquivalents(string productId)
    {
        var resp = new Response<List<ProductEquivalentResponse>> { Data = [] };
        try
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty)
                throw new CustomException("El identificador del producto no es válido.", MessageTypes.Warning);

            var automaticas = await _pharmaRepository.GetEquivalents(id);
            var manuales = await _pharmaRepository.GetManualAlternatives(id);

            // Cuando una alternativa está en las dos listas gana la MANUAL, y
            // sigue apareciendo una sola vez.
            //
            // Antes ganaba la automática, con el argumento de que "equivalente
            // por composición" dice algo más fuerte que "sugerida". El problema
            // práctico es que volvía imposible destacar un equivalente: cargarlo
            // a mano no tenía ningún efecto visible, ni en la ficha ni en el
            // mostrador. Que gane la manual no pierde el dato —el motivo típico
            // es justamente "Misma composición"— y además deja decir POR QUÉ se
            // sugiere ese y no otro de los equivalentes, que es lo que el
            // vendedor necesita leer.
            var idsManuales = manuales.Select(m => m.ProductId).ToHashSet();

            resp.Data = [.. manuales, .. automaticas.Where(a => !idsManuales.Contains(a.ProductId))];
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> AddAlternative(string productId, string alternativeId, string? motivo, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty ||
                !Guid.TryParse(alternativeId, out var altId) || altId == Guid.Empty)
                throw new CustomException("Los identificadores no son válidos.", MessageTypes.Warning);

            if (id == altId)
                throw new CustomException("Un producto no puede ser alternativa de sí mismo.", MessageTypes.Warning);

            await _pharmaRepository.AddAlternative(id, altId, Limpiar(motivo), userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> RemoveAlternative(string productId, string alternativeId)
    {
        var resp = new Response<bool>();
        try
        {
            if (!Guid.TryParse(productId, out var id) || !Guid.TryParse(alternativeId, out var altId))
                throw new CustomException("Los identificadores no son válidos.", MessageTypes.Warning);

            await _pharmaRepository.RemoveAlternative(id, altId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
