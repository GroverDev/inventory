using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class SalesApplication(
    ISalesRepository _salesRepository,
    IProductRepository _productRepository,
    ICustomersRepository _customersRepository,
    IDiscountRepository _discountRepository,
    IDiscountLimitsApplication _discountLimits,
    IPaymentMethodRepository _paymentMethodRepository) : ISalesApplication
{
    public async Task<Response<string>> CreateSale(SaleRequest saleRequest, int createdBy, string userRole, bool supervisorApproved = false)
    {
        Response<string> respuesta = new();
        try
        {
            if (saleRequest.Detail.Count > 0)
            {
                // Precios, descuentos y topes se calculan acá, no se toman del
                // cliente. Antes el servidor validaba el porcentaje declarado de
                // un descuento manual pero grababa el importe que mandaba el
                // navegador, y aceptaba cualquier precio unitario: una petición
                // armada a mano podía vender a cualquier precio.
                var limites = await _discountLimits.GetEffective();
                // `userRole` llega con todos los roles: quien además de Cajero tiene
                // un rol administrativo no necesita autorización de supervisor.
                bool cajeroSinAutorizacion =
                    Common.Utilities.Comun.Bases.RolePolicy.VeSoloLoPropio(userRole) && !supervisorApproved;

                foreach (var line in saleRequest.Detail)
                {
                    if (line.Quantity <= 0)
                        throw new CustomException("Las cantidades deben ser mayores a cero.");

                    // El precio es el vigente en la sucursal (su excepción, o el
                    // base). Si el punto de venta tiene uno viejo en memoria, se
                    // avisa en vez de vender a un precio que ya no existe.
                    var producto = await _productRepository.GetProduct(Guid.Parse(line.ProductId));
                    if (Math.Abs(line.UnitPrice - producto.SalePrice) > 0.005m)
                        throw new CustomException(
                            $"El precio de «{producto.ProductName}» es Bs. {producto.SalePrice:0.00}, no Bs. {line.UnitPrice:0.00}. " +
                            "Actualice el punto de venta e intente de nuevo.");

                    line.UnitPrice = producto.SalePrice;
                    line.LineSubtotal = Math.Round(line.Quantity * line.UnitPrice, 2);

                    if (!string.IsNullOrEmpty(line.DiscountId) && Guid.TryParse(line.DiscountId, out var discountGuid))
                    {
                        // Del catálogo: ya aprobado por quien lo dio de alta.
                        var discount = await _discountRepository.GetDiscount(discountGuid)
                            ?? throw new CustomException($"El descuento indicado en el producto no existe o está inactivo.");

                        line.LineTotalDiscounts = discount.Type == "Percentage"
                            ? Math.Round(line.LineSubtotal * discount.Value / 100, 2)
                            : Math.Min(discount.Value, line.LineSubtotal);
                    }
                    else
                    {
                        line.LineTotalDiscounts = SaleDiscountRules.ManualAmount(line.DiscountType, line.DiscountValue, line.LineSubtotal);
                        SaleDiscountRules.CheckLimits(line.DiscountType, line.DiscountValue, limites, cajeroSinAutorizacion, "por línea");
                    }
                    line.LineTotal = line.LineSubtotal - line.LineTotalDiscounts;
                }

                decimal subtotalAfterLineDiscounts = saleRequest.Detail.Sum(x => x.LineTotal);

                if (!string.IsNullOrEmpty(saleRequest.HeaderDiscountId) && Guid.TryParse(saleRequest.HeaderDiscountId, out var headerDiscGuid))
                {
                    var headerDiscount = await _discountRepository.GetDiscount(headerDiscGuid)
                        ?? throw new CustomException("El descuento de cabecera no existe o está inactivo.");

                    saleRequest.HeaderDiscountAmount = headerDiscount.Type == "Percentage"
                        ? Math.Round(subtotalAfterLineDiscounts * headerDiscount.Value / 100, 2)
                        : Math.Min(headerDiscount.Value, subtotalAfterLineDiscounts);
                }
                else
                {
                    saleRequest.HeaderDiscountAmount = SaleDiscountRules.ManualAmount(
                        saleRequest.HeaderDiscountType, saleRequest.HeaderDiscountValue, subtotalAfterLineDiscounts);
                    SaleDiscountRules.CheckLimits(saleRequest.HeaderDiscountType, saleRequest.HeaderDiscountValue,
                        limites, cajeroSinAutorizacion, "global");
                }

                // Recompute sale totals on the server — never trust client-side totals
                saleRequest.Subtotal              = saleRequest.Detail.Sum(x => x.LineSubtotal);
                var totalLineDiscounts             = saleRequest.Detail.Sum(x => x.LineTotalDiscounts);
                saleRequest.TotalDiscounts         = totalLineDiscounts + saleRequest.HeaderDiscountAmount;
                saleRequest.Total                  = saleRequest.Subtotal - saleRequest.TotalDiscounts;

                if (saleRequest.Total <= 0)
                    throw new CustomException("El total de la venta no puede ser cero o negativo.");

                // Valida los pagos y calcula el vuelto en el servidor (ver SalePaymentRules).
                var metodos = await _paymentMethodRepository.GetPaymentMethods();
                SalePaymentRules.Apply(saleRequest.Payments, saleRequest.Total,
                    metodos.ToDictionary(m => m.Id, m => m.RequiresChanges));

                var respCustomer = await _customersRepository.GetCustomer(Guid.Parse(saleRequest.CustomerId));

                saleRequest.SaleDate = saleRequest.SaleDate == "" ? DateTime.UtcNow.ToString() : saleRequest.SaleDate;
                saleRequest.Id = Guid.Empty.ToString();
                if (string.IsNullOrEmpty(saleRequest.HeaderDiscountId))
                    saleRequest.HeaderDiscountId = Guid.Empty.ToString();
                saleRequest.Detail.ForEach(x =>
                {
                    x.SaleId = saleRequest.Id;
                    x.Id = saleRequest.Id;
                    if (string.IsNullOrEmpty(x.DiscountId))
                        x.DiscountId = Guid.Empty.ToString();
                });

                var sale = saleRequest.Adapt<Sale>();
                // El mismo criterio de los descuentos: al cajero se le exige la
                // autorización de un supervisor, y a quien tiene un rol
                // administrativo no. Con la firma, el faltante se vende y el saldo
                // queda negativo, como siempre. Lo aplica la base, dentro de la
                // transacción de la venta (ver fn_asignar_fefo).
                sale.EnforceStock = cajeroSinAutorizacion;
                // SaleDate llega como texto y Mapster lo parsea a hora local del
                // servidor. Npgsql después guarda los dígitos tal cual, así que sin
                // normalizar la venta quedaría corrida tantas horas como el servidor
                // se aparte de UTC. En producción (contenedor en UTC) esto no cambia
                // nada; es lo que hace que también salga bien fuera de UTC.
                sale.SaleDate = sale.SaleDate.ToUniversalTime();

                // Convertir Guid.Empty → null en campos opcionales de descuento
                if (sale.HeaderDiscountId == Guid.Empty) sale.HeaderDiscountId = null;
                foreach (var det in sale.Detail)
                    if (det.DiscountId == Guid.Empty) det.DiscountId = null;

                AuditHelper.SetCreated(sale, createdBy);
                foreach (var det in sale.Detail)
                {
                    var respProduct = await _productRepository.GetProduct(det.ProductId);
                    AuditHelper.SetCreated(det, createdBy);
                }

                respuesta.Data = await _salesRepository.CreateSale(sale);
                respuesta.ok = true;
            }
            else
            {
                throw new CustomException("El detalle de la venta no puede estar vacio.");
            }
        }
        catch (CustomException ex) when (StockSupervisorRequiredException.EnCadena(ex))
        {
            // Falta la firma de un supervisor. El Id le dice al punto de venta que
            // no es un rechazo sino un pedido de autorización, para que la pida y
            // reintente en vez de mostrar el error.
            respuesta.SetMessage(MessageTypes.Warning, ex.Message);
            respuesta.Message.Id = StockSupervisorRequiredException.MessageId;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateSale(SaleRequest saleRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (saleRequest.Detail.Count > 0)
            {
                var sale = saleRequest.Adapt<Sale>();
                sale.SaleDate = sale.SaleDate.ToUniversalTime();   // ver CreateSale
                AuditHelper.SetModified(sale, modifiedBy);
                foreach (var det in sale.Detail)
                {
                    AuditHelper.SetModified(det, modifiedBy);
                }
                var rowsAffected = await _salesRepository.UpdateSale(sale);
                if (rowsAffected <= 0)
                    throw new CustomException("No se pudo modificar la venta");
                respuesta.Data = respuesta.ok = true;
            }
            else
            {
                throw new CustomException("El detalle de la venta no puede estar vacio.");
            }
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteSale(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid saleId = Guid.Parse(id);

            // Eliminar solo baja el flag `state`: no repone stock ni mueve caja, pero
            // saca la venta de los totales y del arqueo. Si se permitiera sobre una
            // venta viva, el efectivo cobrado desaparecería de los números mientras
            // sigue en el cajón, y el faltante se le imputaría al cajero. Por eso solo
            // se admite sobre una venta ya devuelta por completo: en ese punto el stock
            // volvió y la plata ya se reintegró, así que borrarla no descuadra nada.
            var sale = await _salesRepository.GetSale(saleId);
            if (sale.Id == Guid.Empty)
                throw new CustomException("La venta no existe.");

            bool totalmenteDevuelta = sale.Detail.Count > 0 && sale.Detail.All(linea =>
                sale.Returns.SelectMany(r => r.Detail)
                    .Where(d => d.SaleDetailId == linea.Id)
                    .Sum(d => d.QuantityReturned) >= linea.Quantity);

            if (!totalmenteDevuelta)
                throw new CustomException(
                    "Solo se puede eliminar una venta que ya fue devuelta por completo. " +
                    "Registre primero la devolución: así el stock vuelve y el reintegro queda en caja.");

            var rowsAffected = await _salesRepository.DeleteSale(saleId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar la venta");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
    public async Task<Response<SalesPagedResponse>> GetSales(string saleDateInitial, string saleDateEnd, int userId, string rol, int page = 1, int pageSize = 50, string? sellerName = null, Guid[]? branches = null)
    {
        Response<SalesPagedResponse> response = new() { Data = new() };
        try
        {
            if (!DateTime.TryParse(saleDateInitial, out var diaDesde))
                throw new CustomException("Fecha desde, es incorrecto.", MessageTypes.Warning);
            if (diaDesde.Year > BusinessTime.Today.Year + 1)
                throw new CustomException($"Fecha desde, el año no puede ser mayor al año {BusinessTime.Today.Year + 1}.", MessageTypes.Warning);
            if (diaDesde.Year < 1900)
                throw new CustomException("Fecha desde, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (!DateTime.TryParse(saleDateEnd, out var diaHasta))
                throw new CustomException("Fecha hasta, es incorrecto.", MessageTypes.Warning);
            if (diaHasta.Year > BusinessTime.Today.Year + 1)
                throw new CustomException($"Fecha hasta, el año no puede ser mayor al año {BusinessTime.Today.Year + 1}.", MessageTypes.Warning);
            if (diaHasta.Year < 1900)
                throw new CustomException("Fecha hasta, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (diaDesde.Date > diaHasta.Date)
                throw new CustomException("Fecha desde, no puede ser mayor a la Fecha hasta.", MessageTypes.Warning);

            // El usuario elige días del calendario boliviano; sale_date guarda
            // instantes UTC. Sin convertir, el día consultado arrancaba a las
            // 20:00 del día anterior. El tope es exclusivo (ver BusinessTime).
            var desdeUtc = BusinessTime.StartOfDayUtc(diaDesde);
            var hastaUtc = BusinessTime.EndOfDayUtcExclusive(diaHasta);

            // `rol` llega con todos los roles del usuario separados por coma. La
            // regla —restringido solo si Cajero es su único rol— vive en RolePolicy.
            bool soloLoPropio = Common.Utilities.Comun.Bases.RolePolicy.VeSoloLoPropio(rol);

            int? filterUserId    = soloLoPropio ? userId : null;
            string? filterSeller = soloLoPropio ? null   : sellerName;

            response.Data = await _salesRepository.GetSales(
                desdeUtc,
                hastaUtc,
                filterUserId,
                page,
                pageSize,
                filterSeller,
                branches);

            response.ok = true;
        }
        catch (CustomException ex) { response.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { response.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return response;
    }
    public async Task<Response<SaleProductResponse>> GetSale(string id)
    {
        Response<SaleProductResponse> respSales = new() { Data = new() };
        try
        {
            Guid saleId = Guid.Parse(id);
            respSales.Data = await _salesRepository.GetSale(saleId);
            respSales.ok = true;
        }
        catch (CustomException ex) { respSales.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respSales.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respSales;
    }
}
