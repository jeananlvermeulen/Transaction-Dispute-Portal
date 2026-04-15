using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Capitec.Dispute.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;

namespace Capitec.Dispute.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;

    public AuthService(UserManager<User> userManager, ILogger<AuthService> logger, IConfiguration configuration, ApplicationDbContext dbContext, IEmailService emailService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<(bool Success, string? Error)> SendRegistrationCodeAsync(string email, string firstName)
    {
        try
        {
            // Check email not already taken
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
                return (false, "An account with this email already exists.");

            // Invalidate any existing unused codes for this email
            var old = _dbContext.EmailVerificationRequests
                .Where(r => r.Email == email && !r.Used);
            _dbContext.EmailVerificationRequests.RemoveRange(old);

            var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            _dbContext.EmailVerificationRequests.Add(new EmailVerificationRequest
            {
                Email = email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });
            await _dbContext.SaveChangesAsync();

            _ = _emailService.SendRegistrationVerificationCodeAsync(email, firstName, code);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending registration code for {Email}", email);
            throw;
        }
    }

    public async Task<string> RegisterAsync(string email, string password, string firstName, string lastName, string phoneNumber, string verificationCode)
    {
        try
        {
            var request = await _dbContext.EmailVerificationRequests
                .Where(r => r.Email == email && !r.Used)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (request == null)
                throw new Exception("No verification code found. Please request a new code.");
            if (request.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Verification code has expired. Please request a new code.");
            if (request.Code != verificationCode)
                throw new Exception("Incorrect verification code.");

            var user = new User
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                AccountNumber = Random.Shared.NextInt64(1000000000L, 9999999999L).ToString()
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                request.Used = true;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("User {Email} registered successfully", email);
                return user.Id;
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for {Email}: {Errors}", email, errors);
            throw new Exception($"Registration failed: {errors}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            throw;
        }
    }

    public async Task<(bool Success, string Token, bool RequiresMfa, string? MfaQrCode)> LoginAsync(string email, string password)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Email} not found", email);
                return (false, "", false, null);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                _logger.LogWarning("Login failed for {Email}: Invalid password", email);
                return (false, "", false, null);
            }

            if (user.IsMfaEnabled && string.IsNullOrEmpty(user.PhoneNumber))
            {
                return (true, "", true, null); // Require MFA
            }

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);
            _logger.LogInformation("User {Email} logged in successfully", email);
            return (true, token, false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }

    public async Task<bool> VerifyMfaCodeAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code);
        return result;
    }

    public async Task<string> GenerateMfaSecretAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        // Reset the authenticator key to generate a new TOTP secret
        await _userManager.ResetAuthenticatorKeyAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user);

        // Generate the TOTP provision URI for QR code
        var email = user.Email ?? "user";
        var appName = "Capitec";
        var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(appName + ":" + email)}?secret={key}&issuer={Uri.EscapeDataString(appName)}";
        
        _logger.LogInformation("MFA secret generated for user {UserId}", userId);
        return qrCodeUri;
    }

    public async Task<bool> EnableMfaAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // Verify the TOTP code before enabling MFA
        var isValidCode = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, token);
        if (!isValidCode)
        {
            _logger.LogWarning("Invalid MFA token provided for user {UserId}", userId);
            return false;
        }

        user.TwoFactorEnabled = true;
        user.IsMfaEnabled = true;
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
            _logger.LogInformation("MFA enabled for user {UserId}", userId);
        else
            _logger.LogError("Failed to enable MFA for user {UserId}", userId);
            
        return result.Succeeded;
    }

    public async Task<bool> DisableMfaAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.IsMfaEnabled = false;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    private async Task<string> GenerateJwtTokenAsync(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "your-secret-key-here-make-it-long");
        var tokenExpirationMinutes = int.Parse(jwtSettings["TokenExpirationMinutes"] ?? "60");
        var issuer = jwtSettings["Issuer"] ?? "Capitec";
        var audience = jwtSettings["Audience"] ?? "DisputePortal";

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
            new Claim(ClaimTypes.Surname, user.LastName ?? ""),
            new Claim("AccountNumber", user.AccountNumber ?? "")
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(secretKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddMinutes(tokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    public async Task<(bool Success, string Token)> VerifyMfaCodeAndGetTokenAsync(string email, string mfaCode)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("MFA verification failed: User {Email} not found", email);
                return (false, "");
            }

            // Verify the TOTP code
            var isValidCode = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, mfaCode);
            if (!isValidCode)
            {
                _logger.LogWarning("Invalid MFA code provided for user {Email}", email);
                return (false, "");
            }

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);
            _logger.LogInformation("User {Email} verified MFA and logged in successfully", email);
            return (true, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MFA verification for {Email}", email);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> RequestPasswordResetAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            // Always return success to avoid email enumeration
            if (user == null) return (true, null);

            // Invalidate any existing unused reset codes
            var existing = await _dbContext.PasswordChangeRequests
                .Where(r => r.UserId == user.Id && !r.Used)
                .ToListAsync();
            _dbContext.PasswordChangeRequests.RemoveRange(existing);

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            _dbContext.PasswordChangeRequests.Add(new PasswordChangeRequest
            {
                UserId = user.Id,
                Code = code,
                NewPasswordHash = string.Empty, // not needed for reset flow
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });
            await _dbContext.SaveChangesAsync();

            _ = _emailService.SendPasswordResetCodeAsync(user.Email!, user.FirstName, code);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset for {Email}", email);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string code, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return (false, "Invalid request");

            var request = await _dbContext.PasswordChangeRequests
                .Where(r => r.UserId == user.Id && !r.Used)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (request == null) return (false, "No pending reset request found");
            if (request.ExpiresAt < DateTime.UtcNow) return (false, "Verification code has expired");
            if (request.Code != code) return (false, "Incorrect verification code");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

            request.Used = true;
            await _dbContext.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", email);
            throw;
        }
    }
}