using Inventory.Application.Mappers;
using Inventory.Domain;
using Inventory.Domain.Entities.Requests;
using Mapster;

namespace MultiTenancy.Tests;

/// <summary>
/// El mapeo de las fechas de compra, que son días del calendario y no instantes.
/// </summary>
/// <remarks>
/// No necesita base de datos. `purchase_date`, `estimated_delivery_date` y
/// `delivery_date` son columnas `date` y en C# son <see cref="DateOnly"/>: antes
/// eran `timestamptz` escritas como "medianoche UTC", lo que obligaba a tratarlas
/// distinto en cada capa y a que los filtros NO convirtieran de hora boliviana.
/// Estas pruebas fijan que el día que manda el cliente es exactamente el que
/// queda, sin corrimientos.
/// </remarks>
public class PurchaseDateMappingTests
{
    public PurchaseDateMappingTests() =>
        new InventoryMappingConfig().Register(TypeAdapterConfig.GlobalSettings);

    [Fact]
    public void El_dia_del_request_llega_intacto()
    {
        var req = new PurchaseRequest
        {
            PurchaseDate = "2026-08-21",
            EstimatedDeliveryDate = "2026-08-28",
        };

        var purchase = req.Adapt<Purchase>();

        Assert.Equal(new DateOnly(2026, 8, 21), purchase.PurchaseDate);
        Assert.Equal(new DateOnly(2026, 8, 28), purchase.EstimatedDeliveryDate);
    }

    [Fact]
    public void La_entrega_estimada_vacia_queda_en_el_centinela()
    {
        // El frontend manda "" cuando no se fijó fecha de entrega.
        var req = new PurchaseRequest { PurchaseDate = "2026-08-21", EstimatedDeliveryDate = "" };

        Assert.Equal(DateOnly.MinValue, req.Adapt<Purchase>().EstimatedDeliveryDate);
    }

    [Fact]
    public void La_vuelta_al_request_devuelve_yyyy_MM_dd()
    {
        // Es el formato que espera el <input type="date"> de la web.
        var resp = new PurchaseProductResponse
        {
            PurchaseDate = new DateOnly(2026, 8, 21),
            EstimatedDeliveryDate = new DateOnly(2026, 8, 28),
        };

        var req = resp.Adapt<PurchaseRequest>();

        Assert.Equal("2026-08-21", req.PurchaseDate);
        Assert.Equal("2026-08-28", req.EstimatedDeliveryDate);
    }

    [Fact]
    public void El_centinela_vuelve_como_cadena_vacia()
    {
        var resp = new PurchaseProductResponse
        {
            PurchaseDate = new DateOnly(2026, 8, 21),
            EstimatedDeliveryDate = DateOnly.MinValue,
        };

        Assert.Equal("", resp.Adapt<PurchaseRequest>().EstimatedDeliveryDate);
    }

    [Fact]
    public void La_fecha_de_recepcion_llega_intacta()
    {
        // PurchaseId y OperationUid van con valores reales: el mapeo los parsea
        // como Guid y una cadena vacía lo haría fallar por un motivo ajeno a la
        // fecha, que es lo que esta prueba mira.
        var req = new PurchaseDeliveryRequest
        {
            PurchaseId = Guid.NewGuid().ToString(),
            OperationUid = Guid.NewGuid().ToString(),
            DeliveryDate = "2026-08-21",
        };

        Assert.Equal(new DateOnly(2026, 8, 21), req.Adapt<PurchaseDelivery>().DeliveryDate);
    }

    [Fact]
    public void Una_ida_y_vuelta_no_corre_el_dia()
    {
        // La regresión que se quiere evitar: que el día se desplace al pasar por
        // el mapeo, como pasaba cuando la fecha viajaba como instante UTC.
        var original = new PurchaseRequest { PurchaseDate = "2026-08-21", EstimatedDeliveryDate = "2026-08-21" };

        var ida = original.Adapt<Purchase>();
        var vuelta = new PurchaseProductResponse
        {
            PurchaseDate = ida.PurchaseDate,
            EstimatedDeliveryDate = ida.EstimatedDeliveryDate,
        }.Adapt<PurchaseRequest>();

        Assert.Equal(original.PurchaseDate, vuelta.PurchaseDate);
        Assert.Equal(original.EstimatedDeliveryDate, vuelta.EstimatedDeliveryDate);
    }
}
