using Common.Utilities.Exceptions;
using Inventory.Application;
using Inventory.Domain;

namespace MultiTenancy.Tests;

/// <summary>Pagos de una venta: el vuelto lo calcula el servidor.</summary>
public class PagosTests
{
    private static readonly Guid Efectivo = Guid.NewGuid();
    private static readonly Guid Tarjeta = Guid.NewGuid();
    private static readonly Dictionary<Guid, bool> DaVuelto = new() { [Efectivo] = true, [Tarjeta] = false };

    private static SalePaymentRequest Pago(Guid metodo, decimal monto, decimal vueltoQueMandaElCliente = 0) => new()
    {
        PaymentMethodId = metodo.ToString(), AmountGiven = monto, AmountReturned = vueltoQueMandaElCliente
    };

    [Fact]
    public void El_vuelto_del_efectivo_lo_calcula_el_servidor()
    {
        var pagos = new List<SalePaymentRequest> { Pago(Efectivo, 200, vueltoQueMandaElCliente: 0) };

        SalePaymentRules.Apply(pagos, 137.50m, DaVuelto);

        Assert.Equal(62.50m, pagos[0].AmountReturned);
    }

    [Fact]
    public void En_un_pago_mixto_el_vuelto_sale_del_efectivo_y_no_de_la_tarjeta()
    {
        var pagos = new List<SalePaymentRequest> { Pago(Tarjeta, 100), Pago(Efectivo, 50) };

        SalePaymentRules.Apply(pagos, 137.50m, DaVuelto);

        Assert.Equal(0, pagos[0].AmountReturned);
        Assert.Equal(12.50m, pagos[1].AmountReturned);
    }

    [Fact]
    public void No_se_puede_cobrar_de_mas_con_tarjeta()
    {
        var pagos = new List<SalePaymentRequest> { Pago(Tarjeta, 150) };

        var ex = Assert.Throws<CustomException>(() => SalePaymentRules.Apply(pagos, 137.50m, DaVuelto));
        Assert.Contains("sin vuelto", ex.Message);
    }

    [Fact]
    public void Lo_pagado_tiene_que_cubrir_el_total()
    {
        Assert.Throws<CustomException>(() =>
            SalePaymentRules.Apply([Pago(Efectivo, 100)], 137.50m, DaVuelto));
    }

    [Fact]
    public void Un_metodo_desconocido_o_un_monto_no_positivo_se_rechazan()
    {
        Assert.Throws<CustomException>(() =>
            SalePaymentRules.Apply([Pago(Guid.NewGuid(), 200)], 137.50m, DaVuelto));
        Assert.Throws<CustomException>(() =>
            SalePaymentRules.Apply([Pago(Efectivo, 200), Pago(Tarjeta, 0)], 137.50m, DaVuelto));
    }
}
