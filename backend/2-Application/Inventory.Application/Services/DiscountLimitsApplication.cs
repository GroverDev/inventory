using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;
using Microsoft.Extensions.Options;

namespace Inventory.Application;

public class DiscountLimitsApplication(IDiscountLimitsRepository _repository, IOptions<PosSettings> _defaults) : IDiscountLimitsApplication
{
    public async Task<EffectiveDiscountLimits> GetEffective()
    {
        var filas = await _repository.GetForCurrentBranch();
        return SaleDiscountRules.Resolve(
            filas.FirstOrDefault(f => f.BranchId is null),
            filas.FirstOrDefault(f => f.BranchId is not null),
            _defaults.Value);
    }

    public async Task<Response<List<DiscountLimitLevel>>> GetLevels()
    {
        Response<List<DiscountLimitLevel>> resp = new() { Data = [] };
        try
        {
            resp.Data = await _repository.GetLevels();
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> SaveLevels(List<DiscountLimitLevel> levels, int userId)
    {
        Response<bool> resp = new();
        try
        {
            if (levels.Count(l => l.BranchId is null) > 1 ||
                levels.Where(l => l.BranchId is not null).GroupBy(l => l.BranchId).Any(g => g.Count() > 1))
                throw new CustomException("Hay niveles repetidos.");

            // Coherencia en cada sucursal ya resuelta, no solo fila por fila: un
            // tope de cajero de la sucursal por encima del máximo de la empresa
            // dejaría al supervisor sin nada que autorizar.
            var empresa = levels.FirstOrDefault(l => l.BranchId is null);
            var defaults = _defaults.Value;
            foreach (var nivel in levels.Where(l => l.BranchId is not null).Append(new DiscountLimitLevel()))
            {
                var efectivo = SaleDiscountRules.Resolve(empresa, nivel.BranchId is null ? null : nivel, defaults);
                string quien = nivel.BranchId is null ? "la empresa" : $"la sucursal {nivel.BranchName}".Trim();

                if (efectivo.GeneralMaxPct is decimal p && efectivo.CashierMaxPct > p)
                    throw new CustomException($"En {quien}, el tope del cajero ({efectivo.CashierMaxPct:0.##}%) supera el tope máximo ({p:0.##}%).");
                if (efectivo.GeneralMaxAmount is decimal m && efectivo.CashierMaxAmount > m)
                    throw new CustomException($"En {quien}, el tope del cajero (Bs. {efectivo.CashierMaxAmount:0.00}) supera el tope máximo (Bs. {m:0.00}).");
            }

            await _repository.SaveLevels(levels, userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }
}
