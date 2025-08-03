using ApiAggregator.Models;
using System.Net;
using System.Text.Json;

namespace ApiAggregator.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access");

                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await WriteResponse(context, ApiResponse<string>.Fail("Unauthorized"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input");

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await WriteResponse(context, ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteResponse(context, ApiResponse<string>.Fail("An unexpected error occurred."));
            }
        }

        private async Task WriteResponse(HttpContext context, ApiResponse<string> response)
        {
            context.Response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}
