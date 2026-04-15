using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Infrastructure.Data;
using Capitec.Dispute.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Capitec.Dispute.Infrastructure.Tests.Services;

public class UserServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Mock<UserManager<User>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static UserService CreateService(Mock<UserManager<User>> mockUserManager, ApplicationDbContext context)
    {
        var logger = new Mock<ILogger<UserService>>().Object;
        var emailService = new Mock<IEmailService>().Object;
        return new UserService(mockUserManager.Object, logger, context, emailService);
    }

    // ── GetUserByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_returns_dto_when_user_found()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = "user-1",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+27821234567",
            AccountNumber = "****john",
            IsMfaEnabled = false
        };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var result = await service.GetUserByIdAsync("user-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("user-1");
        result.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.AccountNumber.Should().Be("****john");
        result.IsMfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserByIdAsync_returns_null_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var result = await service.GetUserByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    // ── UpdateUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_returns_true_and_updates_fields_on_success()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-1", FirstName = "Old", LastName = "Name", PhoneNumber = "+27000000000" };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var result = await service.UpdateUserAsync("user-1", "Jane", "Smith", "+27831234567");

        result.Should().BeTrue();
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.PhoneNumber.Should().Be("+27831234567");
    }

    [Fact]
    public async Task UpdateUserAsync_returns_false_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var result = await service.UpdateUserAsync("nonexistent", "Jane", "Smith", "+27831234567");

        result.Should().BeFalse();
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_returns_failure_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.ChangePasswordAsync("nonexistent", "OldPass1!", "NewPass1!");

        success.Should().BeFalse();
        error.Should().Be("User not found");
    }

    [Fact]
    public async Task ChangePasswordAsync_returns_failure_for_wrong_current_password()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-1" };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.ChangePasswordAsync(user, "WrongPass1!", "NewPass1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.ChangePasswordAsync("user-1", "WrongPass1!", "NewPass1!");

        success.Should().BeFalse();
        error.Should().Contain("Incorrect password");
    }

    [Fact]
    public async Task ChangePasswordAsync_returns_success_for_valid_credentials()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-1" };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.ChangePasswordAsync(user, "OldPass1!", "NewPass1!"))
            .ReturnsAsync(IdentityResult.Success);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.ChangePasswordAsync("user-1", "OldPass1!", "NewPass1!");

        success.Should().BeTrue();
        error.Should().BeNull();
    }

    // ── ConfirmPasswordChangeAsync ────────────────────────────────────────────

    [Fact]
    public async Task ConfirmPasswordChangeAsync_returns_failure_when_no_pending_request()
    {
        var mockUserManager = CreateMockUserManager();
        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.ConfirmPasswordChangeAsync("user-1", "123456");

        success.Should().BeFalse();
        error.Should().Contain("No pending");
    }

    [Fact]
    public async Task ConfirmPasswordChangeAsync_returns_failure_for_expired_code()
    {
        var mockUserManager = CreateMockUserManager();
        using var context = CreateContext();

        context.PasswordChangeRequests.Add(new PasswordChangeRequest
        {
            UserId = "user-1",
            Code = "123456",
            NewPasswordHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.ConfirmPasswordChangeAsync("user-1", "123456");

        success.Should().BeFalse();
        error.Should().Contain("expired");
    }

    [Fact]
    public async Task ConfirmPasswordChangeAsync_returns_failure_for_wrong_code()
    {
        var mockUserManager = CreateMockUserManager();
        using var context = CreateContext();

        context.PasswordChangeRequests.Add(new PasswordChangeRequest
        {
            UserId = "user-1",
            Code = "999999",
            NewPasswordHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.ConfirmPasswordChangeAsync("user-1", "123456");

        success.Should().BeFalse();
        error.Should().Contain("Incorrect");
    }

    [Fact]
    public async Task ConfirmPasswordChangeAsync_applies_password_and_marks_request_used_for_correct_code()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-1" };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        using var context = CreateContext();

        context.PasswordChangeRequests.Add(new PasswordChangeRequest
        {
            UserId = "user-1",
            Code = "123456",
            NewPasswordHash = "new-hashed-password",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.ConfirmPasswordChangeAsync("user-1", "123456");

        success.Should().BeTrue();
        error.Should().BeNull();
        user.PasswordHash.Should().Be("new-hashed-password");

        var request = await context.PasswordChangeRequests.SingleAsync(r => r.UserId == "user-1");
        request.Used.Should().BeTrue();
    }

    // ── RequestPasswordChangeAsync ────────────────────────────────────────────

    [Fact]
    public async Task RequestPasswordChangeAsync_returns_failure_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.RequestPasswordChangeAsync("nonexistent", "OldPass1!", "NewPass1!");

        success.Should().BeFalse();
        error.Should().Be("User not found");
    }

    [Fact]
    public async Task RequestPasswordChangeAsync_returns_failure_for_wrong_current_password()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-1", Email = "test@example.com", FirstName = "Test" };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.CheckPasswordAsync(user, "WrongPass1!")).ReturnsAsync(false);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.RequestPasswordChangeAsync("user-1", "WrongPass1!", "NewPass1!");

        success.Should().BeFalse();
        error.Should().Contain("incorrect");
    }

    [Fact]
    public async Task RequestPasswordChangeAsync_creates_pending_request_for_valid_input()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-1", Email = "test@example.com", FirstName = "Test" };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.CheckPasswordAsync(user, "OldPass1!")).ReturnsAsync(true);
        // PasswordHasher is non-virtual — set it directly rather than via Moq Setup
        mockUserManager.Object.PasswordHasher = new PasswordHasher<User>();

        using var context = CreateContext();
        var service = CreateService(mockUserManager, context);

        var (success, error) = await service.RequestPasswordChangeAsync("user-1", "OldPass1!", "NewPass1!");

        success.Should().BeTrue();
        error.Should().BeNull();

        var request = await context.PasswordChangeRequests.SingleAsync(r => r.UserId == "user-1");
        request.NewPasswordHash.Should().NotBeNullOrEmpty();
        request.Used.Should().BeFalse();
        request.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RequestPasswordChangeAsync_invalidates_existing_unused_request_before_creating_new_one()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-1", Email = "test@example.com", FirstName = "Test" };
        mockUserManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.CheckPasswordAsync(user, "OldPass1!")).ReturnsAsync(true);
        mockUserManager.Object.PasswordHasher = new PasswordHasher<User>();

        using var context = CreateContext();

        context.PasswordChangeRequests.Add(new PasswordChangeRequest
        {
            UserId = "user-1",
            Code = "old-code",
            NewPasswordHash = "old-hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, context);

        await service.RequestPasswordChangeAsync("user-1", "OldPass1!", "NewPass1!");

        var requests = await context.PasswordChangeRequests
            .Where(r => r.UserId == "user-1")
            .ToListAsync();

        requests.Should().HaveCount(1);
        requests[0].NewPasswordHash.Should().NotBeNullOrEmpty();
        requests[0].NewPasswordHash.Should().NotBe("old-hash");
    }
}
