using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Capitec.Dispute.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Capitec.Dispute.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;

    public UserService(UserManager<User> userManager, ILogger<UserService> logger, ApplicationDbContext dbContext, IEmailService emailService)
    {
        _userManager = userManager;
        _logger = logger;
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber ?? "",
                AccountNumber = user.AccountNumber,
                IsMfaEnabled = user.IsMfaEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateUserAsync(string userId, string firstName, string lastName, string phoneNumber)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} updated successfully", userId);
            }
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded) return (true, null);

            var error = string.Join(" ", result.Errors.Select(e => e.Description));
            return (false, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> RequestPasswordChangeAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found");

            // Verify the current password is correct before sending the code
            var passwordValid = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!passwordValid) return (false, "Current password is incorrect");

            // Invalidate any existing unused codes for this user
            var existing = await _dbContext.PasswordChangeRequests
                .Where(r => r.UserId == userId && !r.Used)
                .ToListAsync();
            _dbContext.PasswordChangeRequests.RemoveRange(existing);

            // Generate a 6-digit code
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Hash the new password using Identity's hasher so we don't store plaintext
            var hasher = _userManager.PasswordHasher;
            var hashedNewPassword = hasher.HashPassword(user, newPassword);

            var request = new PasswordChangeRequest
            {
                UserId = userId,
                Code = code,
                NewPasswordHash = hashedNewPassword,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            _dbContext.PasswordChangeRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            // Send code by email (non-blocking)
            _ = _emailService.SendPasswordChangeCodeAsync(user.Email!, user.FirstName, code);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password change for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> ConfirmPasswordChangeAsync(string userId, string code)
    {
        try
        {
            var request = await _dbContext.PasswordChangeRequests
                .Where(r => r.UserId == userId && !r.Used)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (request == null) return (false, "No pending password change request found");
            if (request.ExpiresAt < DateTime.UtcNow) return (false, "Verification code has expired");
            if (request.Code != code) return (false, "Incorrect verification code");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found");

            // Apply the pre-hashed password directly
            user.PasswordHash = request.NewPasswordHash;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return (false, error);
            }

            // Mark code as used
            request.Used = true;
            await _dbContext.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password change for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> RequestProfileChangeAsync(string userId, string firstName, string lastName, string phoneNumber)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found");

            // Invalidate any existing unused codes for this user
            var existing = await _dbContext.ProfileChangeRequests
                .Where(r => r.UserId == userId && !r.Used)
                .ToListAsync();
            _dbContext.ProfileChangeRequests.RemoveRange(existing);

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var request = new ProfileChangeRequest
            {
                UserId = userId,
                Code = code,
                PendingFirstName = firstName,
                PendingLastName = lastName,
                PendingPhoneNumber = phoneNumber,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            _dbContext.ProfileChangeRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            _ = _emailService.SendProfileChangeCodeAsync(user.Email!, user.FirstName, code);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting profile change for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> ConfirmProfileChangeAsync(string userId, string code)
    {
        try
        {
            var request = await _dbContext.ProfileChangeRequests
                .Where(r => r.UserId == userId && !r.Used)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (request == null) return (false, "No pending profile change request found");
            if (request.ExpiresAt < DateTime.UtcNow) return (false, "Verification code has expired");
            if (request.Code != code) return (false, "Incorrect verification code");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found");

            user.FirstName = request.PendingFirstName;
            user.LastName = request.PendingLastName;
            user.PhoneNumber = request.PendingPhoneNumber;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return (false, error);
            }

            request.Used = true;
            await _dbContext.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming profile change for user {UserId}", userId);
            throw;
        }
    }
}