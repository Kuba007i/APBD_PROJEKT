using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Contracts;
using RevenueRecognition.Api.Data;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Services.Contracts;

public sealed class ContractService : IContractService
{
    private const decimal AdditionalSupportYearPrice = 1000m;
    private const decimal ReturningCustomerDiscount = 5m;

    private readonly AppDbContext _dbContext;

    public ContractService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContractResponse> CreateAsync(
        CreateContractRequest request)
    {
        ValidateContractPeriod(
            request.StartDate,
            request.EndDate);

        var now = DateTime.UtcNow;

        var client = await _dbContext.Clients
            .SingleOrDefaultAsync(client =>
                client.Id == request.ClientId &&
                !client.IsDeleted);

        if (client is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono aktywnego klienta o ID {request.ClientId}.");
        }

        var software = await _dbContext.SoftwareProducts
            .SingleOrDefaultAsync(software =>
                software.Id == request.SoftwareId);

        if (software is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono oprogramowania o ID {request.SoftwareId}.");
        }

        await ExpireOldOffersAsync(
            request.ClientId,
            request.SoftwareId,
            now);

        var hasActiveContract = await _dbContext.Contracts
            .AnyAsync(contract =>
                contract.ClientId == request.ClientId &&
                contract.SoftwareId == request.SoftwareId &&
                !contract.IsDeleted &&
                (
                    contract.Status == ContractStatus.Pending &&
                    contract.EndDate >= now
                    ||
                    contract.Status == ContractStatus.Signed &&
                    (
                        contract.UpdatesUntil == null ||
                        contract.UpdatesUntil >= now
                    )
                ));

        if (hasActiveContract)
        {
            throw new ConflictException(
                "Klient ma już aktywną ofertę albo umowę " +
                "na wybrane oprogramowanie.");
        }

        var productDiscount =
            await GetHighestActiveDiscountAsync(
                software.Id,
                now);

        var isReturningCustomer =
            await IsReturningCustomerAsync(client.Id);

        var returningCustomerDiscount =
            isReturningCustomer
                ? ReturningCustomerDiscount
                : 0m;

        var basePrice = software.YearlyLicensePrice;

        var supportCost =
            request.AdditionalSupportYears *
            AdditionalSupportYearPrice;

        var priceBeforeDiscount =
            basePrice + supportCost;

        var totalDiscount =
            productDiscount +
            returningCustomerDiscount;

        var finalPrice = CalculateFinalPrice(
            priceBeforeDiscount,
            totalDiscount);

        var contract = new Contract
        {
            ClientId = client.Id,
            SoftwareId = software.Id,
            SoftwareVersion = software.CurrentVersion,
            CreatedAt = now,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BasePrice = basePrice,
            AdditionalSupportYears =
                request.AdditionalSupportYears,
            SupportCost = supportCost,
            ProductDiscountPercentage =
                productDiscount,
            ReturningCustomerDiscountPercentage =
                returningCustomerDiscount,
            FinalPrice = finalPrice,
            Status = ContractStatus.Pending,
            IsDeleted = false
        };

        _dbContext.Contracts.Add(contract);

        await _dbContext.SaveChangesAsync();

        return MapContract(
            contract,
            software.Name);
    }

    public async Task DeleteAsync(int contractId)
    {
        var contract = await _dbContext.Contracts
            .SingleOrDefaultAsync(contract =>
                contract.Id == contractId);

        if (contract is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono kontraktu o ID {contractId}.");
        }

        if (contract.IsDeleted)
        {
            return;
        }

        contract.IsDeleted = true;
        contract.DeletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    private async Task<decimal> GetHighestActiveDiscountAsync(
        int softwareId,
        DateTime now)
    {
        var highestDiscount = await _dbContext.Discounts
            .Where(discount =>
                discount.SoftwareId == softwareId &&
                discount.Target == DiscountTarget.License &&
                discount.ValidFrom <= now &&
                discount.ValidTo >= now)
            .MaxAsync(discount =>
                (decimal?)discount.Percentage);

        return highestDiscount ?? 0m;
    }

    private async Task<bool> IsReturningCustomerAsync(
        int clientId)
    {
        return await _dbContext.Contracts
            .AnyAsync(contract =>
                contract.ClientId == clientId &&
                contract.Status == ContractStatus.Signed);
    }

    private async Task ExpireOldOffersAsync(
        int clientId,
        int softwareId,
        DateTime now)
    {
        var expiredOffers = await _dbContext.Contracts
            .Include(contract => contract.Payments)
            .Where(contract =>
                contract.ClientId == clientId &&
                contract.SoftwareId == softwareId &&
                contract.Status == ContractStatus.Pending &&
                contract.EndDate < now &&
                !contract.IsDeleted)
            .ToListAsync();

        if (expiredOffers.Count == 0)
        {
            return;
        }

        foreach (var contract in expiredOffers)
        {
            contract.Status = ContractStatus.Expired;

            foreach (var payment in contract.Payments
                         .Where(payment => !payment.IsRefunded))
            {
                payment.IsRefunded = true;
                payment.RefundedAt = now;
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    private static void ValidateContractPeriod(
        DateTime startDate,
        DateTime endDate)
    {
        var durationDays =
            (endDate.Date - startDate.Date).Days;

        if (durationDays is < 3 or > 30)
        {
            throw new BusinessRuleException(
                "Okres na opłacenie kontraktu " +
                "musi wynosić od 3 do 30 dni.");
        }
    }

    private static decimal CalculateFinalPrice(
        decimal priceBeforeDiscount,
        decimal totalDiscountPercentage)
    {
        var normalizedDiscount = Math.Clamp(
            totalDiscountPercentage,
            0m,
            100m);

        var finalPrice =
            priceBeforeDiscount *
            (1m - normalizedDiscount / 100m);

        return decimal.Round(
            finalPrice,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static ContractResponse MapContract(
        Contract contract,
        string softwareName)
    {
        return new ContractResponse
        {
            Id = contract.Id,
            ClientId = contract.ClientId,
            SoftwareId = contract.SoftwareId,
            SoftwareName = softwareName,
            SoftwareVersion = contract.SoftwareVersion,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            BasePrice = contract.BasePrice,
            IncludedUpdateYears =
                1 + contract.AdditionalSupportYears,
            AdditionalSupportYears =
                contract.AdditionalSupportYears,
            SupportCost = contract.SupportCost,
            ProductDiscountPercentage =
                contract.ProductDiscountPercentage,
            ReturningCustomerDiscountPercentage =
                contract.ReturningCustomerDiscountPercentage,
            FinalPrice = contract.FinalPrice,
            Status = contract.Status.ToString(),
            SignedAt = contract.SignedAt,
            UpdatesUntil = contract.UpdatesUntil
        };
    }
}