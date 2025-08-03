using ApiAggregator.Models;
using System.Collections.Concurrent;

namespace ApiAggregator.Services.Statistics
{
    public class StatisticsService : IStatisticsService
    {
        private static readonly ConcurrentDictionary<string, List<RequestStats>> _stats = new();

        // Records a new request with its source and duration
        public void Record(string source, TimeSpan duration)
        {
            var entry = new RequestStats
            {
                Duration = duration,
                Timestamp = DateTime.UtcNow
            };

            _stats.AddOrUpdate(source,
                (_) => new List<RequestStats> { entry },
                (_, list) =>
                {
                    lock (list) list.Add(entry); // thread-safe
                    return list;
                });
        }

        // Retrieves aggregated statistics for all sources
        public Dictionary<string, RequestTotalStats> GetStatistics()
        {
            var result = new Dictionary<string, RequestTotalStats>();

            foreach (var (source, entries) in _stats)
            {
                lock (entries)
                {
                    if (entries.Count == 0) continue;

                    var avgMs = entries.Average(x => x.Duration.TotalMilliseconds);
                    var fast = entries.Count(x => x.Duration.TotalMilliseconds < 100);
                    var average = entries.Count(x => x.Duration.TotalMilliseconds is >= 100 and <= 200);
                    var slow = entries.Count(x => x.Duration.TotalMilliseconds > 200);

                    result[source] = new RequestTotalStats
                    {
                        TotalRequests = entries.Count,
                        AverageMs = avgMs,
                        FastCount = fast,
                        AverageCount = average,
                        SlowCount = slow
                    };
                }
            }

            return result;
        }

        // Retrieves recent stats for a specific source within the given time period
        public List<RequestStats> GetRecentStats(string source, TimeSpan period)
        {
            if (!_stats.TryGetValue(source, out var list))
                return new List<RequestStats>();

            lock (list)
            {
                var threshold = DateTime.UtcNow - period;
                return list.Where(x => x.Timestamp >= threshold).ToList();
            }
        }
    }
}
