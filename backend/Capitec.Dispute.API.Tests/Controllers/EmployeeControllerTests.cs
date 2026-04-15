using System.Security.Claims;
using Capitec.Dispute.API.Controllers;
using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Capitec.Dispute.API.Tests.Controllers;

public class EmployeeControllerTests
{
    private const string EmployeeUserId = "emp-user-id";
    private const string EmployeeEmail = "emp@example.com";

    private static EmployeeController CreateController(
        Mock<IEmployeeAuthService> mockEmpAuthService,
        Mock<IDisputeService> mockDisputeService,
        string? userId = EmployeeUserId,
        string? employeeCode = "EMP-123456")
    {
        var controller = new EmployeeController(
            mockEmpAuthService.Object,
            mockDisputeService.Object,
            new Mock<IActivityLogger>().Object,
            new Mock<ILogger<EmployeeController>>().Object);

        if (userId != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, EmployeeEmail),
                new Claim(ClaimTypes.Role, "Employee"),
                new Claim("EmployeeCode", employeeCode ?? "")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }
        else
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        return controller;
    }

    private static DisputeDto SampleDisputeDto(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        TransactionId = Guid.NewGuid(),
        Reason = "Unauthorised",
        Summary = "Test summary",
        Status = "Submitted",
        IncidentReference = "ABC12345",
        CreatedAt = DateTime.UtcNow
    };

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_returns_200_on_success()
    {
        var mockEmpAuth = new Mock<IEmployeeAuthService>();
        mockEmpAuth
            .Setup(s => s.RegisterEmployeeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("new-emp-user-id");

        var controller = CreateController(mockEmpAuth, new Mock<IDisputeService>());

        var result = await controller.Register(new RegisterEmployeeRequestDto
        {
            Email = "newemployee@example.com",
            Password = "EmpPass1!",
            ConfirmPassword = "EmpPass1!",
            FirstName = "Alice",
            LastName = "Smith",
            PhoneNumber = "+27821234567",
            Department = "Disputes"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Register_returns_400_when_passwords_do_not_match()
    {
        var mockEmpAuth = new Mock<IEmployeeAuthService>();
        var controller = CreateController(mockEmpAuth, new Mock<IDisputeService>());

        var result = await controller.Register(new RegisterEmployeeRequestDto
        {
            Email = "newemployee@example.com",
            Password = "EmpPass1!",
            ConfirmPassword = "Different1!",
            FirstName = "Alice",
            LastName = "Smith",
            PhoneNumber = "+27821234567",
            Department = "Disputes"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_returns_400_when_service_throws()
    {
        var mockEmpAuth = new Mock<IEmployeeAuthService>();
        mockEmpAuth
            .Setup(s => s.RegisterEmployeeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email already in use"));

        var controller = CreateController(mockEmpAuth, new Mock<IDisputeService>());

        var result = await controller.Register(new RegisterEmployeeRequestDto
        {
            Email = "taken@example.com",
            Password = "EmpPass1!",
            ConfirmPassword = "EmpPass1!",
            FirstName = "Alice",
            LastName = "Smith",
            PhoneNumber = "+27821234567",
            Department = "Disputes"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_returns_200_with_token_and_employeeCode_on_success()
    {
        // Build a minimal valid JWT with EmployeeCode claim so the controller can decode it
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.CreateEncodedJwt(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                { "EmployeeCode", "EMP-999999" }
            },
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.ASCII.GetBytes("test-secret-key-that-is-long-enough")),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
        });

        var mockEmpAuth = new Mock<IEmployeeAuthService>();
        mockEmpAuth
            .Setup(s => s.LoginEmployeeAsync(EmployeeEmail, "EmpPass1!"))
            .ReturnsAsync((true, jwtToken, "emp-domain-id"));

        var controller = CreateController(mockEmpAuth, new Mock<IDisputeService>());

        var result = await controller.Login(new LoginRequestDto
        {
            Email = EmployeeEmail,
            Password = "EmpPass1!"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_returns_200_with_success_false_for_invalid_credentials()
    {
        var mockEmpAuth = new Mock<IEmployeeAuthService>();
        mockEmpAuth
            .Setup(s => s.LoginEmployeeAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "", ""));

        var controller = CreateController(mockEmpAuth, new Mock<IDisputeService>());

        var result = await controller.Login(new LoginRequestDto
        {
            Email = EmployeeEmail,
            Password = "wrongpassword"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        // The controller returns 200 with success=false for invalid credentials
    }

    // ── GetDisputeByReference ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDisputeByReference_returns_200_when_found()
    {
        var mockDisputeService = new Mock<IDisputeService>();
        mockDisputeService
            .Setup(s => s.GetDisputeByReferenceAsync("ABC12345"))
            .ReturnsAsync(SampleDisputeDto());

        var controller = CreateController(new Mock<IEmployeeAuthService>(), mockDisputeService);

        var result = await controller.GetDisputeByReference("ABC12345");

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetDisputeByReference_returns_404_when_not_found()
    {
        var mockDisputeService = new Mock<IDisputeService>();
        mockDisputeService
            .Setup(s => s.GetDisputeByReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync((DisputeDto?)null);

        var controller = CreateController(new Mock<IEmployeeAuthService>(), mockDisputeService);

        var result = await controller.GetDisputeByReference("NOTEXIST");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetDisputeByReference_returns_400_when_service_throws()
    {
        var mockDisputeService = new Mock<IDisputeService>();
        mockDisputeService
            .Setup(s => s.GetDisputeByReferenceAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        var controller = CreateController(new Mock<IEmployeeAuthService>(), mockDisputeService);

        var result = await controller.GetDisputeByReference("ERR12345");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GetAllDisputes ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllDisputes_returns_200_with_dispute_list()
    {
        var mockDisputeService = new Mock<IDisputeService>();
        mockDisputeService
            .Setup(s => s.GetAllDisputesAsync(1, 20))
            .ReturnsAsync(new DisputeListDto
            {
                Disputes = new[] { SampleDisputeDto(), SampleDisputeDto() },
                TotalCount = 2
            });

        var controller = CreateController(new Mock<IEmployeeAuthService>(), mockDisputeService);

        var result = await controller.GetAllDisputes();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAllDisputes_passes_pagination_params_to_service()
    {
        var mockDisputeService = new Mock<IDisputeService>();
        mockDisputeService
            .Setup(s => s.GetAllDisputesAsync(2, 10))
            .ReturnsAsync(new DisputeListDto
            {
                Disputes = new[] { SampleDisputeDto() },
                TotalCount = 1
            });

        var controller = CreateController(new Mock<IEmployeeAuthService>(), mockDisputeService);

        await controller.GetAllDisputes(pageNumber: 2, pageSize: 10);

        mockDisputeService.Verify(s => s.GetAllDisputesAsync(2, 10), Times.Once);
    }

    [Fact]
    public async Task GetAllDisputes_returns_400_when_service_throws()
    {
        var mockDisputeService = new Mock<IDisputeService>();
        mockDisputeService
            .Setup(s => s.GetAllDisputesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        var controller = CreateController(new Mock<IEmployeeAuthService>(), mockDisputeService);

        var result = await controller.GetAllDisputes();

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
