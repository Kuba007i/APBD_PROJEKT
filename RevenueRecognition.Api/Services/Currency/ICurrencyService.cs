namespace RevenueRecognition.Api.Services.Currency;

public interface ICurrencyService
{
    Task<CurrencyConversionResult> ConvertFromPlnAsync(
        decimal amountInPln,
        string currencyCode);
}

public sealed record CurrencyConversionResult(
    string Currency,
    decimal ExchangeRate,
    decimal ConvertedAmount);