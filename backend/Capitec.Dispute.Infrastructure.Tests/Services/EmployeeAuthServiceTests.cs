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

public class EmployeeAuthServiceTests
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

    private static Mock<RoleManager<IdentityRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null!, null!, null!, null!);
    }

    private static IConfiguration CreateConfiguration()
    {
        var mockConfig = new Mock<IConfiguration>();
        var mockJwtSection = new Mock<IConfigurationSection>();
        mockConfig.Setup(c => c.GetSection("Jwt")).Returns(mockJwtSection.Object);
        mockJwtSection.Setup(s => s["SecretKey"]).Returns("test-secret-key-that-is-long-enough-for-hmac256");
        mockJwtSection.Setup(s => s["TokenExpirationMinutes"]).Returns("60");
        mockJwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        mockJwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        return mockConfig.Object;
    }

    private static EmployeeAuthService CreateService(
        Mock<UserManager<User>> mockUserManager,
        Mock<RoleManager<IdentityRole>> mockRoleManager,
        ApplicationDbContext context)
    {
        return new EmployeeAuthService(
            mockUserManager.Object,
            mockRoleManager.Object,
            context,
            CreateConfiguration(),
            new Mock<IEmailService>().Object,
            new Mock<ILogger<EmployeeAuthService>>().Object);
    }

    // ── LoginEmployeeAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task LoginEmployeeAsync_returns_failure_when_user_not_found()
    {
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        var (success, token, employeeId) = await service.LoginEmployeeAsync("unknown@example.com", "Pass1!");

        success.Should().BeFalse();
        token.Should().BeEmpty();
        employeeId.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginEmployeeAsync_returns_failure_for_wrong_password()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "emp-id", Email = "emp@example.com" };
        mockUserManager.Setup(m => m.FindByEmailAsync("emp@example.com")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.CheckPasswordAsync(user, "wrongpassword")).ReturnsAsync(false);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        var (success, token, _) = await service.LoginEmployeeAsync("emp@example.com", "wrongpassword");

        success.Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginEmployeeAsync_returns_failure_when_user_is_not_in_employee_role()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User { Id = "user-id", Email = "customer@example.com" };
        mockUserManager.Setup(m => m.FindByEmailAsync("customer@example.com")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Pass1!")).ReturnsAsync(true);
        mockUserManager.Setup(m => m.IsInRoleAsync(user, "Employee")).ReturnsAsync(false);

        using var context = CreateContext();
        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        var (success, token, _) = await service.LoginEmployeeAsync("customer@example.com", "Pass1!");

        success.Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginEmployeeAsync_returns_token_for_valid_employee()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "emp@example.com",
            UserName = "emp@example.com",
            FirstName = "Alice",
            LastName = "Smith"
        };
        mockUserManager.Setup(m => m.FindByEmailAsync("emp@example.com")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.CheckPasswordAsync(user, "EmpPass1!")).ReturnsAsync(true);
        mockUserManager.Setup(m => m.IsInRoleAsync(user, "Employee")).ReturnsAsync(true);

        using var context = CreateContext();
        context.Employees.Add(new Employee
        {
            Email = "emp@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            Department = "Disputes",
            EmployeeCode = "EMP-123456"
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        var (success, token, employeeId) = await service.LoginEmployeeAsync("emp@example.com", "EmpPass1!");

        success.Should().BeTrue();
        token.Should().NotBeNullOrEmpty();
        employeeId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginEmployeeAsync_token_contains_employee_role_claim()
    {
        var mockUserManager = CreateMockUserManager();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "emp@example.com",
            UserName = "emp@example.com",
            FirstName = "Bob",
            LastName = "Jones"
        };
        mockUserManager.Setup(m => m.FindByEmailAsync("emp@example.com")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.CheckPasswordAsync(user, "EmpPass1!")).ReturnsAsync(true);
        mockUserManager.Setup(m => m.IsInRoleAsync(user, "Employee")).ReturnsAsync(true);

        using var context = CreateContext();
        context.Employees.Add(new Employee
        {
            Email = "emp@example.com",
            FirstName = "Bob",
            LastName = "Jones",
            Department = "IT",
            EmployeeCode = "EMP-999999"
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        var (_, token, _) = await service.LoginEmployeeAsync("emp@example.com", "EmpPass1!");

        // Decode and verify the token contains the expected EmployeeCode claim
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "EmployeeCode" && c.Value == "EMP-999999");
        jwt.Claims.Should().Contain(c => c.Value == "Employee");
    }

    // ── RegisterEmployeeAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RegisterEmployeeAsync_throws_when_no_verification_code_found()
    {
        var mockUserManager = CreateMockUserManager();
        using var context = CreateContext();
        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        Func<Task> act = () => service.RegisterEmployeeAsync(
            "new@example.com", "Pass1!", "Bob", "Jones", "+27821234567", "IT", "000000");

        await act.Should().ThrowAsync<Exception>().WithMessage("*No verification code found*");
    }

    [Fact]
    public async Task RegisterEmployeeAsync_throws_when_code_is_expired()
    {
        var mockUserManager = CreateMockUserManager();
        using var context = CreateContext();

        context.EmailVerificationRequests.Add(new EmailVerificationRequest
        {
            Email = "new@example.com",
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        Func<Task> act = () => service.RegisterEmployeeAsync(
            "new@example.com", "Pass1!", "Bob", "Jones", "+27821234567", "IT", "123456");

        await act.Should().ThrowAsync<Exception>().WithMessage("*expired*");
    }

    [Fact]
    public async Task RegisterEmployeeAsync_throws_when_code_is_incorrect()
    {
        var mockUserManager = CreateMockUserManager();
        using var context = CreateContext();

        context.EmailVerificationRequests.Add(new EmailVerificationRequest
        {
            Email = "new@example.com",
            Code = "999999",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, CreateMockRoleManager(), context);

        Func<Task> act = () => service.RegisterEmployeeAsync(
            "new@example.com", "Pass1!", "Bob", "Jones", "+27821234567", "IT", "123456");

        await act.Should().ThrowAsync<Exception>().WithMessage("*Incorrect verification code*");
    }

    [Fact]
    public async Task RegisterEmployeeAsync_creates_employee_record_and_marks_code_used_for_valid_input()
    {
        var mockUserManager = CreateMockUserManager();
        var mockRoleManager = CreateMockRoleManager();

        mockRoleManager.Setup(r => r.RoleExistsAsync("Employee")).ReturnsAsync(true);
        mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Employee"))
            .ReturnsAsync(IdentityResult.Success);

        using var context = CreateContext();

        context.EmailVerificationRequests.Add(new EmailVerificationRequest
        {
            Email = "new@example.com",
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, mockRoleManager, context);

        await service.RegisterEmployeeAsync(
            "new@example.com", "Pass1!", "Bob", "Jones", "+27821234567", "IT", "123456");

        var employee = await context.Employees.SingleAsync(e => e.Email == "new@example.com");
        employee.FirstName.Should().Be("Bob");
        employee.LastName.Should().Be("Jones");
        employee.Department.Should().Be("IT");
        employee.EmployeeCode.Should().StartWith("EMP-");

        var request = await context.EmailVerificationRequests.FirstAsync(r => r.Email == "new@example.com");
        request.Used.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterEmployeeAsync_throws_when_identity_create_fails()
    {
        var mockUserManager = CreateMockUserManager();
        var mockRoleManager = CreateMockRoleManager();

        mockRoleManager.Setup(r => r.RoleExistsAsync("Employee")).ReturnsAsync(true);
        mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already in use" }));

        using var context = CreateContext();

        context.EmailVerificationRequests.Add(new EmailVerificationRequest
        {
            Email = "taken@example.com",
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(mockUserManager, mockRoleManager, context);

        Func<Task> act = () => service.RegisterEmployeeAsync(
            "taken@example.com", "Pass1!", "Bob", "Jones", "+27821234567", "IT", "123456");

        await act.Should().ThrowAsync<Exception>().WithMessage("*Email already in use*");
    }
}
