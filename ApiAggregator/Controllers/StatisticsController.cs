using ApiAggregator.Models;
using ApiAggregator.Services.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _stats;

        public StatisticsController(IStatisticsService stats)
        {
            _stats = stats;
        }

        [HttpGet]
        public ActionResult<ApiResponse<Dictionary<string, RequestTotalStats>>> Get()
        {
            var statistics = _stats.GetStatistics();
            return Ok(ApiResponse<Dictionary<string, RequestTotalStats>>.SuccessResponse(statistics));
        }
    }
}
