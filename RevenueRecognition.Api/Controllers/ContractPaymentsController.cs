using Microsoft.AspNetCore.Mvc;
using RevenueRecognition.Api.Contracts.Payments;
using RevenueRecognition.Api.Services.Payments;

namespace RevenueRecognition.Api.Controllers;

[ApiController]
[Route("api/contracts/{contractId:int}/payments")]
public sealed class ContractPaymentsController : ControllerBase
{
    private readonly IContractPaymentService _paymentService;

    public ContractPaymentsController(
        IContractPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ContractPaymentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractPaymentResponse>> Create(
        int contractId,
        [FromBody] CreateContractPaymentRequest request)
    {
        var payment =
            await _paymentService.CreateAsync(
                contractId,
                request);

        return StatusCode(
            StatusCodes.Status201Created,
            payment);
    }
}