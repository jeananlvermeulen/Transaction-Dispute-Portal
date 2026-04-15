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

public class AuthControllerTests
{
    private static AuthController CreateController(Mock<IAuthService> mockAuthService, string? userId = "test-user-id")
    {
        var controller = new AuthController(
            mockAuthService.Object,
            new Mock<IActivityLogger>().Object,
            new Mock<ILogger<AuthController>>().Object);

        if (userId != null)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
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

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_returns_200_with_userId_on_success()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("new-user-id");

        var controller = CreateController(mockAuthService);

        var result = await controller.Register(new RegisterUserRequestDto
        {
            Email = "test@example.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+27821234567"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);

        var body = ok.Value!;
        body.Should().BeEquivalentTo(new
        {
            success = true,
            userId = "new-user-id",
            message = "Registration successful"
        });
    }

    [Fact]
    public async Task Register_returns_400_when_service_throws()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Registration failed: Email already in use"));

        var controller = CreateController(mockAuthService);

        var result = await controller.Register(new RegisterUserRequestDto
        {
            Email = "taken@example.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "+27821234567"
        });

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_returns_200_with_token_on_success()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.LoginAsync("user@example.com", "Password1!"))
            .ReturnsAsync((true, "jwt-token-here", false, (string?)null));

        var controller = CreateController(mockAuthService);

        var result = await controller.Login(new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "Password1!"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);

        var body = ok.Value as AuthResponseDto;
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Token.Should().Be("jwt-token-here");
        body.RequiresMfa.Should().BeFalse();
    }

    [Fact]
    public async Task Login_returns_401_for_invalid_credentials()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "", false, (string?)null));

        var controller = CreateController(mockAuthService);

        var result = await controller.Login(new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "wrongpassword"
        });

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_returns_200_with_RequiresMfa_true_when_mfa_enabled()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((true, "", true, (string?)null));

        var controller = CreateController(mockAuthService);

        var result = await controller.Login(new LoginRequestDto
        {
            Email = "mfauser@example.com",
            Password = "Password1!"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value as AuthResponseDto;
        body!.RequiresMfa.Should().BeTrue();
        body.Token.Should().BeNullOrEmpty();
    }

    // ── VerifyMfaAndLogin ─────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyMfaAndLogin_returns_200_with_token_on_success()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.VerifyMfaCodeAndGetTokenAsync("user@example.com", "123456"))
            .ReturnsAsync((true, "mfa-jwt-token"));

        var controller = CreateController(mockAuthService);

        var result = await controller.VerifyMfaAndLogin(new VerifyMfaRequestDto
        {
            Email = "user@example.com",
            MfaCode = "123456"
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task VerifyMfaAndLogin_returns_401_for_invalid_code()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.VerifyMfaCodeAndGetTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, ""));

        var controller = CreateController(mockAuthService);

        var result = await controller.VerifyMfaAndLogin(new VerifyMfaRequestDto
        {
            Email = "user@example.com",
            MfaCode = "000000"
        });

        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.StatusCode.Should().Be(401);
    }

    // ── GenerateMfa ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateMfa_returns_200_with_qrcode_on_success()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.GenerateMfaSecretAsync("test-user-id"))
            .ReturnsAsync("data:image/png;base64,qrdata");

        var controller = CreateController(mockAuthService);

        var result = await controller.GenerateMfa();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GenerateMfa_returns_401_when_user_claim_missing()
    {
        var mockAuthService = new Mock<IAuthService>();
        var controller = CreateController(mockAuthService, userId: null);

        var result = await controller.GenerateMfa();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GenerateMfa_returns_400_when_service_throws()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.GenerateMfaSecretAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("User not found"));

        var controller = CreateController(mockAuthService);

        var result = await controller.GenerateMfa();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── EnableMfa ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnableMfa_returns_200_when_code_is_valid()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.VerifyMfaCodeAsync(It.IsAny<string>(), "123456"))
            .ReturnsAsync(true);
        mockAuthService
            .Setup(s => s.EnableMfaAsync("test-user-id", "123456"))
            .ReturnsAsync(true);

        var controller = CreateController(mockAuthService);

        var result = await controller.EnableMfa("123456");

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task EnableMfa_returns_400_when_code_is_invalid()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.VerifyMfaCodeAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        mockAuthService
            .Setup(s => s.EnableMfaAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var controller = CreateController(mockAuthService);

        var result = await controller.EnableMfa("000000");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EnableMfa_returns_401_when_user_claim_missing()
    {
        var mockAuthService = new Mock<IAuthService>();
        var controller = CreateController(mockAuthService, userId: null);

        var result = await controller.EnableMfa("123456");

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── ForgotPassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_returns_200_on_success()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.RequestPasswordResetAsync("user@example.com"))
            .ReturnsAsync((true, (string?)null));

        var controller = CreateController(mockAuthService);

        var result = await controller.ForgotPassword(new ForgotPasswordRequestDto
        {
            Email = "user@example.com"
        });

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ForgotPassword_returns_400_when_service_throws()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.RequestPasswordResetAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service error"));

        var controller = CreateController(mockAuthService);

        var result = await controller.ForgotPassword(new ForgotPasswordRequestDto
        {
            Email = "user@example.com"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── ResetPassword ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_returns_400_when_passwords_do_not_match()
    {
        var mockAuthService = new Mock<IAuthService>();
        var controller = CreateController(mockAuthService);

        var result = await controller.ResetPassword(new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "123456",
            NewPassword = "Password1!",
            ConfirmNewPassword = "Different1!"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_returns_200_on_success()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.ResetPasswordAsync("user@example.com", "123456", "NewPass1!"))
            .ReturnsAsync((true, (string?)null));

        var controller = CreateController(mockAuthService);

        var result = await controller.ResetPassword(new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "123456",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        });

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ResetPassword_returns_400_when_reset_fails()
    {
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "Invalid or expired code"));

        var controller = CreateController(mockAuthService);

        var result = await controller.ResetPassword(new ResetPasswordDto
        {
            Email = "user@example.com",
            Code = "000000",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
