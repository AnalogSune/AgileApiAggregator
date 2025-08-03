using ApiAggregator.Models;
using ApiAggregator.Services.Aggregation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AggregateController : ControllerBase
    {
        private readonly IAggregatorService _aggregatorService;

        public AggregateController(IAggregatorService aggregatorService)
        {
            _aggregatorService = aggregatorService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AggregatedItem>>>> Get(
            [FromQuery] string? keyword,
            [FromQuery] string? sort,
            [FromQuery] string? location,
            [FromQuery] string? source,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            if (from != null && to != null && from > to)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid date range: 'from' is after 'to'"));
            }

            var results = await _aggregatorService.AggregateAsync(keyword, sort, location, source, from, to);

            return Ok(ApiResponse<List<AggregatedItem>>.SuccessResponse(results));
        }
    }
}
