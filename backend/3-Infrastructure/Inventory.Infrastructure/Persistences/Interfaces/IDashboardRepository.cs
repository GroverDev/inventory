using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IDashboardRepository
{
    Task<DashboardResponse> GetDashboard();
}
