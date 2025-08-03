using ApiAggregator.Models;

namespace ApiAggregator.Services.External
{
    public interface IWeatherApiService
    {
        Task<List<AggregatedItem>> FetchWeatherDataAsync(string? location);
    }
}
