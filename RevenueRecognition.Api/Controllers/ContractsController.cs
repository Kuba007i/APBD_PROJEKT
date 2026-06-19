using Microsoft.AspNetCore.Mvc;
using RevenueRecognition.Api.Contracts.Contracts;
using RevenueRecognition.Api.Services.Contracts;

namespace RevenueRecognition.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public sealed class ContractsController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ContractResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractResponse>> Create(
        [FromBody] CreateContractRequest request)
    {
        var contract =
            await _contractService.CreateAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            contract);
    }

    [HttpDelete("{contractId:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int contractId)
    {
        await _contractService.DeleteAsync(contractId);

        return NoContent();
    }
}