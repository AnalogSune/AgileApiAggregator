using ApiAggregator.Models;
using ApiAggregator.Services.Statistics;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace ApiAggregator.Services.External
{
    public class WeatherApiService : IWeatherApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IStatisticsService _stats;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeatherApiService> _logger;

        public WeatherApiService(HttpClient httpClient, IConfiguration config, IStatisticsService stats, IMemoryCache cache, ILogger<WeatherApiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["OpenWeatherMap:ApiKey"] ?? throw new ArgumentNullException("WeatherApiKey is missing");
            _stats = stats;
            _cache = cache;
            _logger = logger;
        }
        public async Task<List<AggregatedItem>> FetchWeatherDataAsync(string? location)
        {
            string cacheKey = $"WeatherAPI:{location}";

            if (_cache.TryGetValue(cacheKey, out List<AggregatedItem>? cached) && cached is not null)
            {
                _logger.LogInformation($"[WeatherAPI] Cache hit for '{location}'");
                return cached;
            }

            //location ??= "Athens";
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={location}&appid={_apiKey}";

            _logger.LogInformation("Url for OpenWeatherMap: " + url);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();

            _stats.Record("OpenWeatherMap", stopwatch.Elapsed);

            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[WeatherAPI] Error: {(int)response.StatusCode} {response.ReasonPhrase}");
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[WeatherAPI] Body: {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<WeatherResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Weather?.Count > 0 && result.Main != null)
            {
                var items = new List<AggregatedItem>
                {
                    new AggregatedItem
                    {
                        Source = "OpenWeatherMap",
                        Title = $"Weather in {location}: {result.Weather[0].Main}, {result.Main.Temp}°C",
                        Url = $"https://openweathermap.org/city/{result.Id}",
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(result.Dt).DateTime
                    }
                };

                _cache.Set(cacheKey, items, TimeSpan.FromMinutes(2));
                return items;
            }
            else
            {
                _logger.LogWarning("Incomplete weather data received.");
                return new List<AggregatedItem>();
            }
        }

        private class WeatherResponse
        {
            public int Id { get; set; }
            public List<WeatherCondition>? Weather { get; set; }
            public MainInfo? Main { get; set; }
            public long Dt { get; set; }
        }

        private class WeatherCondition
        {
            public string Main { get; set; } = null!;
            public string Description { get; set; } = null!;
        }

        private class MainInfo
        {
            public float Temp { get; set; }
        }
    }
}
