using LeakChecker.UI.Models;

namespace LeakChecker.UI.Services;

public interface IDashboardService
{
    Task<DashboardStats>  GetStatsAsync();
    Task<List<ParseRow>>  GetRecentParsesAsync(int limit = 10);
}