
namespace ApiAggregator.Services.Statistics
{
    public class PerformanceAnomalyLoggerService : BackgroundService
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<PerformanceAnomalyLoggerService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public PerformanceAnomalyLoggerService(IStatisticsService statisticsService, ILogger<PerformanceAnomalyLoggerService> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PerformanceMonitorService started.");
            while (!stoppingToken.IsCancellationRequested)
            {

                _logger.LogInformation("[Background] Performance logger running at: {Time}", DateTime.UtcNow);

                try
                {
                    CheckForAnomalies();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while checking for performance anomalies.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("PerformanceMonitorService stopped.");
        }

        // Checks for performance anomalies by comparing recent (5 mins) stats with historical averages
        private void CheckForAnomalies()
        {
            var stats = _statisticsService.GetStatistics();

            foreach (var (api, data) in stats)
            {
                var recent = _statisticsService.GetRecentStats(api, TimeSpan.FromMinutes(5));

                // Skip if not enough recent data
                if (recent.Count < 5) continue;

                var historicalAvg = data.AverageMs;
                var recentAvg = recent.Average(x => x.Duration.TotalMilliseconds);

                if (recentAvg > historicalAvg * 1.5)
                {
                    _logger.LogWarning($"[Anomaly] {api} average response time increased significantly: " +
                                       $"{recentAvg:F2}ms (recent) vs {historicalAvg:F2}ms (overall)");
                }
            }
        }
    }
}
