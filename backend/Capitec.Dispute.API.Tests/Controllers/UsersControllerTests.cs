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

public class UsersControllerTests
{
    private const string UserId = "test-user-id";
    private const string UserEmail = "test@example.com";

    private static UsersController CreateController(
        Mock<IUserService> mockUserService,
        string? userId = UserId,
        string? userEmail = UserEmail)
    {
        var controller = new UsersController(
            mockUserService.Object,
            new Mock<IActivityLogger>().Object,
            new Mock<ILogger<UsersController>>().Object);

        if (userId != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, userEmail ?? "")
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

    private static UserDto SampleUserDto() => new()
    {
        Id = UserId,
        Email = UserEmail,
        FirstName = "John",
        LastName = "Doe",
        PhoneNumber = "+27821234567",
        AccountNumber = "****john",
        IsMfaEnabled = false
    };

    // ── GetCurrentUser ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUser_returns_200_with_user_data()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.GetUserByIdAsync(UserId))
            .ReturnsAsync(SampleUserDto());

        var controller = CreateController(mockService);

        var result = await controller.GetCurrentUser();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetCurrentUser_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<IUserService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.GetCurrentUser();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCurrentUser_returns_404_when_user_not_found()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.GetUserByIdAsync(UserId))
            .ReturnsAsync((UserDto?)null);

        var controller = CreateController(mockService);

        var result = await controller.GetCurrentUser();

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── UpdateProfile ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_returns_200_on_success()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.UpdateUserAsync(UserId, "Jane", "Smith", "+27831234567"))
            .ReturnsAsync(true);

        var controller = CreateController(mockService);

        var result = await controller.UpdateProfile(new UpdateUserRequestDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "+27831234567"
        });

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateProfile_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<IUserService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.UpdateProfile(new UpdateUserRequestDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "+27831234567"
        });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateProfile_returns_404_when_user_not_found()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var controller = CreateController(mockService);

        var result = await controller.UpdateProfile(new UpdateUserRequestDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "+27831234567"
        });

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_returns_200_on_success()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.ChangePasswordAsync(UserId, "OldPass1!", "NewPass1!"))
            .ReturnsAsync((true, (string?)null));

        var controller = CreateController(mockService);

        var result = await controller.ChangePassword(new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        });

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ChangePassword_returns_400_when_passwords_do_not_match()
    {
        var mockService = new Mock<IUserService>();
        var controller = CreateController(mockService);

        var result = await controller.ChangePassword(new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "Different1!"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_returns_400_when_service_returns_error()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "Current password is incorrect"));

        var controller = CreateController(mockService);

        var result = await controller.ChangePassword(new ChangePasswordRequestDto
        {
            CurrentPassword = "WrongPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<IUserService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.ChangePassword(new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── RequestPasswordChange ─────────────────────────────────────────────────

    [Fact]
    public async Task RequestPasswordChange_returns_200_on_success()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.RequestPasswordChangeAsync(UserId, "OldPass1!", "NewPass1!"))
            .ReturnsAsync((true, (string?)null));

        var controller = CreateController(mockService);

        var result = await controller.RequestPasswordChange(new RequestPasswordChangeDto
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        });

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RequestPasswordChange_returns_400_when_passwords_do_not_match()
    {
        var mockService = new Mock<IUserService>();
        var controller = CreateController(mockService);

        var result = await controller.RequestPasswordChange(new RequestPasswordChangeDto
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "Different1!"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RequestPasswordChange_returns_400_when_current_password_wrong()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.RequestPasswordChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "Current password is incorrect"));

        var controller = CreateController(mockService);

        var result = await controller.RequestPasswordChange(new RequestPasswordChangeDto
        {
            CurrentPassword = "WrongPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── ConfirmPasswordChange ─────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmPasswordChange_returns_200_on_success()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.ConfirmPasswordChangeAsync(UserId, "123456"))
            .ReturnsAsync((true, (string?)null));

        var controller = CreateController(mockService);

        var result = await controller.ConfirmPasswordChange(new ConfirmPasswordChangeDto
        {
            Code = "123456"
        });

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ConfirmPasswordChange_returns_400_when_code_invalid()
    {
        var mockService = new Mock<IUserService>();
        mockService
            .Setup(s => s.ConfirmPasswordChangeAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "Incorrect verification code"));

        var controller = CreateController(mockService);

        var result = await controller.ConfirmPasswordChange(new ConfirmPasswordChangeDto
        {
            Code = "000000"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ConfirmPasswordChange_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<IUserService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.ConfirmPasswordChange(new ConfirmPasswordChangeDto
        {
            Code = "123456"
        });

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
