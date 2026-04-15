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

public class TransactionsControllerTests
{
    private const string UserId = "test-user-id";

    private static TransactionsController CreateController(
        Mock<ITransactionService> mockService,
        string? userId = UserId)
    {
        var controller = new TransactionsController(
            mockService.Object,
            new Mock<ILogger<TransactionsController>>().Object);

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

    private static TransactionDto SampleTransactionDto(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Amount = 350.00m,
        Currency = "ZAR",
        Description = "Test purchase",
        Date = DateTime.UtcNow,
        Status = "Completed"
    };

    // ── GetTransactions ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_returns_200_with_list()
    {
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(s => s.GetUserTransactionsAsync(UserId, 1, 10))
            .ReturnsAsync(new TransactionListDto
            {
                Transactions = new[] { SampleTransactionDto(), SampleTransactionDto() },
                TotalCount = 2
            });

        var controller = CreateController(mockService);

        var result = await controller.GetTransactions();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetTransactions_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<ITransactionService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.GetTransactions();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetTransactions_passes_pagination_params_to_service()
    {
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(s => s.GetUserTransactionsAsync(UserId, 2, 5))
            .ReturnsAsync(new TransactionListDto
            {
                Transactions = new[] { SampleTransactionDto() },
                TotalCount = 1
            });

        var controller = CreateController(mockService);

        await controller.GetTransactions(pageNumber: 2, pageSize: 5);

        mockService.Verify(s => s.GetUserTransactionsAsync(UserId, 2, 5), Times.Once);
    }

    // ── GetTransaction ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransaction_returns_200_when_found()
    {
        var txId = Guid.NewGuid();
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(s => s.GetTransactionByIdAsync(txId, UserId))
            .ReturnsAsync(SampleTransactionDto(txId));

        var controller = CreateController(mockService);

        var result = await controller.GetTransaction(txId);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetTransaction_returns_404_when_not_found()
    {
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(s => s.GetTransactionByIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((TransactionDto?)null);

        var controller = CreateController(mockService);

        var result = await controller.GetTransaction(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetTransaction_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<ITransactionService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.GetTransaction(Guid.NewGuid());

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── SimulateTransaction ───────────────────────────────────────────────────

    [Fact]
    public async Task SimulateTransaction_returns_201_on_success()
    {
        var newId = Guid.NewGuid();
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(s => s.CreateSimulatedTransactionAsync(UserId, 500m, "Test simulate"))
            .ReturnsAsync(newId);

        var controller = CreateController(mockService);

        var result = await controller.SimulateTransaction(new CreateTransactionDto
        {
            Amount = 500m,
            Description = "Test simulate"
        });

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task SimulateTransaction_returns_401_when_user_claim_missing()
    {
        var mockService = new Mock<ITransactionService>();
        var controller = CreateController(mockService, userId: null);

        var result = await controller.SimulateTransaction(new CreateTransactionDto
        {
            Amount = 100m,
            Description = "Test"
        });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task SimulateTransaction_returns_400_when_service_throws()
    {
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(s => s.CreateSimulatedTransactionAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Simulation error"));

        var controller = CreateController(mockService);

        var result = await controller.SimulateTransaction(new CreateTransactionDto
        {
            Amount = -100m,
            Description = "Bad simulate"
        });

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }
}
