using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application.Interfaces;

public interface IDashboardApplication
{
    Task<Response<DashboardResponse>> GetDashboard();
}
