using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Payments;
using RevenueRecognition.Api.Data;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Domain.Enums;
using RevenueRecognition.Api.Services.Payments;
using RevenueRecognition.Tests.Helpers;
using Xunit;

namespace RevenueRecognition.Tests.Services.Payments;

public sealed class ContractPaymentServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenPaidInInstallments_ShouldSignContractAfterFullPayment()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var data = await SeedContractAsync(
            dbContext,
            finalPrice: 10000m,
            additionalSupportYears: 1);

        var service =
            new ContractPaymentService(dbContext);

        // Act — pierwsza rata
        var firstPayment =
            await service.CreateAsync(
                data.Contract.Id,
                new CreateContractPaymentRequest
                {
                    ClientId = data.Client.Id,
                    Amount = 4000m
                });

        // Assert — kontrakt nadal oczekuje na płatność
        Assert.Equal(
            4000m,
            firstPayment.TotalPaid);

        Assert.Equal(
            6000m,
            firstPayment.RemainingAmount);

        Assert.Equal(
            ContractStatus.Pending.ToString(),
            firstPayment.ContractStatus);

        Assert.Null(firstPayment.SignedAt);
        Assert.Null(firstPayment.UpdatesUntil);

        var beforeSecondPayment =
            DateTime.UtcNow;

        // Act — druga rata kończy płatność
        var secondPayment =
            await service.CreateAsync(
                data.Contract.Id,
                new CreateContractPaymentRequest
                {
                    ClientId = data.Client.Id,
                    Amount = 6000m
                });

        var afterSecondPayment =
            DateTime.UtcNow;

        // Assert — kontrakt został podpisany
        Assert.Equal(
            10000m,
            secondPayment.TotalPaid);

        Assert.Equal(
            0m,
            secondPayment.RemainingAmount);

        Assert.Equal(
            ContractStatus.Signed.ToString(),
            secondPayment.ContractStatus);

        Assert.NotNull(secondPayment.SignedAt);
        Assert.NotNull(secondPayment.UpdatesUntil);

        // 1 rok podstawowy + 1 rok dodatkowy
        Assert.InRange(
            secondPayment.UpdatesUntil!.Value,
            beforeSecondPayment.AddYears(2),
            afterSecondPayment.AddYears(2));

        var savedContract =
            await dbContext.Contracts
                .Include(contract => contract.Payments)
                .SingleAsync(contract =>
                    contract.Id == data.Contract.Id);

        Assert.Equal(
            ContractStatus.Signed,
            savedContract.Status);

        Assert.Equal(
            2,
            savedContract.Payments.Count);

        Assert.Equal(
            10000m,
            savedContract.Payments.Sum(
                payment => payment.Amount));

        Assert.All(
            savedContract.Payments,
            payment => Assert.False(payment.IsRefunded));
    }

    [Fact]
    public async Task CreateAsync_WhenPaymentExceedsRemainingAmount_ShouldThrowBusinessRuleException()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var data = await SeedContractAsync(
            dbContext,
            finalPrice: 10000m);

        var service =
            new ContractPaymentService(dbContext);

        await service.CreateAsync(
            data.Contract.Id,
            new CreateContractPaymentRequest
            {
                ClientId = data.Client.Id,
                Amount = 7000m
            });

        // Act
        var action = async () =>
            await service.CreateAsync(
                data.Contract.Id,
                new CreateContractPaymentRequest
                {
                    ClientId = data.Client.Id,
                    Amount = 4000m
                });

        // Assert
        var exception =
            await Assert.ThrowsAsync<
                BusinessRuleException>(action);

        Assert.Contains(
            "Kwota płatności przekracza pozostałą kwotę kontraktu",
            exception.Message);

        var payments =
            await dbContext.ContractPayments
                .Where(payment =>
                    payment.ContractId == data.Contract.Id)
                .ToListAsync();

        Assert.Single(payments);
        Assert.Equal(7000m, payments[0].Amount);

        var contract =
            await dbContext.Contracts
                .SingleAsync(contract =>
                    contract.Id == data.Contract.Id);

        Assert.Equal(
            ContractStatus.Pending,
            contract.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenDeadlinePassed_ShouldExpireContractAndRefundPreviousPayments()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var startDate =
            DateTime.UtcNow.AddDays(-10);

        var endDate =
            DateTime.UtcNow.AddDays(-1);

        var data = await SeedContractAsync(
            dbContext,
            finalPrice: 10000m,
            startDate: startDate,
            endDate: endDate);

        dbContext.ContractPayments.Add(
            new ContractPayment
            {
                ClientId = data.Client.Id,
                ContractId = data.Contract.Id,
                Amount = 3000m,
                PaidAt = endDate.AddDays(-1),
                IsRefunded = false
            });

        await dbContext.SaveChangesAsync();

        var service =
            new ContractPaymentService(dbContext);

        // Act
        var action = async () =>
            await service.CreateAsync(
                data.Contract.Id,
                new CreateContractPaymentRequest
                {
                    ClientId = data.Client.Id,
                    Amount = 7000m
                });

        // Assert
        var exception =
            await Assert.ThrowsAsync<
                BusinessRuleException>(action);

        Assert.Contains(
            "Termin płatności minął",
            exception.Message);

        var savedContract =
            await dbContext.Contracts
                .Include(contract => contract.Payments)
                .SingleAsync(contract =>
                    contract.Id == data.Contract.Id);

        Assert.Equal(
            ContractStatus.Expired,
            savedContract.Status);

        Assert.Single(savedContract.Payments);

        var previousPayment =
            savedContract.Payments.Single();

        Assert.True(previousPayment.IsRefunded);
        Assert.NotNull(previousPayment.RefundedAt);

        // Nie zapisano nowej wpłaty po terminie.
        Assert.Equal(3000m, previousPayment.Amount);
    }

    [Fact]
    public async Task CreateAsync_WhenClientDoesNotOwnContract_ShouldThrowBusinessRuleException()
    {
        // Arrange
        await using var dbContext =
            TestDbContextFactory.Create();

        var data = await SeedContractAsync(
            dbContext,
            finalPrice: 10000m);

        var anotherClient =
            CreateIndividualClient(
                "Anna",
                "Nowak",
                "88020212345");

        dbContext.IndividualClients.Add(
            anotherClient);

        await dbContext.SaveChangesAsync();

        var service =
            new ContractPaymentService(dbContext);

        // Act
        var action = async () =>
            await service.CreateAsync(
                data.Contract.Id,
                new CreateContractPaymentRequest
                {
                    ClientId = anotherClient.Id,
                    Amount = 1000m
                });

        // Assert
        var exception =
            await Assert.ThrowsAsync<
                BusinessRuleException>(action);

        Assert.Equal(
            "Płatność została przypisana do niewłaściwego klienta.",
            exception.Message);

        Assert.Empty(
            await dbContext.ContractPayments
                .ToListAsync());
    }

    private static async Task<ContractTestData>
        SeedContractAsync(
            AppDbContext dbContext,
            decimal finalPrice,
            int additionalSupportYears = 0,
            DateTime? startDate = null,
            DateTime? endDate = null)
    {
        var client =
            CreateIndividualClient(
                "Jan",
                "Kowalski",
                "99010112345");

        var software =
            new Software
            {
                Name = "FinTrack",
                Description = "System finansowy",
                CurrentVersion = "1.0",
                Category = "Finanse",
                YearlyLicensePrice = finalPrice
            };

        dbContext.IndividualClients.Add(client);
        dbContext.SoftwareProducts.Add(software);

        await dbContext.SaveChangesAsync();

        var supportCost =
            additionalSupportYears * 1000m;

        var contract =
            new Contract
            {
                ClientId = client.Id,
                SoftwareId = software.Id,
                SoftwareVersion =
                    software.CurrentVersion,

                CreatedAt =
                    DateTime.UtcNow.AddDays(-1),

                StartDate =
                    startDate ??
                    DateTime.UtcNow.AddDays(-1),

                EndDate =
                    endDate ??
                    DateTime.UtcNow.AddDays(10),

                BasePrice =
                    finalPrice - supportCost,

                AdditionalSupportYears =
                    additionalSupportYears,

                SupportCost =
                    supportCost,

                ProductDiscountPercentage = 0m,

                ReturningCustomerDiscountPercentage = 0m,

                FinalPrice =
                    finalPrice,

                Status =
                    ContractStatus.Pending,

                IsDeleted =
                    false
            };

        dbContext.Contracts.Add(contract);

        await dbContext.SaveChangesAsync();

        return new ContractTestData(
            client,
            software,
            contract);
    }

    private static IndividualClient
        CreateIndividualClient(
            string firstName,
            string lastName,
            string pesel)
    {
        return new IndividualClient
        {
            FirstName = firstName,
            LastName = lastName,
            Address = "Warszawa",
            Email =
                $"{firstName.ToLowerInvariant()}@example.com",
            PhoneNumber = "500600700",
            Pesel = pesel
        };
    }

    private sealed record ContractTestData(
        IndividualClient Client,
        Software Software,
        Contract Contract);
}