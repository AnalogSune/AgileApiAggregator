using ApiAggregator.Models;

namespace ApiAggregator.Services.External
{
    public interface IGitHubApiService
    {
        Task<List<AggregatedItem>> FetchRepositoriesAsync(string? keyword);
    }
}
