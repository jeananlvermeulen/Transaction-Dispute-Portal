using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec.Dispute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DisputesController : ControllerBase
{
    private readonly IDisputeService _disputeService;
    private readonly IActivityLogger _activity;
    private readonly ILogger<DisputesController> _logger;

    public DisputesController(IDisputeService disputeService, IActivityLogger activity, ILogger<DisputesController> logger)
    {
        _disputeService = disputeService;
        _activity = activity;
        _logger = logger;
    }

    /// <summary>
    /// Create a new dispute
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var dispute = await _disputeService.CreateDisputeAsync(userId, request);
            _logger.LogInformation("Dispute created: {DisputeId}", dispute.Id);

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? userId;
            _activity.CustomerAction(email, "Dispute submitted", $"Ref: {dispute.IncidentReference} | Reason: {dispute.Reason}");

            return CreatedAtAction(nameof(GetDisputeById), new { disputeId = dispute.Id }, new { success = true, data = dispute });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dispute");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get all disputes for the authenticated user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserDisputes([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _disputeService.GetUserDisputesAsync(userId, pageNumber, pageSize);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific dispute by ID
    /// </summary>
    [HttpGet("{disputeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisputeById([FromRoute] Guid disputeId)
    {
        try
        {
            var dispute = await _disputeService.GetDisputeByIdAsync(disputeId);
            if (dispute == null)
                return NotFound();

            return Ok(new { success = true, data = dispute });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute {DisputeId}", disputeId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed dispute information including status history
    /// </summary>
    [HttpGet("{disputeId}/detail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisputeDetail([FromRoute] Guid disputeId)
    {
        try
        {
            var detail = await _disputeService.GetDisputeDetailAsync(disputeId);
            if (detail == null)
                return NotFound();

            return Ok(new { success = true, data = detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute detail {DisputeId}", disputeId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a dispute (Customer only)
    /// </summary>
    [HttpPost("{disputeId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelDispute([FromRoute] Guid disputeId, [FromBody] CancelDisputeRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.CancellationReason))
                return BadRequest(new { success = false, message = "A cancellation reason is required." });

            var (success, error) = await _disputeService.CancelDisputeAsync(disputeId, userId, request.CancellationReason);
            if (!success)
                return BadRequest(new { success = false, message = error });

            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? userId;
            _activity.CustomerAction(email, "Dispute cancelled", $"Dispute: {disputeId} | Reason: {request.CancellationReason}");

            return Ok(new { success = true, message = "Dispute cancelled successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling dispute {DisputeId}", disputeId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Update dispute status (Employee only)
    /// </summary>
    [Authorize(Roles = "Employee")]
    [HttpPut("{disputeId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDisputeStatus([FromRoute] Guid disputeId, [FromBody] UpdateDisputeStatusDto request)
    {
        try
        {
            var employeeId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return Unauthorized();

            var result = await _disputeService.UpdateDisputeStatusAsync(disputeId, employeeId, request);
            if (!result)
                return NotFound();

            _logger.LogInformation("Dispute {DisputeId} status updated", disputeId);
            var empCode = User.FindFirst("EmployeeCode")?.Value ?? "";
            var empEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? employeeId;
            var detail = $"Dispute: {disputeId} | New status: {request.NewStatus}";
            if (!string.IsNullOrEmpty(request.Notes)) detail += $" | Notes: {request.Notes}";
            _activity.EmployeeAction(empCode, empEmail, "Dispute status updated", detail);

            return Ok(new { success = true, message = "Dispute status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dispute status {DisputeId}", disputeId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}