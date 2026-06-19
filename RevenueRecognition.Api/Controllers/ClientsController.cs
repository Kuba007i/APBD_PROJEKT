using Microsoft.AspNetCore.Mvc;
using RevenueRecognition.Api.Contracts.Clients;
using RevenueRecognition.Api.Services.Clients;
using Microsoft.AspNetCore.Authorization;
using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Controllers;

[ApiController]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpPost("individuals")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientResponse>> CreateIndividual(
        [FromBody] CreateIndividualClientRequest request)
    {
        var client =
            await _clientService.CreateIndividualAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            client);
    }

    [HttpPost("companies")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientResponse>> CreateCompany(
        [FromBody] CreateCompanyClientRequest request)
    {
        var client =
            await _clientService.CreateCompanyAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            client);
    }

    [HttpPut("individuals/{clientId:int}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public async Task<ActionResult<ClientResponse>> UpdateIndividual(
        int clientId,
        [FromBody] UpdateIndividualClientRequest request)
    {
        var client =
            await _clientService.UpdateIndividualAsync(
                clientId,
                request);

        return Ok(client);
    }

    [HttpPut("companies/{clientId:int}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public async Task<ActionResult<ClientResponse>> UpdateCompany(
        int clientId,
        [FromBody] UpdateCompanyClientRequest request)
    {
        var client =
            await _clientService.UpdateCompanyAsync(
                clientId,
                request);

        return Ok(client);
    }

    [HttpDelete("{clientId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = nameof(EmployeeRole.Admin))]
    public async Task<IActionResult> Delete(
        int clientId)
    {
        await _clientService.DeleteAsync(clientId);

        return NoContent();
    }
}