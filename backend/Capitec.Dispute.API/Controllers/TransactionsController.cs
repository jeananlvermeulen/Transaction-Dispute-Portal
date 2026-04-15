using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec.Dispute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all transactions for the authenticated user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _transactionService.GetUserTransactionsAsync(userId, pageNumber, pageSize);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific transaction by ID
    /// </summary>
    [HttpGet("{transactionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction([FromRoute] Guid transactionId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var transaction = await _transactionService.GetTransactionByIdAsync(transactionId, userId);
            if (transaction == null)
                return NotFound();

            return Ok(new { success = true, data = transaction });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {TransactionId}", transactionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Create a simulated transaction for testing
    /// </summary>
    [HttpPost("simulate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SimulateTransaction([FromBody] CreateTransactionDto request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var transactionId = await _transactionService.CreateSimulatedTransactionAsync(
                userId,
                request.Amount,
                request.Description
            );

            _logger.LogInformation("Simulated transaction created: {TransactionId}", transactionId);
            return CreatedAtAction(nameof(GetTransaction), new { transactionId }, new { success = true, transactionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating simulated transaction");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}