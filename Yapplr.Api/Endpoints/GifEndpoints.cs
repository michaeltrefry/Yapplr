using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace Yapplr.Api.Endpoints;

public static class GifEndpoints
{
    private const string TenorBaseUrl = "https://tenor.googleapis.com/v2/";
    private const string TenorClient = "yapplr";
    public static void MapGifEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/gif")
            .WithTags("GIF")
            .RequireAuthorization();

        group.MapGet("/search", SearchGifs)
            .WithName("SearchGifs")
            .WithSummary("Search for GIFs using Tenor API")
            .WithDescription("Search for GIFs by query string using the Tenor API");

        group.MapGet("/trending", GetTrendingGifs)
            .WithName("GetTrendingGifs")
            .WithSummary("Get trending GIFs from Tenor API")
            .WithDescription("Get currently trending GIFs from the Tenor API");

        group.MapGet("/categories", GetGifCategories)
            .WithName("GetGifCategories")
            .WithSummary("Get GIF categories from Tenor API")
            .WithDescription("Get available GIF categories from the Tenor API");
    }

    private static async Task<IResult> SearchGifs(
        string q,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Program> logger,
        int limit = 20,
        string? pos = null)
    {
        try
        {
            var apiKey = configuration["Tenor:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("Tenor API key is not configured");
                return Results.Problem("Tenor API key is not configured", statusCode: 500);
            }
            
            var queryParams = new List<string>
            {
                $"key={apiKey}",
                $"q={Uri.EscapeDataString(q)}",
                $"limit={limit}",
                "media_filter=gif,tinygif",
                "ar_range=all",
                $"client={TenorClient}"
            };

            if (!string.IsNullOrEmpty(pos))
            {
                queryParams.Add($"pos={Uri.EscapeDataString(pos)}");
            }

            var url = $"{TenorBaseUrl}search?{string.Join("&", queryParams)}";
            
            logger.LogInformation("Fetching GIFs from Tenor: {Url}", url);
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Tenor API error: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return Results.Problem($"Failed to fetch GIFs from Tenor: {response.ReasonPhrase}", 
                    statusCode: (int)response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            
            return Results.Ok(jsonDocument.RootElement);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching GIFs");
            return Results.Problem("Internal server error while searching GIFs", statusCode: 500);
        }
    }

    private static async Task<IResult> GetTrendingGifs(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Program> logger,
        int limit = 20,
        string? pos = null)
    {
        try
        {
            var apiKey = configuration["Tenor:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("Tenor API key is not configured");
                return Results.Problem("Tenor API key is not configured", statusCode: 500);
            }
            
            var queryParams = new List<string>
            {
                $"key={apiKey}",
                $"limit={limit}",
                "media_filter=gif,tinygif",
                "ar_range=all",
                $"client={TenorClient}"
            };

            if (!string.IsNullOrEmpty(pos))
            {
                queryParams.Add($"pos={Uri.EscapeDataString(pos)}");
            }

            var url = $"{TenorBaseUrl}featured?{string.Join("&", queryParams)}";
            
            logger.LogInformation("Fetching trending GIFs from Tenor: {Url}", url);
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Tenor API error: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return Results.Problem($"Failed to fetch trending GIFs from Tenor: {response.ReasonPhrase}", 
                    statusCode: (int)response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            
            return Results.Ok(jsonDocument.RootElement);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching trending GIFs");
            return Results.Problem("Internal server error while fetching trending GIFs", statusCode: 500);
        }
    }

    private static async Task<IResult> GetGifCategories(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        try
        {
            var apiKey = configuration["Tenor:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("Tenor API key is not configured");
                return Results.Problem("Tenor API key is not configured", statusCode: 500);
            }
            
            var queryParams = new List<string>
            {
                $"key={apiKey}",
                "type=featured",
                $"client={TenorClient}"
            };

            var url = $"{TenorBaseUrl}categories?{string.Join("&", queryParams)}";
            
            logger.LogInformation("Fetching GIF categories from Tenor: {Url}", url);
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Tenor API error: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return Results.Problem($"Failed to fetch GIF categories from Tenor: {response.ReasonPhrase}", 
                    statusCode: (int)response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            
            return Results.Ok(jsonDocument.RootElement);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching GIF categories");
            return Results.Problem("Internal server error while fetching GIF categories", statusCode: 500);
        }
    }
}
