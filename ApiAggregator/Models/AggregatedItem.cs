namespace ApiAggregator.Models
{
    public class AggregatedItem
    {
        public string Source { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Url { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}
