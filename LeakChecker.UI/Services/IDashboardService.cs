using LeakChecker.UI.Models;

namespace LeakChecker.UI.Services;

public interface IDashboardService
{
    Task<DashboardStats>        GetStatsAsync();
    Task<List<ParseListModel>>  GetRecentParsesAsync(int limit = 50);
    Task<ParseDetailModel?> GetParseByIdAsync(string parseId);
}