using ApiAggregator.Services.Statistics;

namespace ApiAggregator.Tests
{
    public class StatisticsServiceTests
    {
        [Fact]
        public void Record_ShouldAddStatsCorrectly()
        {
            // Arrange
            var service = new StatisticsService();

            // Act
            service.Record("GitHubAPI", TimeSpan.FromMilliseconds(80));   // fast
            service.Record("GitHubAPI", TimeSpan.FromMilliseconds(150));  // average
            service.Record("GitHubAPI", TimeSpan.FromMilliseconds(250));  // slow
            service.Record("NewsAPI", TimeSpan.FromMilliseconds(90));     // fast

            var stats = service.GetStatistics();

            // Assert
            Assert.Equal(2, stats.Count);
            Assert.True(stats.ContainsKey("GitHubAPI"));
            Assert.True(stats.ContainsKey("NewsAPI"));

            var githubStats = stats["GitHubAPI"];
            Assert.Equal(3, githubStats.TotalRequests);
            Assert.Equal(1, githubStats.FastCount);
            Assert.Equal(1, githubStats.AverageCount);
            Assert.Equal(1, githubStats.SlowCount);
            Assert.InRange(githubStats.AverageMs, 160, 165); // (80 + 150 + 250)/3 = 160

            var newsStats = stats["NewsAPI"];
            Assert.Equal(1, newsStats.TotalRequests);
            Assert.Equal(1, newsStats.FastCount);
            Assert.Equal(0, newsStats.AverageCount);
            Assert.Equal(0, newsStats.SlowCount);
        }
    }
}