using System.Net;
using System.Net.Http.Json;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Revenue;

namespace RevenueRecognition.Api.Services.Currency;

public sealed class NbpCurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NbpCurrencyService> _logger;

    public NbpCurrencyService(
        HttpClient httpClient,
        ILogger<NbpCurrencyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CurrencyConversionResult> ConvertFromPlnAsync(
        decimal amountInPln,
        string currencyCode)
    {
        var normalizedCurrency =
            currencyCode.Trim().ToUpperInvariant();

        if (normalizedCurrency == "PLN")
        {
            return new CurrencyConversionResult(
                "PLN",
                1m,
                decimal.Round(
                    amountInPln,
                    2,
                    MidpointRounding.AwayFromZero));
        }

        if (normalizedCurrency.Length != 3)
        {
            throw new BusinessRuleException(
                "Kod waluty musi składać się z trzech znaków, np. EUR.");
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"exchangerates/rates/a/{normalizedCurrency}/?format=json");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new BusinessRuleException(
                    $"Waluta {normalizedCurrency} nie jest obsługiwana przez NBP.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceException(
                    "Nie udało się pobrać kursu waluty z NBP.");
            }

            var exchangeRateResponse =
                await response.Content.ReadFromJsonAsync<
                    NbpExchangeRateResponse>();

            var rate = exchangeRateResponse?
                .Rates
                .FirstOrDefault()?
                .Mid;

            if (rate is null or <= 0m)
            {
                throw new ExternalServiceException(
                    "NBP zwrócił nieprawidłowy kurs waluty.");
            }

            // Kurs NBP oznacza liczbę PLN za jedną jednostkę waluty.
            // Dlatego PLN dzielimy przez kurs.
            var convertedAmount =
                decimal.Round(
                    amountInPln / rate.Value,
                    2,
                    MidpointRounding.AwayFromZero);

            return new CurrencyConversionResult(
                normalizedCurrency,
                rate.Value,
                convertedAmount);
        }
        catch (AppException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Błąd komunikacji z API NBP.");

            throw new ExternalServiceException(
                "Nie można połączyć się z usługą kursów walut NBP.");
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(
                exception,
                "Przekroczono czas oczekiwania na odpowiedź NBP.");

            throw new ExternalServiceException(
                "Przekroczono czas oczekiwania na kurs waluty.");
        }
    }
}