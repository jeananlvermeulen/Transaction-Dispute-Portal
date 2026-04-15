using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Infrastructure.Data;
using Capitec.Dispute.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Capitec.Dispute.Infrastructure.Tests.Services;

public class AuthServiceTests
{
    private static Mock<UserManager<User>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static IConfiguration CreateConfiguration()
    {
        var mockConfig = new Mock<IConfiguration>();
        var mockJwtSection = new Mock<IConfigurationSection>();

        mockConfig.Setup(c => c.GetSection("Jwt")).Returns(mockJwtSection.Object);

        mockJwtSection.Setup(s => s["SecretKey"])
            .Returns("test-secret-key-that-is-long-enough-for-hmac256");
        mockJwtSection.Setup(s => s["TokenExpirationMinutes"])
            .Returns("60");
        mockJwtSection.Setup(s => s["Issuer"])
            .Returns("TestIssuer");
        mockJwtSection.Setup(s => s["Audience"])
            .Returns("TestAudience");

        return mockConfig.Object;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AuthService CreateService(Mock<UserManager<User>> mockUserManager, ApplicationDbContext? ctx = null) =>
        new AuthService(
            mockUserManager.Object,
            new Mock<ILogger<AuthService>>().Object,
            CreateConfiguration(),
            ctx ?? CreateDbContext(),
            new Mock<IEmailService>().Object);

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_returns_user_id_on_success()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var ctx = CreateDbContext();
        ctx.EmailVerificationRequests.Add(new EmailVerificationRequest
        {
            Email = "test@example.com",
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(mockUserManager, ctx);

        var userId = await service.RegisterAsync(
            "test@example.com", "Password1!", "John", "Doe", "+27821234567", "123456");

        userId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_throws_when_identity_returns_errors()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Email already taken" }));

        var ctx = CreateDbContext();
        ctx.EmailVerificationRequests.Add(new EmailVerificationRequest
        {
            Email = "taken@example.com",
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(mockUserManager, ctx);

        Func<Task> act = () => service.RegisterAsync(
            "taken@example.com", "Password1!", "Jane", "Smith", "+27821234567", "123456");

        await act.Should().ThrowAsync<Exception>().WithMessage("*Email already taken*");
    }

    [Fact]
    public async Task RegisterAsync_assigns_a_numeric_account_number()
    {
        var mockUserManager = CreateMockUserManager();
        User? capturedUser = null;

        mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        var ctx = CreateDbContext();
        ctx.EmailVerificationRequests.Add(new EmailVerificationRequest
        {
            Email = "john@example.com",
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(mockUserManager, ctx);

        await service.RegisterAsync("john@example.com", "Password1!", "John", "Doe", "+27821234567", "123456");

        capturedUser!.AccountNumber.Should().NotBeNullOrEmpty();
        long.TryParse(capturedUser.AccountNumber, out _).Should().BeTrue("account number should be a numeric string");
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_returns_failure_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(mockUserManager);

        var result = await service.LoginAsync("unknown@example.com", "Password1!");

        result.Success.Should().BeFalse();
        result.Token.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginAsync_returns_failure_for_wrong_password()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = Guid.NewGuid().ToString(), Email = "test@example.com" };

        mockUserManager
            .Setup(m => m.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.CheckPasswordAsync(user, "wrongpassword"))
            .ReturnsAsync(false);

        var service = CreateService(mockUserManager);

        var result = await service.LoginAsync("test@example.com", "wrongpassword");

        result.Success.Should().BeFalse();
        result.Token.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginAsync_returns_success_with_token_for_valid_credentials()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsMfaEnabled = false
        };

        mockUserManager
            .Setup(m => m.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.CheckPasswordAsync(user, "Password1!"))
            .ReturnsAsync(true);
        mockUserManager
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        var service = CreateService(mockUserManager);

        var result = await service.LoginAsync("test@example.com", "Password1!");

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RequiresMfa.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_requires_mfa_when_mfa_enabled_and_no_phone()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            IsMfaEnabled = true,
            PhoneNumber = null
        };

        mockUserManager
            .Setup(m => m.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.CheckPasswordAsync(user, "Password1!"))
            .ReturnsAsync(true);

        var service = CreateService(mockUserManager);

        var result = await service.LoginAsync("test@example.com", "Password1!");

        result.Success.Should().BeTrue();
        result.RequiresMfa.Should().BeTrue();
        result.Token.Should().BeEmpty();
    }

    // ── VerifyMfaCodeAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyMfaCodeAsync_returns_false_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(mockUserManager);

        var result = await service.VerifyMfaCodeAsync("nonexistent-id", "123456");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyMfaCodeAsync_returns_true_for_valid_code()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-id" };

        mockUserManager
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456"))
            .ReturnsAsync(true);

        var service = CreateService(mockUserManager);

        var result = await service.VerifyMfaCodeAsync("user-id", "123456");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyMfaCodeAsync_returns_false_for_invalid_code()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-id" };

        mockUserManager
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "000000"))
            .ReturnsAsync(false);

        var service = CreateService(mockUserManager);

        var result = await service.VerifyMfaCodeAsync("user-id", "000000");

        result.Should().BeFalse();
    }

    // ── EnableMfaAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task EnableMfaAsync_returns_false_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(mockUserManager);

        var result = await service.EnableMfaAsync("nonexistent-id", "123456");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnableMfaAsync_returns_false_when_token_is_invalid()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-id" };

        mockUserManager
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "badtoken"))
            .ReturnsAsync(false);

        var service = CreateService(mockUserManager);

        var result = await service.EnableMfaAsync("user-id", "badtoken");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnableMfaAsync_enables_mfa_for_valid_token()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-id", IsMfaEnabled = false };

        mockUserManager
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456"))
            .ReturnsAsync(true);
        mockUserManager
            .Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService(mockUserManager);

        var result = await service.EnableMfaAsync("user-id", "123456");

        result.Should().BeTrue();
        user.IsMfaEnabled.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
    }

    // ── VerifyMfaCodeAndGetTokenAsync ─────────────────────────────────────────

    [Fact]
    public async Task VerifyMfaCodeAndGetTokenAsync_returns_failure_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(mockUserManager);

        var result = await service.VerifyMfaCodeAndGetTokenAsync("unknown@example.com", "123456");

        result.Success.Should().BeFalse();
        result.Token.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyMfaCodeAndGetTokenAsync_returns_failure_for_invalid_code()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-id", Email = "test@example.com" };

        mockUserManager
            .Setup(m => m.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "000000"))
            .ReturnsAsync(false);

        var service = CreateService(mockUserManager);

        var result = await service.VerifyMfaCodeAndGetTokenAsync("test@example.com", "000000");

        result.Success.Should().BeFalse();
        result.Token.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyMfaCodeAndGetTokenAsync_returns_token_for_valid_code()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        mockUserManager
            .Setup(m => m.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456"))
            .ReturnsAsync(true);
        mockUserManager
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        var service = CreateService(mockUserManager);

        var result = await service.VerifyMfaCodeAndGetTokenAsync("test@example.com", "123456");

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
    }

    // ── RequestPasswordResetAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RequestPasswordResetAsync_returns_success_true_when_user_not_found()
    {
        // Security: always return success to prevent user enumeration
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(mockUserManager);

        var result = await service.RequestPasswordResetAsync("unknown@example.com");

        // Should not throw and should return gracefully
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RequestPasswordResetAsync_returns_success_for_existing_user()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@example.com",
            FirstName = "Test"
        };

        mockUserManager
            .Setup(m => m.FindByEmailAsync("user@example.com"))
            .ReturnsAsync(user);

        var service = CreateService(mockUserManager);

        var result = await service.RequestPasswordResetAsync("user@example.com");

        result.Success.Should().BeTrue();
    }

    // ── ResetPasswordAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_returns_failure_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(mockUserManager);

        var result = await service.ResetPasswordAsync("unknown@example.com", "123456", "NewPass1!");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_returns_failure_for_invalid_or_expired_code()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@example.com",
            FirstName = "Test"
        };

        mockUserManager
            .Setup(m => m.FindByEmailAsync("user@example.com"))
            .ReturnsAsync(user);

        // No matching PasswordResetRequest in the DB, so code lookup will fail
        var service = CreateService(mockUserManager);

        var result = await service.ResetPasswordAsync("user@example.com", "000000", "NewPass1!");

        result.Success.Should().BeFalse();
    }
}
