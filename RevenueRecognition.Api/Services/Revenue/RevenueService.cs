using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Revenue;
using RevenueRecognition.Api.Data;
using RevenueRecognition.Api.Domain.Enums;
using RevenueRecognition.Api.Services.Currency;

namespace RevenueRecognition.Api.Services.Revenue;

public sealed class RevenueService : IRevenueService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrencyService _currencyService;

    public RevenueService(
        AppDbContext dbContext,
        ICurrencyService currencyService)
    {
        _dbContext = dbContext;
        _currencyService = currencyService;
    }

    public async Task<RevenueResponse> GetCurrentRevenueAsync(
        int? softwareId,
        string currency)
    {
        var softwareName =
            await ValidateAndGetSoftwareNameAsync(softwareId);

        var query = _dbContext.Contracts
            .AsNoTracking()
            .Where(contract =>
                contract.Status == ContractStatus.Signed);

        if (softwareId.HasValue)
        {
            query = query.Where(contract =>
                contract.SoftwareId == softwareId.Value);
        }

        // Podpisany kontrakt został w całości opłacony,
        // więc cała FinalPrice stanowi aktualny przychód.
        var amountInPln = await query.SumAsync(
            contract => (decimal?)contract.FinalPrice) ?? 0m;

        return await CreateResponseAsync(
            "Current",
            softwareId,
            softwareName,
            amountInPln,
            currency);
    }

    public async Task<RevenueResponse> GetExpectedRevenueAsync(
        int? softwareId,
        string currency)
    {
        var softwareName =
            await ValidateAndGetSoftwareNameAsync(softwareId);

        var now = DateTime.UtcNow;

        var query = _dbContext.Contracts
            .AsNoTracking()
            .Where(contract =>
                contract.Status == ContractStatus.Signed
                ||
                (
                    contract.Status == ContractStatus.Pending &&
                    contract.EndDate >= now &&
                    !contract.IsDeleted
                ));

        if (softwareId.HasValue)
        {
            query = query.Where(contract =>
                contract.SoftwareId == softwareId.Value);
        }

        // Signed: przychód już uznany.
        // Pending: zakładamy, że aktywna oferta zostanie opłacona.
        var amountInPln = await query.SumAsync(
            contract => (decimal?)contract.FinalPrice) ?? 0m;

        return await CreateResponseAsync(
            "Expected",
            softwareId,
            softwareName,
            amountInPln,
            currency);
    }

    private async Task<string?> ValidateAndGetSoftwareNameAsync(
        int? softwareId)
    {
        if (!softwareId.HasValue)
        {
            return null;
        }

        if (softwareId.Value <= 0)
        {
            throw new BusinessRuleException(
                "Identyfikator oprogramowania musi być większy od zera.");
        }

        var softwareName = await _dbContext.SoftwareProducts
            .AsNoTracking()
            .Where(software =>
                software.Id == softwareId.Value)
            .Select(software => software.Name)
            .SingleOrDefaultAsync();

        if (softwareName is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono oprogramowania o ID {softwareId.Value}.");
        }

        return softwareName;
    }

    private async Task<RevenueResponse> CreateResponseAsync(
        string revenueType,
        int? softwareId,
        string? softwareName,
        decimal amountInPln,
        string currency)
    {
        var conversion =
            await _currencyService.ConvertFromPlnAsync(
                amountInPln,
                currency);

        return new RevenueResponse
        {
            RevenueType = revenueType,
            SoftwareId = softwareId,
            SoftwareName = softwareName,
            AmountInPln = decimal.Round(
                amountInPln,
                2,
                MidpointRounding.AwayFromZero),
            Currency = conversion.Currency,
            ExchangeRate = conversion.ExchangeRate,
            ConvertedAmount = conversion.ConvertedAmount,
            CalculatedAt = DateTime.UtcNow
        };
    }
}