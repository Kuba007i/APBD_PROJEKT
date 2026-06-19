using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Data;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Domain.Enums;
using RevenueRecognition.Api.Services.Currency;
using RevenueRecognition.Api.Services.Revenue;
using RevenueRecognition.Tests.Helpers;
using Xunit;

namespace RevenueRecognition.Tests.Services.Revenue;

public sealed class RevenueServiceTests
{
    [Fact]
    public async Task GetCurrentRevenueAsync_ShouldIncludeOnlySignedContracts()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var data = await SeedClientAndSoftwareAsync(
            dbContext,
            "FinTrack");

        dbContext.Contracts.AddRange(
            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Signed,
                finalPrice: 1000m),

            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Pending,
                finalPrice: 2000m),

            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Expired,
                finalPrice: 3000m));

        await dbContext.SaveChangesAsync();

        var currencyService =
            new FakeCurrencyService();

        var service =
            new RevenueService(
                dbContext,
                currencyService);

        // Act
        var response =
            await service.GetCurrentRevenueAsync(
                softwareId: null,
                currency: "PLN");

        // Assert
        Assert.Equal("Current", response.RevenueType);
        Assert.Equal(1000m, response.AmountInPln);
        Assert.Equal("PLN", response.Currency);
        Assert.Equal(1m, response.ExchangeRate);
        Assert.Equal(1000m, response.ConvertedAmount);
    }

    [Fact]
    public async Task GetExpectedRevenueAsync_ShouldIncludeSignedAndActivePendingContracts()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var data = await SeedClientAndSoftwareAsync(
            dbContext,
            "FinTrack");

        dbContext.Contracts.AddRange(
            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Signed,
                finalPrice: 1000m),

            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Pending,
                finalPrice: 2000m,
                endDate: DateTime.UtcNow.AddDays(10)),

            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Pending,
                finalPrice: 3000m,
                endDate: DateTime.UtcNow.AddDays(-1)),

            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Expired,
                finalPrice: 4000m),

            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Pending,
                finalPrice: 5000m,
                endDate: DateTime.UtcNow.AddDays(10),
                isDeleted: true));

        await dbContext.SaveChangesAsync();

        var service =
            new RevenueService(
                dbContext,
                new FakeCurrencyService());

        // Act
        var response =
            await service.GetExpectedRevenueAsync(
                softwareId: null,
                currency: "PLN");

        // Assert
        // Podpisany: 1000
        // Aktywna oferta: 2000
        // Razem: 3000
        Assert.Equal("Expected", response.RevenueType);
        Assert.Equal(3000m, response.AmountInPln);
        Assert.Equal(3000m, response.ConvertedAmount);
    }

    [Fact]
    public async Task GetCurrentRevenueAsync_WithSoftwareId_ShouldFilterByProduct()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var client =
            CreateIndividualClient();

        var firstSoftware =
            CreateSoftware("FinTrack");

        var secondSoftware =
            CreateSoftware("EduManager");

        dbContext.IndividualClients.Add(client);

        dbContext.SoftwareProducts.AddRange(
            firstSoftware,
            secondSoftware);

        await dbContext.SaveChangesAsync();

        dbContext.Contracts.AddRange(
            CreateContract(
                client.Id,
                firstSoftware.Id,
                ContractStatus.Signed,
                finalPrice: 12000m),

            CreateContract(
                client.Id,
                secondSoftware.Id,
                ContractStatus.Signed,
                finalPrice: 8000m));

        await dbContext.SaveChangesAsync();

        var service =
            new RevenueService(
                dbContext,
                new FakeCurrencyService());

        // Act
        var response =
            await service.GetCurrentRevenueAsync(
                firstSoftware.Id,
                "PLN");

        // Assert
        Assert.Equal(firstSoftware.Id, response.SoftwareId);
        Assert.Equal("FinTrack", response.SoftwareName);
        Assert.Equal(12000m, response.AmountInPln);
    }

    [Fact]
    public async Task GetCurrentRevenueAsync_WithEuroCurrency_ShouldReturnConvertedAmount()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var data = await SeedClientAndSoftwareAsync(
            dbContext,
            "FinTrack");

        dbContext.Contracts.Add(
            CreateContract(
                data.Client.Id,
                data.Software.Id,
                ContractStatus.Signed,
                finalPrice: 800m));

        await dbContext.SaveChangesAsync();

        var currencyService =
            new FakeCurrencyService(
                exchangeRate: 4m);

        var service =
            new RevenueService(
                dbContext,
                currencyService);

        // Act
        var response =
            await service.GetCurrentRevenueAsync(
                softwareId: null,
                currency: "EUR");

        // Assert
        Assert.Equal(800m, response.AmountInPln);
        Assert.Equal("EUR", response.Currency);
        Assert.Equal(4m, response.ExchangeRate);
        Assert.Equal(200m, response.ConvertedAmount);

        Assert.Equal("EUR", currencyService.LastCurrencyCode);
        Assert.Equal(800m, currencyService.LastAmountInPln);
    }

    private static async Task<RevenueTestData>
        SeedClientAndSoftwareAsync(
            AppDbContext dbContext,
            string softwareName)
    {
        var client =
            CreateIndividualClient();

        var software =
            CreateSoftware(softwareName);

        dbContext.IndividualClients.Add(client);
        dbContext.SoftwareProducts.Add(software);

        await dbContext.SaveChangesAsync();

        return new RevenueTestData(
            client,
            software);
    }

    private static Contract CreateContract(
        int clientId,
        int softwareId,
        ContractStatus status,
        decimal finalPrice,
        DateTime? endDate = null,
        bool isDeleted = false)
    {
        var now = DateTime.UtcNow;

        return new Contract
        {
            ClientId = clientId,
            SoftwareId = softwareId,
            SoftwareVersion = "1.0",
            CreatedAt = now.AddDays(-1),
            StartDate = now.AddDays(-1),
            EndDate = endDate ?? now.AddDays(10),
            BasePrice = finalPrice,
            AdditionalSupportYears = 0,
            SupportCost = 0m,
            ProductDiscountPercentage = 0m,
            ReturningCustomerDiscountPercentage = 0m,
            FinalPrice = finalPrice,
            Status = status,
            SignedAt =
                status == ContractStatus.Signed
                    ? now
                    : null,
            UpdatesUntil =
                status == ContractStatus.Signed
                    ? now.AddYears(1)
                    : null,
            IsDeleted = isDeleted,
            DeletedAt =
                isDeleted
                    ? now
                    : null
        };
    }

    private static IndividualClient
        CreateIndividualClient()
    {
        return new IndividualClient
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Address = "Warszawa",
            Email = "jan@example.com",
            PhoneNumber = "500600700",
            Pesel = Guid.NewGuid()
                .ToString("N")[..11]
        };
    }

    private static Software CreateSoftware(
        string name)
    {
        return new Software
        {
            Name = name,
            Description = $"Opis produktu {name}",
            CurrentVersion = "1.0",
            Category = "Business",
            YearlyLicensePrice = 10000m
        };
    }

    private sealed record RevenueTestData(
        IndividualClient Client,
        Software Software);

    private sealed class FakeCurrencyService
        : ICurrencyService
    {
        private readonly decimal _exchangeRate;

        public FakeCurrencyService(
            decimal exchangeRate = 1m)
        {
            _exchangeRate = exchangeRate;
        }

        public string? LastCurrencyCode { get; private set; }

        public decimal? LastAmountInPln { get; private set; }

        public Task<CurrencyConversionResult>
            ConvertFromPlnAsync(
                decimal amountInPln,
                string currencyCode)
        {
            LastAmountInPln = amountInPln;

            LastCurrencyCode =
                currencyCode
                    .Trim()
                    .ToUpperInvariant();

            var convertedAmount =
                decimal.Round(
                    amountInPln / _exchangeRate,
                    2,
                    MidpointRounding.AwayFromZero);

            return Task.FromResult(
                new CurrencyConversionResult(
                    LastCurrencyCode,
                    _exchangeRate,
                    convertedAmount));
        }
    }
}