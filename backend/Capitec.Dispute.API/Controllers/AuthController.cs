using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Capitec.Dispute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IActivityLogger _activity;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IActivityLogger activity, ILogger<AuthController> logger)
    {
        _authService = authService;
        _activity = activity;
        _logger = logger;
    }

    /// <summary>
    /// Send email verification code before registration
    /// </summary>
    [HttpPost("register/send-code")]
    public async Task<IActionResult> SendRegistrationCode([FromBody] SendRegistrationCodeRequestDto request)
    {
        try
        {
            var (success, error) = await _authService.SendRegistrationCodeAsync(request.Email, request.FirstName);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send registration code error for {Email}", request.Email);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto request)
    {
        try
        {
            var userId = await _authService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.VerificationCode
            );

            _logger.LogInformation("User {Email} registered successfully", request.Email);
            return Ok(new { success = true, userId, message = "Registration successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for {Email}", request.Email);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);
            if (!result.Success)
            {
                _activity.CustomerAction(request.Email, "Login failed", "Invalid credentials");
                return Unauthorized(new { success = false, message = "Invalid credentials" });
            }

            if (result.RequiresMfa)
                _activity.CustomerAction(request.Email, "Login - MFA required");
            else
                _activity.CustomerAction(request.Email, "Logged in");

            var response = new AuthResponseDto
            {
                Success = true,
                Token = result.Token,
                RequiresMfa = result.RequiresMfa,
                MfaQrCode = result.MfaQrCode
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", request.Email);
            return Unauthorized(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Generate MFA QR code
    /// </summary>
    [HttpPost("mfa/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateMfa()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var qrCode = await _authService.GenerateMfaSecretAsync(userId);
            return Ok(new { success = true, qrCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA generation error");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Enable MFA for user account
    /// </summary>
    [HttpPost("mfa/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableMfa([FromBody] string mfaCode)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _authService.VerifyMfaCodeAsync(userId, mfaCode);
            var result = await _authService.EnableMfaAsync(userId, mfaCode);

            if (result)
                return Ok(new { success = true, message = "MFA enabled successfully" });

            return BadRequest(new { success = false, message = "Failed to enable MFA" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA enable error");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Verify MFA code and get JWT token after login
    /// </summary>
    [HttpPost("mfa/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyMfaAndLogin([FromBody] VerifyMfaRequestDto request)
    {
        try
        {
            var (success, token) = await _authService.VerifyMfaCodeAndGetTokenAsync(request.Email, request.MfaCode);

            if (!success)
            {
                _activity.CustomerAction(request.Email, "MFA verification failed");
                return Unauthorized(new { success = false, message = "Invalid MFA code" });
            }

            _activity.CustomerAction(request.Email, "Logged in via MFA");
            return Ok(new { success = true, token, message = "MFA verification successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA verification error");
            return Unauthorized(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Request a password reset — sends a reset code to the user's email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            await _authService.RequestPasswordResetAsync(request.Email);
            _activity.CustomerAction(request.Email, "Password reset requested");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password error");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Reset the user's password using the code received via email
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        try
        {
            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var (success, error) = await _authService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
            if (!success) return BadRequest(new { message = error });

            _activity.CustomerAction(request.Email, "Password reset successfully");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password error");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}