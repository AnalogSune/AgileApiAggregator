using ApiAggregator.Models;

namespace ApiAggregator.Services.External
{
    public interface INewsApiService
    {
        Task<List<AggregatedItem>> FetchNewsAsync(string? keyword);
    }
}
