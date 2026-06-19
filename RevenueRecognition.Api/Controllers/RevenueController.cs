using Microsoft.AspNetCore.Mvc;
using RevenueRecognition.Api.Contracts.Revenue;
using RevenueRecognition.Api.Services.Revenue;

namespace RevenueRecognition.Api.Controllers;

[ApiController]
[Route("api/revenue")]
public sealed class RevenueController : ControllerBase
{
    private readonly IRevenueService _revenueService;

    public RevenueController(
        IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(RevenueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RevenueResponse>> GetCurrent(
        [FromQuery] int? softwareId,
        [FromQuery] string currency = "PLN")
    {
        var response =
            await _revenueService.GetCurrentRevenueAsync(
                softwareId,
                currency);

        return Ok(response);
    }

    [HttpGet("expected")]
    [ProducesResponseType(typeof(RevenueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RevenueResponse>> GetExpected(
        [FromQuery] int? softwareId,
        [FromQuery] string currency = "PLN")
    {
        var response =
            await _revenueService.GetExpectedRevenueAsync(
                softwareId,
                currency);

        return Ok(response);
    }
}