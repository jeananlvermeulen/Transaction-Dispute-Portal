using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Capitec.Dispute.Infrastructure.Services;

public class EmployeeAuthService : IEmployeeAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeAuthService> _logger;

    public EmployeeAuthService(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<EmployeeAuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    private static string GenerateEmployeeCode()
    {
        var digits = Random.Shared.Next(100000, 999999);
        return $"EMP-{digits}";
    }

    public async Task<(bool Success, string? Error)> SendRegistrationCodeAsync(string email, string firstName)
    {
        try
        {
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
                return (false, "An account with this email already exists.");

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
            _logger.LogError(ex, "Error sending employee registration code for {Email}", email);
            throw;
        }
    }

    public async Task<string> RegisterEmployeeAsync(
        string email, string password,
        string firstName, string lastName,
        string phoneNumber, string department,
        string verificationCode)
    {
        // Validate registration verification code
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

        // Ensure Employee role exists
        if (!await _roleManager.RoleExistsAsync("Employee"))
            await _roleManager.CreateAsync(new IdentityRole("Employee"));

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
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Employee registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, "Employee");

        // Create the domain Employee record for FK relationships
        var employee = new Employee
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = phoneNumber,
            Department = department,
            PasswordHash = user.PasswordHash ?? string.Empty,
            EmployeeCode = GenerateEmployeeCode()
        };

        _dbContext.Employees.Add(employee);
        request.Used = true;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Employee {Email} registered with domain ID {EmployeeId}", email, employee.Id);
        return user.Id;
    }

    public async Task<(bool Success, string Token, string EmployeeId)> LoginEmployeeAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Employee login failed: {Email} not found", email);
            return (false, "", "");
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            _logger.LogWarning("Employee login failed: wrong password for {Email}", email);
            return (false, "", "");
        }

        if (!await _userManager.IsInRoleAsync(user, "Employee"))
        {
            _logger.LogWarning("Login rejected: {Email} is not an employee", email);
            return (false, "", "");
        }

        // Get the domain Employee record
        var employee = _dbContext.Employees.FirstOrDefault(e => e.Email == email);
        var employeeId = employee?.Id.ToString() ?? "";
        var employeeCode = employee?.EmployeeCode ?? "";

        var token = GenerateEmployeeJwtToken(user, employeeCode);
        _logger.LogInformation("Employee {Email} logged in successfully", email);
        return (true, token, employeeId);
    }

    private string GenerateEmployeeJwtToken(User user, string employeeCode = "")
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "your-secret-key-here-make-it-long");
        var tokenExpirationMinutes = int.Parse(jwtSettings["TokenExpirationMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
            new Claim(ClaimTypes.Surname, user.LastName ?? ""),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim("EmployeeCode", employeeCode)
        };

        var key = new SymmetricSecurityKey(secretKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "Capitec",
            audience: jwtSettings["Audience"] ?? "DisputePortal",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(tokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
