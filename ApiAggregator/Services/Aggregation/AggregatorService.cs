using ApiAggregator.Models;
using ApiAggregator.Services.External;
using System.Collections.Generic;

namespace ApiAggregator.Services.Aggregation
{
    public class AggregatorService : IAggregatorService
    {
        private readonly INewsApiService _newsService;
        private readonly IGitHubApiService _gitHubService;
        private readonly IWeatherApiService _weatherService;

        public AggregatorService(INewsApiService newsService, IGitHubApiService gitHubService, IWeatherApiService weatherService)
        {
            _newsService = newsService;
            _gitHubService = gitHubService;
            _weatherService = weatherService;
        }

        public async Task<List<AggregatedItem>> AggregateAsync(string? keyword, string? sortBy, string? location, string? source, DateTime? from, DateTime? to)
        {
            var allItems = new List<AggregatedItem>();

            var newsTask = _newsService.FetchNewsAsync(keyword);
            var gitHubTask = _gitHubService.FetchRepositoriesAsync(keyword);
            var weatherTask = _weatherService.FetchWeatherDataAsync(location);
            // Add more API tasks here

            await Task.WhenAll(newsTask, gitHubTask, weatherTask);

            allItems.AddRange(newsTask.Result);
            allItems.AddRange(gitHubTask.Result);
            allItems.AddRange(weatherTask.Result);

            // filtering on source
            if (!string.IsNullOrWhiteSpace(source))
                allItems = allItems
                    .Where(x => x.Source.Equals(source, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // filtering on date
            if (from.HasValue)
                allItems = allItems
                    .Where(x => x.Timestamp >= from.Value)
                    .ToList();

            if (to.HasValue)
                allItems = allItems
                    .Where(x => x.Timestamp <= to.Value)
                    .ToList();

            // sorting
            allItems = sortBy?.ToLower() switch
            {
                "date" => allItems.OrderByDescending(x => x.Timestamp).ToList(),
                "source" => allItems.OrderBy(x => x.Source).ToList(),
                _ => allItems
            };

            if (sortBy == "date")
                allItems = allItems.OrderByDescending(i => i.Timestamp).ToList();

            return allItems;
        }
    }
}
