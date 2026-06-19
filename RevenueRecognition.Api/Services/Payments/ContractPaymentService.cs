using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Common.Exceptions;
using RevenueRecognition.Api.Contracts.Payments;
using RevenueRecognition.Api.Data;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Services.Payments;

public sealed class ContractPaymentService
    : IContractPaymentService
{
    private readonly AppDbContext _dbContext;

    public ContractPaymentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContractPaymentResponse> CreateAsync(
        int contractId,
        CreateContractPaymentRequest request)
    {
        var now = DateTime.UtcNow;

        var contract = await _dbContext.Contracts
            .Include(contract => contract.Payments)
            .SingleOrDefaultAsync(contract =>
                contract.Id == contractId &&
                !contract.IsDeleted);

        if (contract is null)
        {
            throw new NotFoundException(
                $"Nie znaleziono aktywnego kontraktu o ID {contractId}.");
        }

        if (contract.ClientId != request.ClientId)
        {
            throw new BusinessRuleException(
                "Płatność została przypisana do niewłaściwego klienta.");
        }

        var clientExists = await _dbContext.Clients
            .AnyAsync(client =>
                client.Id == request.ClientId &&
                !client.IsDeleted);

        if (!clientExists)
        {
            throw new NotFoundException(
                $"Nie znaleziono aktywnego klienta o ID {request.ClientId}.");
        }

        if (contract.Status == ContractStatus.Signed)
        {
            throw new BusinessRuleException(
                "Kontrakt został już w pełni opłacony i podpisany.");
        }

        if (contract.Status == ContractStatus.Expired)
        {
            await RefundPaymentsAsync(contract, now);

            throw new BusinessRuleException(
                "Oferta wygasła. Należy przygotować nowy kontrakt.");
        }

        if (now < contract.StartDate)
        {
            throw new BusinessRuleException(
                "Nie można przyjąć płatności przed datą rozpoczęcia kontraktu.");
        }

        if (now > contract.EndDate)
        {
            contract.Status = ContractStatus.Expired;

            await RefundPaymentsAsync(contract, now);

            throw new BusinessRuleException(
                "Termin płatności minął. " +
                "Poprzednie wpłaty zostały zwrócone i należy przygotować nową ofertę.");
        }

        var amount = decimal.Round(
            request.Amount,
            2,
            MidpointRounding.AwayFromZero);

        var totalPaidBefore = contract.Payments
            .Where(payment => !payment.IsRefunded)
            .Sum(payment => payment.Amount);

        var remainingBefore =
            contract.FinalPrice - totalPaidBefore;

        if (amount > remainingBefore)
        {
            throw new BusinessRuleException(
                $"Kwota płatności przekracza pozostałą kwotę kontraktu. " +
                $"Do zapłaty pozostało {remainingBefore:F2} PLN.");
        }

        var payment = new ContractPayment
        {
            ClientId = request.ClientId,
            ContractId = contract.Id,
            Amount = amount,
            PaidAt = now,
            IsRefunded = false
        };

        contract.Payments.Add(payment);

        var totalPaidAfter =
            totalPaidBefore + amount;

        if (totalPaidAfter == contract.FinalPrice)
        {
            contract.Status = ContractStatus.Signed;
            contract.SignedAt = now;

            var includedUpdateYears =
                1 + contract.AdditionalSupportYears;

            contract.UpdatesUntil =
                now.AddYears(includedUpdateYears);
        }

        await _dbContext.SaveChangesAsync();

        var remainingAfter =
            contract.FinalPrice - totalPaidAfter;

        return new ContractPaymentResponse
        {
            Id = payment.Id,
            ContractId = contract.Id,
            ClientId = payment.ClientId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt,
            TotalPaid = totalPaidAfter,
            RemainingAmount = remainingAfter,
            ContractStatus = contract.Status.ToString(),
            SignedAt = contract.SignedAt,
            UpdatesUntil = contract.UpdatesUntil
        };
    }

    private async Task RefundPaymentsAsync(
        Contract contract,
        DateTime refundedAt)
    {
        foreach (var payment in contract.Payments
                     .Where(payment => !payment.IsRefunded))
        {
            payment.IsRefunded = true;
            payment.RefundedAt = refundedAt;
        }

        await _dbContext.SaveChangesAsync();
    }
}