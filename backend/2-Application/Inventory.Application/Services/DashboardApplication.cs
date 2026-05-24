using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application.Services;

public class DashboardApplication(IDashboardRepository _dashboardRepository) : IDashboardApplication
{
    public async Task<Response<DashboardResponse>> GetDashboard()
    {
        Response<DashboardResponse> respuesta = new();
        try
        {
            respuesta.Data = await _dashboardRepository.GetDashboard();
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
}
