using ApiAggregator.Models;
using ApiAggregator.Services.Statistics;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace ApiAggregator.Services.External
{
    public class NewsApiService : INewsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IStatisticsService _stats;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NewsApiService> _logger;

        public NewsApiService(HttpClient httpClient, IConfiguration config, IStatisticsService stats, IMemoryCache cache, ILogger<NewsApiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["NewsApi:ApiKey"] ?? throw new ArgumentNullException("News API key is missing");
            _stats = stats;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<AggregatedItem>> FetchNewsAsync(string? keyword)
        {
            string cacheKey = $"NewsAPI:{keyword}";

            if (_cache.TryGetValue(cacheKey, out List<AggregatedItem>? cached) && cached is not null)
            {
                _logger.LogInformation($"[NewsAPI] Cache hit for '{keyword}'");
                return cached;
            }

            var url = $"https://newsapi.org/v2/top-headlines?q={keyword ?? "tech"}&apiKey={_apiKey}";

            _logger.LogInformation("Url for NewsAPI: " + url);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            _stats.Record("NewsAPI", stopwatch.Elapsed);

            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[NewsAPI] Error: {(int)response.StatusCode} {response.ReasonPhrase}");
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[NewsAPI] Body: {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<NewsApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var items = result?.Articles.Select(a => new AggregatedItem
            {
                Source = "NewsAPI",
                Title = a.Title,
                Url = a.Url,
                Timestamp = a.PublishedAt
            }).ToList() ?? new List<AggregatedItem>();

            // cache the result for 2 minutes
            _cache.Set(cacheKey, items, TimeSpan.FromMinutes(2)); 
            return items;
        }

        private class NewsApiResponse
        {
            public List<Article> Articles { get; set; } = new();
        }

        private class Article
        {
            public string Title { get; set; } = null!;
            public string Url { get; set; } = null!;
            public DateTime PublishedAt { get; set; }
        }
    }
}
