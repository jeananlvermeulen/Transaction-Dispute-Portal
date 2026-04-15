using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec.Dispute.API.Controllers;

[ApiController]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeAuthService _employeeAuthService;
    private readonly IDisputeService _disputeService;
    private readonly IActivityLogger _activity;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeAuthService employeeAuthService, IDisputeService disputeService, IActivityLogger activity, ILogger<EmployeeController> logger)
    {
        _employeeAuthService = employeeAuthService;
        _disputeService = disputeService;
        _activity = activity;
        _logger = logger;
    }

    /// <summary>
    /// Send email verification code before employee registration
    /// </summary>
    [HttpPost("register/send-code")]
    public async Task<IActionResult> SendRegistrationCode([FromBody] SendRegistrationCodeRequestDto request)
    {
        try
        {
            var (success, error) = await _employeeAuthService.SendRegistrationCodeAsync(request.Email, request.FirstName);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending employee registration code for {Email}", request.Email);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Register a new employee account
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterEmployeeRequestDto request)
    {
        try
        {
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { success = false, message = "Passwords do not match." });

            var userId = await _employeeAuthService.RegisterEmployeeAsync(
                request.Email, request.Password,
                request.FirstName, request.LastName,
                request.PhoneNumber, request.Department,
                request.VerificationCode);

            return Ok(new { success = true, message = "Employee registered successfully.", userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering employee");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Employee login — returns a JWT token and employee code
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var (success, token, employeeId) = await _employeeAuthService.LoginEmployeeAsync(request.Email, request.Password);

            if (!success)
            {
                _activity.EmployeeAction("", request.Email, "Login failed", "Invalid credentials");
                return Ok(new { success = false, message = "Invalid email or password, or account is not an employee account." });
            }

            // Decode employeeCode from token claims for convenience
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var employeeCode = jwt.Claims.FirstOrDefault(c => c.Type == "EmployeeCode")?.Value ?? "";

            _activity.EmployeeAction(employeeCode, request.Email, "Logged in");
            return Ok(new { success = true, token, employeeCode, role = "Employee" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during employee login");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Look up a dispute by its incident reference number (Employee only)
    /// </summary>
    [HttpGet("disputes/reference/{reference}")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetDisputeByReference(string reference)
    {
        try
        {
            var dispute = await _disputeService.GetDisputeByReferenceAsync(reference.ToUpperInvariant());
            if (dispute == null)
                return NotFound(new { success = false, message = "No dispute found with that reference number." });

            return Ok(new { success = true, data = dispute });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute by reference {Reference}", reference);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get all disputes (paginated) — visible to employees only
    /// </summary>
    [HttpGet("disputes")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetAllDisputes([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _disputeService.GetAllDisputesAsync(pageNumber, pageSize);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all disputes for employee");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
