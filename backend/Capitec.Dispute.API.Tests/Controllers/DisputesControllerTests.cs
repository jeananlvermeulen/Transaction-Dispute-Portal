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

public class DisputesControllerTests
{
    private const string UserId = "test-user-id";

    private static DisputesController CreateController(
        Mock<IDisputeService> mockService,
        string? userId = UserId)
    {
        var controller = new DisputesController(
            mockService.Object,
            new Mock<IActivityLogger>().Object,
            new Mock<ILogger<DisputesController>>().Object);

        if (userId != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
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
        CreatedAt = DateTime.UtcNow
    };

    // ── CreateDispute ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDispute_returns_201_with_dispute_on_success()
    {
        var mockService = new Mock<IDisputeService>();
        var dispute = SampleDisputeDto();
        mockService
            .Setup(s => s.CreateDisputeAsync(UserId, It.IsAny<CreateDisputeRequestDto>()))
            .ReturnsAsync(dispute);

        var controller = CreateController(mockService);

        var result = await controller.CreateDispute(new CreateDisputeRequestDto
        {
            TransactionId = dispute.TransactionId,
            Reason = "Unauthorised",
            Summary = "I did not authorise this transaction."
        });

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateDispute_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<IDisputeService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.CreateDispute(new CreateDisputeRequestDto
        {
            TransactionId = Guid.NewGuid(),
            Reason = "Unauthorised",
            Summary = "Some valid summary text."
        });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateDispute_returns_400_when_service_throws()
    {
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.CreateDisputeAsync(It.IsAny<string>(), It.IsAny<CreateDisputeRequestDto>()))
            .ThrowsAsync(new Exception("Invalid dispute reason"));

        var controller = CreateController(mockService);

        var result = await controller.CreateDispute(new CreateDisputeRequestDto
        {
            TransactionId = Guid.NewGuid(),
            Reason = "BadReason",
            Summary = "Some valid summary text."
        });

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    // ── GetUserDisputes ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserDisputes_returns_200_with_list()
    {
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.GetUserDisputesAsync(UserId, 1, 10))
            .ReturnsAsync(new DisputeListDto
            {
                Disputes = new[] { SampleDisputeDto(), SampleDisputeDto() },
                TotalCount = 2
            });

        var controller = CreateController(mockService);

        var result = await controller.GetUserDisputes();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetUserDisputes_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<IDisputeService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.GetUserDisputes();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── GetDisputeById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDisputeById_returns_200_when_found()
    {
        var disputeId = Guid.NewGuid();
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.GetDisputeByIdAsync(disputeId))
            .ReturnsAsync(SampleDisputeDto(disputeId));

        var controller = CreateController(mockService);

        var result = await controller.GetDisputeById(disputeId);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetDisputeById_returns_404_when_not_found()
    {
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.GetDisputeByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((DisputeDto?)null);

        var controller = CreateController(mockService);

        var result = await controller.GetDisputeById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>()
            .Which.StatusCode.Should().Be(404);
    }

    // ── GetDisputeDetail ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetDisputeDetail_returns_200_with_history()
    {
        var disputeId = Guid.NewGuid();
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.GetDisputeDetailAsync(disputeId))
            .ReturnsAsync(new DisputeDetailDto
            {
                Dispute = SampleDisputeDto(disputeId),
                StatusHistory = new[]
                {
                    new DisputeStatusHistoryDto
                    {
                        OldStatus = "Submitted",
                        NewStatus = "Submitted",
                        EmployeeName = "System",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            });

        var controller = CreateController(mockService);

        var result = await controller.GetDisputeDetail(disputeId);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetDisputeDetail_returns_404_when_not_found()
    {
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.GetDisputeDetailAsync(It.IsAny<Guid>()))
            .ReturnsAsync((DisputeDetailDto?)null);

        var controller = CreateController(mockService);

        var result = await controller.GetDisputeDetail(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>()
            .Which.StatusCode.Should().Be(404);
    }

    // ── UpdateDisputeStatus ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDisputeStatus_returns_200_on_success()
    {
        var disputeId = Guid.NewGuid();
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.UpdateDisputeStatusAsync(
                disputeId, UserId, It.IsAny<UpdateDisputeStatusDto>()))
            .ReturnsAsync(true);

        var controller = CreateController(mockService);

        var result = await controller.UpdateDisputeStatus(
            disputeId,
            new UpdateDisputeStatusDto { NewStatus = "UnderReview", Notes = "Under review" });

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateDisputeStatus_returns_404_when_dispute_not_found()
    {
        var mockService = new Mock<IDisputeService>();
        mockService
            .Setup(s => s.UpdateDisputeStatusAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<UpdateDisputeStatusDto>()))
            .ReturnsAsync(false);

        var controller = CreateController(mockService);

        var result = await controller.UpdateDisputeStatus(
            Guid.NewGuid(),
            new UpdateDisputeStatusDto { NewStatus = "UnderReview" });

        result.Should().BeOfType<NotFoundResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateDisputeStatus_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<IDisputeService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.UpdateDisputeStatus(
            Guid.NewGuid(),
            new UpdateDisputeStatusDto { NewStatus = "UnderReview" });

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
