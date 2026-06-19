using System.Text.Json.Serialization;

namespace RevenueRecognition.Api.Contracts.Revenue;

public sealed class NbpExchangeRateResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("rates")]
    public List<NbpRate> Rates { get; set; } = new();
}

public sealed class NbpRate
{
    [JsonPropertyName("no")]
    public string Number { get; set; } = null!;

    [JsonPropertyName("effectiveDate")]
    public DateTime EffectiveDate { get; set; }

    [JsonPropertyName("mid")]
    public decimal Mid { get; set; }
}