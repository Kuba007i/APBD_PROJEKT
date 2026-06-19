using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Contracts;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Domain.Enums;
using RevenueRecognition.Api.Services.Contracts;
using RevenueRecognition.Tests.Helpers;
using Xunit;

namespace RevenueRecognition.Tests.Services.Contracts;

public sealed class ContractServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenPeriodIsShorterThanThreeDays_ShouldThrowBusinessRuleException()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var service =
            new ContractService(dbContext);

        var startDate =
            DateTime.UtcNow.Date;

        var request =
            new CreateContractRequest
            {
                ClientId = 1,
                SoftwareId = 1,
                StartDate = startDate,
                EndDate = startDate.AddDays(2),
                AdditionalSupportYears = 0
            };

        // Act
        var action = async () =>
            await service.CreateAsync(request);

        // Assert
        var exception =
            await Assert.ThrowsAsync<
                BusinessRuleException>(action);

        Assert.Equal(
            "Okres na opłacenie kontraktu musi wynosić od 3 do 30 dni.",
            exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldUseHighestActiveDiscount()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var client =
            CreateIndividualClient();

        var software =
            CreateSoftware(
                name: "FinTrack",
                price: 12000m);

        dbContext.IndividualClients.Add(client);
        dbContext.SoftwareProducts.Add(software);

        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;

        dbContext.Discounts.AddRange(
            new Discount
            {
                Name = "Discount 10%",
                Percentage = 10m,
                ValidFrom = now.AddDays(-1),
                ValidTo = now.AddDays(1),
                Target = DiscountTarget.License,
                SoftwareId = software.Id
            },
            new Discount
            {
                Name = "Discount 15%",
                Percentage = 15m,
                ValidFrom = now.AddDays(-1),
                ValidTo = now.AddDays(1),
                Target = DiscountTarget.License,
                SoftwareId = software.Id
            },
            new Discount
            {
                Name = "Expired discount 50%",
                Percentage = 50m,
                ValidFrom = now.AddDays(-10),
                ValidTo = now.AddDays(-5),
                Target = DiscountTarget.License,
                SoftwareId = software.Id
            });

        await dbContext.SaveChangesAsync();

        var service =
            new ContractService(dbContext);

        var startDate =
            DateTime.UtcNow.Date;

        var request =
            new CreateContractRequest
            {
                ClientId = client.Id,
                SoftwareId = software.Id,
                StartDate = startDate,
                EndDate = startDate.AddDays(10),
                AdditionalSupportYears = 2
            };

        // Act
        var response =
            await service.CreateAsync(request);

        // Assert
        Assert.Equal(
            15m,
            response.ProductDiscountPercentage);

        Assert.Equal(
            0m,
            response.ReturningCustomerDiscountPercentage);

        Assert.Equal(
            12000m,
            response.BasePrice);

        Assert.Equal(
            2000m,
            response.SupportCost);

        Assert.Equal(
            2,
            response.AdditionalSupportYears);

        Assert.Equal(
            3,
            response.IncludedUpdateYears);

        // 12000 + 2000 = 14000
        // 14000 - 15% = 11900
        Assert.Equal(
            11900m,
            response.FinalPrice);

        Assert.Equal(
            ContractStatus.Pending.ToString(),
            response.Status);
    }

    [Fact]
    public async Task CreateAsync_ForReturningCustomer_ShouldApplyAdditionalFivePercentDiscount()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var client =
            CreateIndividualClient();

        var previousSoftware =
            CreateSoftware(
                name: "Old Product",
                price: 5000m);

        var newSoftware =
            CreateSoftware(
                name: "New Product",
                price: 10000m);

        dbContext.IndividualClients.Add(client);

        dbContext.SoftwareProducts.AddRange(
            previousSoftware,
            newSoftware);

        await dbContext.SaveChangesAsync();

        dbContext.Contracts.Add(
            new Contract
            {
                ClientId = client.Id,
                SoftwareId = previousSoftware.Id,
                SoftwareVersion =
                    previousSoftware.CurrentVersion,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(-2)
                    .AddDays(10),
                BasePrice = 5000m,
                AdditionalSupportYears = 0,
                SupportCost = 0m,
                ProductDiscountPercentage = 0m,
                ReturningCustomerDiscountPercentage = 0m,
                FinalPrice = 5000m,
                Status = ContractStatus.Signed,
                SignedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatesUntil = DateTime.UtcNow.AddMonths(10),
                IsDeleted = false
            });

        await dbContext.SaveChangesAsync();

        var service =
            new ContractService(dbContext);

        var startDate =
            DateTime.UtcNow.Date;

        var request =
            new CreateContractRequest
            {
                ClientId = client.Id,
                SoftwareId = newSoftware.Id,
                StartDate = startDate,
                EndDate = startDate.AddDays(10),
                AdditionalSupportYears = 0
            };

        // Act
        var response =
            await service.CreateAsync(request);

        // Assert
        Assert.Equal(
            0m,
            response.ProductDiscountPercentage);

        Assert.Equal(
            5m,
            response.ReturningCustomerDiscountPercentage);

        // 10000 - 5% = 9500
        Assert.Equal(
            9500m,
            response.FinalPrice);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveContractExists_ShouldThrowConflictException()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var client =
            CreateIndividualClient();

        var software =
            CreateSoftware(
                name: "FinTrack",
                price: 12000m);

        dbContext.IndividualClients.Add(client);
        dbContext.SoftwareProducts.Add(software);

        await dbContext.SaveChangesAsync();

        dbContext.Contracts.Add(
            new Contract
            {
                ClientId = client.Id,
                SoftwareId = software.Id,
                SoftwareVersion =
                    software.CurrentVersion,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(10),
                BasePrice = 12000m,
                AdditionalSupportYears = 0,
                SupportCost = 0m,
                ProductDiscountPercentage = 0m,
                ReturningCustomerDiscountPercentage = 0m,
                FinalPrice = 12000m,
                Status = ContractStatus.Pending,
                IsDeleted = false
            });

        await dbContext.SaveChangesAsync();

        var service =
            new ContractService(dbContext);

        var startDate =
            DateTime.UtcNow.Date;

        var request =
            new CreateContractRequest
            {
                ClientId = client.Id,
                SoftwareId = software.Id,
                StartDate = startDate,
                EndDate = startDate.AddDays(10),
                AdditionalSupportYears = 0
            };

        // Act
        var action = async () =>
            await service.CreateAsync(request);

        // Assert
        var exception =
            await Assert.ThrowsAsync<
                ConflictException>(action);

        Assert.Equal(
            "Klient ma już aktywną ofertę albo umowę na wybrane oprogramowanie.",
            exception.Message);

        Assert.Equal(
            1,
            await dbContext.Contracts.CountAsync());
    }

    private static IndividualClient CreateIndividualClient()
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
        string name,
        decimal price)
    {
        return new Software
        {
            Name = name,
            Description = $"Opis produktu {name}",
            CurrentVersion = "1.0",
            Category = "Business",
            YearlyLicensePrice = price
        };
    }
}