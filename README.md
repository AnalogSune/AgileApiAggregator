API Aggregator Service

Overview:

This project is a .NET 8-based API Aggregation Service that integrates multiple external APIs (GitHub, News, Weather) into a unified endpoint. It includes JWT authentication, caching, rate limiting, filtering, logging and performance anomaly detection.

----------------------------------------------------

Features:

- Aggregation Service for GitHub repositories, News headlines, and Weather reports
- Rate Limiting (10 requests/minute per IP)
- Performance Statistics with anomaly monitoring (background service)
- JWT Authentication
- Flexible Query Filtering: keyword, source, location, date range
- Caching for optimized performance
- Polly retry policies with fallback support
- Global exception handling
- Wrapper object for API responses
- Unit Tests with Moq

----------------------------------------------------

Requirements:

- .NET 8 SDK
- Visual Studio/ Rider / VS Code
- API keys for:
    - OpenWeatherMap
    - NewsAPI
    - GitHub

----------------------------------------------------

Setup Instructions:

1. Clone the repository from https://github.com/AnalogSune/AgileApiAggregator.git
2. Add your API keys to appsettings.json 


    "Jwt": {
    "Key": "YOUR_SUPER_SECRET_KEY_AT_LEAST_32_CHARS",
    "Issuer": "ApiAggregatorIssuer",
    "Audience": "ApiAggregatorClient"
    },
    "NewsApi": {
    "ApiKey": "YOUR_NEWS_API_KEY"
    },
    "GitHub": {
    "Token": "YOUR_GITHUB_KEY_HERE"
    },
    "OpenWeatherMap": {
    "ApiKey": "YOUR_WEATHER_API_KEY"
    }


3. Run the API
4. Open Swagger or any other similar tool. For Swagger, paste only the JWT token (no Bearer prefix) in the authorize box.

----------------------------------------------------

Authentication:

- POST /api/auth/login

Body (hard-coded credentials):

{
  "username": "admin",
  "password": "admin"
}

Response:

{
  "token": "<JWT-token>"
}

----------------------------------------------------

API Endpoints

1. GET /api/aggregate

Aggregates data from multiple sources based on optional filters.

Query Parameters:

- keyword - Search term (used in GitHub & News)
- sort - you can use either "source" or "date"
- location - City (for weather data)
- source - One of: GitHubAPI, NewsAPI, OpenWeatherMap
- from - Start datetime
- to - End datetime 

For exaple on params, check postman collection included in the project files (root directory).

2. GET /api/statistics

Returns the average performance statistics of each source.

----------------------------------------------------

Rate Limiting

- 10 requests per minute per IP
- Excess requests get 429 response

----------------------------------------------------

Testing

All tests are included in project "ApiAggregator.Tests" and you can run by running "dotnet test" in project's root directory.

Logic covered:
- Aggregator service
- JWT generation
- Filtering
- Error handling