using ApiAggregator.Models;

namespace ApiAggregator.Services.Aggregation
{
    public interface IAggregatorService
    {
        Task<List<AggregatedItem>> AggregateAsync(string? keyword, string? sortBy, string? location, string? source, DateTime? from, DateTime? to);
    }
}
