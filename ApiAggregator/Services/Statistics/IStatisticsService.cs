using ApiAggregator.Controllers;
using ApiAggregator.Models;

namespace ApiAggregator.Services.Statistics
{
    public interface IStatisticsService
    {
        void Record(string source, TimeSpan duration);
        Dictionary<string, RequestTotalStats> GetStatistics();
        List<RequestStats> GetRecentStats(string source, TimeSpan period);
    }
}
