using ApiAggregator.DTOs;
using ApiAggregator.Jwt;
using ApiAggregator.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly List<User> _users;
        private readonly IJwtService _jwtService;

        public AuthController(List<User> users, IJwtService jwtService)
        {
            _users = users;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequestDto request)
        {
            var user = _users.FirstOrDefault(u =>
                u.Username == request.Username &&
                u.Password == request.Password);

            if (user is null)
            {
                return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
            }

            var token = _jwtService.GenerateToken(user.Username);
            var response = new AuthResponseDto { Token = token };

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(response));
        }
    }
}
