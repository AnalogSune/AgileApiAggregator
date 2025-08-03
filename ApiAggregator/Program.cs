using ApiAggregator.Jwt;
using ApiAggregator.Middleware;
using ApiAggregator.Models;
using ApiAggregator.Services.Aggregation;
using ApiAggregator.Services.External;
using ApiAggregator.Services.Statistics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicyWithFallback()
{
    return Policy.WrapAsync(
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .FallbackAsync(
                fallbackValue: new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"articles\":[]}")
                },
                onFallbackAsync: async (response, context) =>
                {
                    Console.WriteLine("[Polly] Fallback triggered for API");
                    await Task.CompletedTask;
                }),
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(500 * i))
    );
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// JWT Authentication setup
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsASuperLongSecureKeyThatIsAtLeast32Chars";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ApiAggregatorIssuer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ApiAggregatorClient";

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var users = new List<User>
{
    new User { Username = "admin", Password = "admin" }
};

builder.Services.AddSingleton(users);
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAggregatorService, AggregatorService>();
builder.Services.AddHostedService<PerformanceAnomalyLoggerService>();

builder.Services.AddHttpClient<INewsApiService, NewsApiService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ApiAggregator/1.0");
}).AddPolicyHandler(GetRetryPolicyWithFallback());

builder.Services.AddHttpClient<IGitHubApiService, GitHubApiService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ApiAggregator/1.0");
}).AddPolicyHandler(GetRetryPolicyWithFallback());

builder.Services.AddHttpClient<IWeatherApiService, WeatherApiService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ApiAggregator/1.0");
}).AddPolicyHandler(GetRetryPolicyWithFallback());

builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
builder.Services.AddMemoryCache();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Aggregator",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, // max requests
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication(); // JWT
app.UseAuthorization();

app.MapControllers();

app.Run();
