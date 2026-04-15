using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Infrastructure.Data;
using Capitec.Dispute.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Capitec.Dispute.Infrastructure.Tests.Services;

public class TransactionServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static TransactionService CreateService(ApplicationDbContext context)
    {
        var logger = new Mock<ILogger<TransactionService>>().Object;
        return new TransactionService(context, logger);
    }

    private static async Task<Transaction> SeedTransaction(
        ApplicationDbContext context,
        string userId = "user-1",
        decimal amount = 500m)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Currency = "ZAR",
            Description = "Test purchase",
            Date = DateTime.UtcNow.AddDays(-1),
            Status = "Completed"
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    // ── GetTransactionByIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionByIdAsync_returns_transaction_for_correct_user()
    {
        using var context = CreateContext();
        var tx = await SeedTransaction(context, userId: "user-1");
        var service = CreateService(context);

        var result = await service.GetTransactionByIdAsync(tx.Id, "user-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(tx.Id);
        result.Amount.Should().Be(500m);
        result.Currency.Should().Be("ZAR");
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetTransactionByIdAsync_returns_null_when_transaction_belongs_to_different_user()
    {
        using var context = CreateContext();
        var tx = await SeedTransaction(context, userId: "user-1");
        var service = CreateService(context);

        var result = await service.GetTransactionByIdAsync(tx.Id, "user-2");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTransactionByIdAsync_returns_null_for_nonexistent_id()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetTransactionByIdAsync(Guid.NewGuid(), "user-1");

        result.Should().BeNull();
    }

    // ── GetUserTransactionsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetUserTransactionsAsync_returns_only_requesting_users_transactions()
    {
        using var context = CreateContext();
        await SeedTransaction(context, userId: "user-1", amount: 100m);
        await SeedTransaction(context, userId: "user-1", amount: 200m);
        await SeedTransaction(context, userId: "user-2", amount: 999m);
        var service = CreateService(context);

        var result = await service.GetUserTransactionsAsync("user-1");

        result.TotalCount.Should().Be(2);
        result.Transactions.Should().HaveCount(2);
        result.Transactions.Should().AllSatisfy(t => t.Currency.Should().Be("ZAR"));
    }

    [Fact]
    public async Task GetUserTransactionsAsync_returns_results_ordered_by_date_descending()
    {
        using var context = CreateContext();
        var older = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Amount = 100m,
            Currency = "ZAR",
            Description = "Older",
            Date = DateTime.UtcNow.AddDays(-10),
            Status = "Completed"
        };
        var newer = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Amount = 200m,
            Currency = "ZAR",
            Description = "Newer",
            Date = DateTime.UtcNow.AddDays(-1),
            Status = "Completed"
        };
        context.Transactions.AddRange(older, newer);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetUserTransactionsAsync("user-1");

        result.Transactions.First().Description.Should().Be("Newer");
        result.Transactions.Last().Description.Should().Be("Older");
    }

    [Fact]
    public async Task GetUserTransactionsAsync_returns_paginated_results()
    {
        using var context = CreateContext();
        for (int i = 0; i < 7; i++)
            await SeedTransaction(context, userId: "user-1", amount: 100m * (i + 1));

        var service = CreateService(context);

        var page1 = await service.GetUserTransactionsAsync("user-1", pageNumber: 1, pageSize: 5);
        var page2 = await service.GetUserTransactionsAsync("user-1", pageNumber: 2, pageSize: 5);

        page1.TotalCount.Should().Be(7);
        page1.Transactions.Should().HaveCount(5);
        page2.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_returns_empty_list_when_no_transactions()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetUserTransactionsAsync("nonexistent-user");

        result.TotalCount.Should().Be(0);
        result.Transactions.Should().BeEmpty();
    }

    // ── CreateSimulatedTransactionAsync ──────────────────────────────────────

    [Fact]
    public async Task CreateSimulatedTransactionAsync_creates_transaction_with_correct_defaults()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var txId = await service.CreateSimulatedTransactionAsync("user-1", 750.50m, "Simulated purchase");

        txId.Should().NotBe(Guid.Empty);

        var saved = await context.Transactions.FindAsync(txId);
        saved.Should().NotBeNull();
        saved!.UserId.Should().Be("user-1");
        saved.Amount.Should().Be(750.50m);
        saved.Currency.Should().Be("ZAR");
        saved.Description.Should().Be("Simulated purchase");
        saved.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task CreateSimulatedTransactionAsync_persists_to_database()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        await service.CreateSimulatedTransactionAsync("user-1", 100m, "Test");

        var count = await context.Transactions.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task CreateSimulatedTransactionAsync_sets_date_within_current_month_and_not_in_future()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var before = DateTime.UtcNow;

        var txId = await service.CreateSimulatedTransactionAsync("user-1", 100m, "Test");

        var saved = await context.Transactions.FindAsync(txId);
        var startOfMonth = new DateTime(before.Year, before.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        saved!.Date.Should().BeOnOrAfter(startOfMonth);
        saved.Date.Should().BeOnOrBefore(before);
    }
}
