using ApiAggregator.Jwt;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ApiAggregator.Tests
{
    public class JwtServiceTests
    {
        private readonly JwtService _jwtService;
        private readonly IConfiguration _config;

        public JwtServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Key", "ThisIsASuperLongSecureKeyThatIsAtLeast32Chars"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            _jwtService = new JwtService(_config);
        }

        [Fact]
        public void GenerateToken_ShouldContainCorrectClaimsAndExpiry()
        {
            // Arrange
            var username = "testuser";

            // Act
            var token = _jwtService.GenerateToken(username);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            Assert.NotNull(jwtToken);
            Assert.Equal("TestIssuer", jwtToken.Issuer);
            Assert.Contains(jwtToken.Audiences, a => a == "TestAudience");

            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            Assert.NotNull(nameClaim);
            Assert.Equal(username, nameClaim!.Value);

            Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
            Assert.True(jwtToken.ValidTo <= DateTime.UtcNow.AddHours(1).AddMinutes(1)); // small margin
        }
    }
}
