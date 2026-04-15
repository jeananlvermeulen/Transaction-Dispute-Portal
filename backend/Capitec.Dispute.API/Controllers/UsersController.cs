using Capitec.Dispute.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec.Dispute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IActivityLogger _activity;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IActivityLogger activity, ILogger<UsersController> logger)
    {
        _userService = userService;
        _activity = activity;
        _logger = logger;
    }

    /// <summary>
    /// Get the currently authenticated user's profile
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Update the authenticated user's name and phone number
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] Application.DTOs.UpdateUserRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var success = await _userService.UpdateUserAsync(userId, request.FirstName, request.LastName, request.PhoneNumber);
        if (!success) return NotFound();

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? userId;
        _activity.CustomerAction(email, "Profile updated");
        return Ok(new { success = true });
    }

    /// <summary>
    /// Change the authenticated user's password directly
    /// </summary>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] Application.DTOs.ChangePasswordRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = "New passwords do not match" });

        var (success, error) = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!success) return BadRequest(new { message = error });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Request a password change — sends an email verification code
    /// </summary>
    [HttpPost("request-password-change")]
    public async Task<IActionResult> RequestPasswordChange([FromBody] Application.DTOs.RequestPasswordChangeDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = "New passwords do not match" });

        var (success, error) = await _userService.RequestPasswordChangeAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!success) return BadRequest(new { message = error });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Confirm a pending password change using the emailed verification code
    /// </summary>
    [HttpPost("confirm-password-change")]
    public async Task<IActionResult> ConfirmPasswordChange([FromBody] Application.DTOs.ConfirmPasswordChangeDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var (success, error) = await _userService.ConfirmPasswordChangeAsync(userId, request.Code);
        if (!success) return BadRequest(new { message = error });

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? userId;
        _activity.CustomerAction(email, "Password changed successfully");
        return Ok(new { success = true });
    }

    /// <summary>
    /// Request a profile update — sends an email verification code
    /// </summary>
    [HttpPost("request-profile-change")]
    public async Task<IActionResult> RequestProfileChange([FromBody] Application.DTOs.RequestProfileChangeDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var (success, error) = await _userService.RequestProfileChangeAsync(userId, request.FirstName, request.LastName, request.PhoneNumber);
        if (!success) return BadRequest(new { message = error });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Confirm a pending profile update using the emailed verification code
    /// </summary>
    [HttpPost("confirm-profile-change")]
    public async Task<IActionResult> ConfirmProfileChange([FromBody] Application.DTOs.ConfirmProfileChangeDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var (success, error) = await _userService.ConfirmProfileChangeAsync(userId, request.Code);
        if (!success) return BadRequest(new { message = error });

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? userId;
        _activity.CustomerAction(email, "Profile updated successfully");
        return Ok(new { success = true });
    }
}
