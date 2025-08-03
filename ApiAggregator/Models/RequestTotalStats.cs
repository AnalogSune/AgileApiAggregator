namespace ApiAggregator.Models
{
    public class RequestTotalStats
    {
        public int TotalRequests { get; set; }
        public double AverageMs { get; set; }
        public int FastCount { get; set; }
        public int AverageCount { get; set; }
        public int SlowCount { get; set; }
    }
}
