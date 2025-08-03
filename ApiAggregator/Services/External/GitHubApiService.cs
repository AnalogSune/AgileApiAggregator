using ApiAggregator.Models;
using ApiAggregator.Services.Statistics;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiAggregator.Services.External
{
    public class GitHubApiService : IGitHubApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IStatisticsService _stats;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GitHubApiService> _logger;

        public GitHubApiService(HttpClient httpClient, IStatisticsService stats, IMemoryCache cache, ILogger<GitHubApiService> logger)
        {
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("ApiAggregator", "1.0"));
            _stats = stats;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<AggregatedItem>> FetchRepositoriesAsync(string? keyword)
        {
            string cacheKey = $"GitHubAPI:{keyword}";

            if (_cache.TryGetValue(cacheKey, out List<AggregatedItem>? cached) && cached is not null)
            {
                _logger.LogInformation($"[GitHubAPI] Cache hit for '{keyword}'");
                return cached;
            }

            var query = string.IsNullOrWhiteSpace(keyword) ? "dotnet" : keyword;
            var url = $"https://api.github.com/search/repositories?q={query}&sort=stars";

            _logger.LogInformation("Url for GitHubAPI: " + url);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            _stats.Record("GitHub", stopwatch.Elapsed);

            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[GitHubAPI] Error: {(int)response.StatusCode} {response.ReasonPhrase}");
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[GitHubAPI] Body: {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<GitHubResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var items = result?.Items.Select(repo => new AggregatedItem
            {
                Source = "GitHub",
                Title = repo.FullName,
                Url = repo.HtmlUrl,
                Timestamp = repo.UpdatedAt
            }).ToList() ?? new List<AggregatedItem>();

            // cache the result for 2 minutes
            _cache.Set(cacheKey, items, TimeSpan.FromMinutes(2));
            return items;
        }

        private class GitHubResponse
        {
            [JsonPropertyName("items")]
            public List<Repo> Items { get; set; } = new();
        }

        private class Repo
        {
            [JsonPropertyName("full_name")]
            public string FullName { get; set; } = null!;

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = null!;

            [JsonPropertyName("updated_at")]
            public DateTime UpdatedAt { get; set; }
        }
    }
}
