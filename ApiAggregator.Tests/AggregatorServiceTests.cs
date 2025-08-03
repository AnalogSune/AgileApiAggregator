using ApiAggregator.Models;
using ApiAggregator.Services.Aggregation;
using ApiAggregator.Services.External;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiAggregator.Tests
{
    public class AggregatorServiceTests
    {
        [Fact]
        public async Task AggregateAsync_ShouldReturnCombinedItemsFromAllApis()
        {
            // Arrange
            var mockNewsApi = new Mock<INewsApiService>();
            var mockGitHubApi = new Mock<IGitHubApiService>();
            var mockWeatherApi = new Mock<IWeatherApiService>();

            var keyword = "test";
            var sort = "date";
            var location = "Athens";
            var source = "";

            var newsItems = new List<AggregatedItem>
            {
                new() { Source = "NewsAPI", Title = "News 1", Timestamp = DateTime.UtcNow }
            };

            var githubItems = new List<AggregatedItem>
            {
                new() { Source = "GitHubAPI", Title = "Repo 1", Timestamp = DateTime.UtcNow }
            };

            var weatherItems = new List<AggregatedItem>
            {
                new() { Source = "WeatherAPI", Title = "Weather", Timestamp = DateTime.UtcNow }
            };

            mockNewsApi.Setup(x => x.FetchNewsAsync(keyword)).ReturnsAsync(newsItems);
            mockGitHubApi.Setup(x => x.FetchRepositoriesAsync(keyword)).ReturnsAsync(githubItems);
            mockWeatherApi.Setup(x => x.FetchWeatherDataAsync(It.IsAny<string?>())).ReturnsAsync(weatherItems);

            var service = new AggregatorService(mockNewsApi.Object, mockGitHubApi.Object, mockWeatherApi.Object);

            // Act
            var result = await service.AggregateAsync(keyword, sort, location, source, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Source == "NewsAPI");
            Assert.Contains(result, x => x.Source == "GitHubAPI");
            Assert.Contains(result, x => x.Source == "WeatherAPI");
        }

        [Fact]
        public async Task AggregateAsync_ShouldFilterBySource()
        {
            // Arrange
            var keyword = "test";
            var sortBy = "date";
            var location = "Athens";
            var source = "NewsAPI";

            var newsItems = new List<AggregatedItem>
            {
                new() { Source = "NewsAPI", Title = "News", Timestamp = DateTime.UtcNow }
            };
            var githubItems = new List<AggregatedItem>
            {
                new() { Source = "GitHubAPI", Title = "Repo", Timestamp = DateTime.UtcNow }
            };
            var weatherItems = new List<AggregatedItem>
            {
                new() { Source = "WeatherAPI", Title = "Weather", Timestamp = DateTime.UtcNow }
            };

            var mockNews = new Mock<INewsApiService>();
            var mockGitHub = new Mock<IGitHubApiService>();
            var mockWeather = new Mock<IWeatherApiService>();

            mockNews.Setup(x => x.FetchNewsAsync(keyword)).ReturnsAsync(newsItems);
            mockGitHub.Setup(x => x.FetchRepositoriesAsync(keyword)).ReturnsAsync(githubItems);
            mockWeather.Setup(x => x.FetchWeatherDataAsync(location)).ReturnsAsync(new List<AggregatedItem>());

            var service = new AggregatorService(mockNews.Object, mockGitHub.Object, mockWeather.Object);

            // Act
            var result = await service.AggregateAsync(keyword, sortBy, location, source, null, null);

            // Assert
            Assert.Single(result);
            Assert.Equal("NewsAPI", result.First().Source);
        }

        [Fact]
        public async Task Filter_ByFromAndToRange_ShouldReturnCorrectItems()
        {
            // Arrange
            var mockNewsApi = new Mock<INewsApiService>();
            var mockGitHubApi = new Mock<IGitHubApiService>();
            var mockWeatherApi = new Mock<IWeatherApiService>();

            var now = DateTime.UtcNow;

            var newsItems = new List<AggregatedItem>
            {
                new() { Source = "NewsAPI", Title = "News 1", Timestamp = now.AddMinutes(-30) },
                new() { Source = "NewsAPI", Title = "News 2", Timestamp = now.AddMinutes(-10) },
            };

            var githubItems = new List<AggregatedItem>
            {
                new() { Source = "GitHubAPI", Title = "Repo 1", Timestamp = now.AddMinutes(-20) }
            };

            var weatherItems = new List<AggregatedItem>
            {
                new() { Source = "WeatherAPI", Title = "Weather", Timestamp = now.AddMinutes(-5) }
            };

            mockNewsApi.Setup(x => x.FetchNewsAsync(null)).ReturnsAsync(newsItems);
            mockGitHubApi.Setup(x => x.FetchRepositoriesAsync(null)).ReturnsAsync(githubItems);
            mockWeatherApi.Setup(x => x.FetchWeatherDataAsync(null)).ReturnsAsync(weatherItems);

            var service = new AggregatorService(mockNewsApi.Object, mockGitHubApi.Object, mockWeatherApi.Object);

            var from = now.AddMinutes(-21);
            var to = now.AddMinutes(-9);

            // Act
            var result = await service.AggregateAsync(null, null, null, null, from, to);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Title == "Repo 1");
            Assert.Contains(result, x => x.Title == "News 2");
        }

        [Fact]
        public async Task NoFiltersProvided_ShouldReturnAllItems()
        {
            // Arrange
            var mockNewsApi = new Mock<INewsApiService>();
            var mockGitHubApi = new Mock<IGitHubApiService>();
            var mockWeatherApi = new Mock<IWeatherApiService>();

            var now = DateTime.UtcNow;

            var newsItems = new List<AggregatedItem>
            {
                new() { Source = "NewsAPI", Title = "News 1", Timestamp = now.AddMinutes(-30) },
            };

            var githubItems = new List<AggregatedItem>
            {
                new() { Source = "GitHubAPI", Title = "Repo 1", Timestamp = now.AddMinutes(-20) },
            };

            var weatherItems = new List<AggregatedItem>
            {
                new() { Source = "WeatherAPI", Title = "Weather Report", Timestamp = now.AddMinutes(-10) },
            };

            mockNewsApi.Setup(x => x.FetchNewsAsync(null)).ReturnsAsync(newsItems);
            mockGitHubApi.Setup(x => x.FetchRepositoriesAsync(null)).ReturnsAsync(githubItems);
            mockWeatherApi.Setup(x => x.FetchWeatherDataAsync(null)).ReturnsAsync(weatherItems);

            var service = new AggregatorService(mockNewsApi.Object, mockGitHubApi.Object, mockWeatherApi.Object);

            // Act
            var result = await service.AggregateAsync(null, null, null, null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Source == "NewsAPI");
            Assert.Contains(result, x => x.Source == "GitHubAPI");
            Assert.Contains(result, x => x.Source == "WeatherAPI");
        }
    }
}
