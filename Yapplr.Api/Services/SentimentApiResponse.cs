using System.Text.Json.Serialization;

namespace Yapplr.Api.Services;

public class SentimentApiResponse
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}
