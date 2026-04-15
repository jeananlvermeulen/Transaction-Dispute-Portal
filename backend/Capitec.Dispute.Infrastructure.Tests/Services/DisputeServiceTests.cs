using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Domain.Enums;
using Capitec.Dispute.Infrastructure.Data;
using Capitec.Dispute.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Capitec.Dispute.Infrastructure.Tests.Services;

public class DisputeServiceTests
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

    private static DisputeService CreateService(ApplicationDbContext context,
        ITranslationService? translationService = null)
    {
        var logger = new Mock<ILogger<DisputeService>>().Object;
        var emailService = new Mock<IEmailService>().Object;
        var userManager = CreateMockUserManager().Object;
        translationService ??= new Mock<ITranslationService>().Object;
        return new DisputeService(context, emailService, userManager, logger, translationService);
    }

    // ── Seed helpers ─────────────────────────────────────────────────────────

    private static async Task<(User user, Transaction transaction)> SeedUserAndTransaction(
        ApplicationDbContext context, string userId = "user-1")
    {
        var user = new User
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 250.00m,
            Currency = "ZAR",
            Description = "Test Transaction",
            Date = DateTime.UtcNow,
            Status = "Completed"
        };

        context.Users.Add(user);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        return (user, transaction);
    }

    // ── CreateDisputeAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDisputeAsync_returns_dto_with_Submitted_status()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var request = new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Unauthorised",
            Summary = "I did not authorise this transaction at all."
        };

        var result = await service.CreateDisputeAsync("user-1", request);

        result.Should().NotBeNull();
        result.Status.Should().Be("Submitted");
        result.Reason.Should().Be("Unauthorised");
        result.TransactionId.Should().Be(transaction.Id);
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateDisputeAsync_creates_initial_status_history_entry()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var request = new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "IncorrectAmount",
            Summary = "The amount charged was different from what I agreed to pay."
        };

        var result = await service.CreateDisputeAsync("user-1", request);

        var history = await context.DisputeStatusHistories
            .Where(h => h.DisputeId == result.Id)
            .ToListAsync();

        history.Should().HaveCount(1);
        history[0].NewStatus.Should().Be(DisputeStatus.Submitted);
        history[0].Notes.Should().Be("Dispute created");
    }

    [Fact]
    public async Task CreateDisputeAsync_throws_for_invalid_reason()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var request = new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "INVALID_REASON",
            Summary = "Some valid summary text here."
        };

        Func<Task> act = () => service.CreateDisputeAsync("user-1", request);

        await act.Should().ThrowAsync<Exception>().WithMessage("*Invalid dispute reason*");
    }

    // ── GetDisputeByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDisputeByIdAsync_returns_dispute_with_transaction()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var createRequest = new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Other",
            Summary = "This is a valid summary for the test dispute."
        };
        var created = await service.CreateDisputeAsync("user-1", createRequest);

        var result = await service.GetDisputeByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Transaction.Should().NotBeNull();
        result.Transaction!.Id.Should().Be(transaction.Id);
        result.Transaction.Amount.Should().Be(250.00m);
    }

    [Fact]
    public async Task GetDisputeByIdAsync_returns_null_for_nonexistent_id()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetDisputeByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetUserDisputesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUserDisputesAsync_returns_only_requesting_users_disputes()
    {
        using var context = CreateContext();
        var (_, tx1) = await SeedUserAndTransaction(context, "user-1");
        var (_, tx2) = await SeedUserAndTransaction(context, "user-2");
        var service = CreateService(context);

        await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = tx1.Id,
            Reason = "Unauthorised",
            Summary = "Dispute belonging to user one here."
        });
        await service.CreateDisputeAsync("user-2", new CreateDisputeRequestDto
        {
            TransactionId = tx2.Id,
            Reason = "Unauthorised",
            Summary = "Dispute belonging to user two here."
        });

        var result = await service.GetUserDisputesAsync("user-1");

        result.TotalCount.Should().Be(1);
        result.Disputes.Should().AllSatisfy(d => d.Status.Should().Be("Submitted"));
    }

    [Fact]
    public async Task GetUserDisputesAsync_returns_paginated_results()
    {
        using var context = CreateContext();
        var (_, tx) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        for (int i = 0; i < 5; i++)
        {
            var newTx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = "user-1",
                Amount = 100m * (i + 1),
                Currency = "ZAR",
                Description = $"Transaction {i}",
                Date = DateTime.UtcNow,
                Status = "Completed"
            };
            context.Transactions.Add(newTx);
            await context.SaveChangesAsync();

            await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
            {
                TransactionId = newTx.Id,
                Reason = "Unauthorised",
                Summary = $"Dispute number {i} with a long enough summary."
            });
        }

        var page1 = await service.GetUserDisputesAsync("user-1", pageNumber: 1, pageSize: 3);
        var page2 = await service.GetUserDisputesAsync("user-1", pageNumber: 2, pageSize: 3);

        page1.TotalCount.Should().Be(5);
        page1.Disputes.Should().HaveCount(3);
        page2.Disputes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserDisputesAsync_returns_empty_list_when_no_disputes()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetUserDisputesAsync("nonexistent-user");

        result.TotalCount.Should().Be(0);
        result.Disputes.Should().BeEmpty();
    }

    // ── UpdateDisputeStatusAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateDisputeStatusAsync_updates_status_and_records_history()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);
        var employeeId = Guid.NewGuid().ToString();

        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Unauthorised",
            Summary = "I did not authorise this transaction at all."
        });

        var updateRequest = new UpdateDisputeStatusDto
        {
            NewStatus = "UnderReview",
            Notes = "Under investigation"
        };
        var result = await service.UpdateDisputeStatusAsync(created.Id, employeeId, updateRequest);

        result.Should().BeTrue();

        var dispute = await context.Disputes.FindAsync(created.Id);
        dispute!.Status.Should().Be(DisputeStatus.UnderReview);

        var latestHistory = await context.DisputeStatusHistories
            .Where(h => h.DisputeId == created.Id)
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync();

        latestHistory!.OldStatus.Should().Be(DisputeStatus.Submitted);
        latestHistory.NewStatus.Should().Be(DisputeStatus.UnderReview);
        latestHistory.Notes.Should().Be("Under investigation");
    }

    [Fact]
    public async Task UpdateDisputeStatusAsync_sets_ResolvedAt_when_resolved()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);
        var employeeId = Guid.NewGuid().ToString();

        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "IncorrectAmount",
            Summary = "The amount charged was more than the agreed amount."
        });

        await service.UpdateDisputeStatusAsync(created.Id, employeeId, new UpdateDisputeStatusDto
        {
            NewStatus = "Resolved",
            Notes = "Refund processed"
        });

        var dispute = await context.Disputes.FindAsync(created.Id);
        dispute!.ResolvedAt.Should().NotBeNull();
        dispute.ResolvedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateDisputeStatusAsync_returns_false_for_nonexistent_dispute()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UpdateDisputeStatusAsync(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            new UpdateDisputeStatusDto { NewStatus = "Pending" });

        result.Should().BeFalse();
    }

    // ── GetAllDisputesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllDisputesAsync_returns_disputes_from_all_users()
    {
        using var context = CreateContext();
        var (_, tx1) = await SeedUserAndTransaction(context, "user-1");
        var (_, tx2) = await SeedUserAndTransaction(context, "user-2");
        var service = CreateService(context);

        await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = tx1.Id,
            Reason = "Unauthorised",
            Summary = "User one dispute with valid summary text."
        });
        await service.CreateDisputeAsync("user-2", new CreateDisputeRequestDto
        {
            TransactionId = tx2.Id,
            Reason = "IncorrectAmount",
            Summary = "User two dispute with valid summary text."
        });

        var result = await service.GetAllDisputesAsync(pageNumber: 1, pageSize: 20);

        result.TotalCount.Should().Be(2);
        result.Disputes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllDisputesAsync_returns_paginated_results()
    {
        using var context = CreateContext();
        var (_, tx) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        for (int i = 0; i < 5; i++)
        {
            var newTx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = "user-1",
                Amount = 100m * (i + 1),
                Currency = "ZAR",
                Description = $"Transaction {i}",
                Date = DateTime.UtcNow,
                Status = "Completed"
            };
            context.Transactions.Add(newTx);
            await context.SaveChangesAsync();

            await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
            {
                TransactionId = newTx.Id,
                Reason = "Unauthorised",
                Summary = $"Valid dispute summary number {i} for testing."
            });
        }

        var page1 = await service.GetAllDisputesAsync(pageNumber: 1, pageSize: 3);
        var page2 = await service.GetAllDisputesAsync(pageNumber: 2, pageSize: 3);

        page1.TotalCount.Should().Be(5);
        page1.Disputes.Should().HaveCount(3);
        page2.Disputes.Should().HaveCount(2);
    }

    // ── GetDisputeByReferenceAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetDisputeByReferenceAsync_returns_dispute_for_valid_reference()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Unauthorised",
            Summary = "I did not authorise this transaction at all."
        });

        var result = await service.GetDisputeByReferenceAsync(created.IncidentReference);

        result.Should().NotBeNull();
        result!.IncidentReference.Should().Be(created.IncidentReference);
        result.Reason.Should().Be("Unauthorised");
    }

    [Fact]
    public async Task GetDisputeByReferenceAsync_returns_null_for_unknown_reference()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetDisputeByReferenceAsync("UNKNOWN999");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDisputeByReferenceAsync_is_case_insensitive()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Other",
            Summary = "Valid summary for reference lookup test case here."
        });

        var result = await service.GetDisputeByReferenceAsync(created.IncidentReference.ToLower());

        result.Should().NotBeNull();
        result!.IncidentReference.Should().Be(created.IncidentReference);
    }

    [Fact]
    public async Task UpdateDisputeStatusAsync_throws_for_invalid_status()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Other",
            Summary = "This is a valid summary for the test dispute."
        });

        Func<Task> act = () => service.UpdateDisputeStatusAsync(
            created.Id,
            Guid.NewGuid().ToString(),
            new UpdateDisputeStatusDto { NewStatus = "INVALID_STATUS" });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Invalid status*");
    }

    // ── GetDisputeDetailAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDisputeDetailAsync_returns_dispute_with_full_status_history()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);
        var employeeId = Guid.NewGuid().ToString();

        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Unauthorised",
            Summary = "I did not authorise this transaction at all."
        });

        await service.UpdateDisputeStatusAsync(created.Id, employeeId, new UpdateDisputeStatusDto
        {
            NewStatus = "Pending",
            Notes = "Assigned for review"
        });

        var result = await service.GetDisputeDetailAsync(created.Id);

        result.Should().NotBeNull();
        result!.Dispute.Id.Should().Be(created.Id);
        result.StatusHistory.Should().HaveCount(2);
        result.StatusHistory.Should().Contain(h => h.NewStatus == "Submitted");
        result.StatusHistory.Should().Contain(h => h.NewStatus == "Pending");
    }

    [Fact]
    public async Task GetDisputeDetailAsync_returns_null_for_nonexistent_dispute()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetDisputeDetailAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDisputeDetailAsync_labels_system_changes_as_System()
    {
        using var context = CreateContext();
        var (_, transaction) = await SeedUserAndTransaction(context);
        var service = CreateService(context);

        var created = await service.CreateDisputeAsync("user-1", new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = "Other",
            Summary = "Valid summary for this particular test case."
        });

        var result = await service.GetDisputeDetailAsync(created.Id);

        // The initial history entry has no ChangedByEmployee
        result!.StatusHistory.First().EmployeeName.Should().Be("System");
    }

    // ── CancelDisputeAsync ────────────────────────────────────────────────────

    private static async Task<Capitec.Dispute.Domain.Entities.Dispute> SeedActiveDispute(
        ApplicationDbContext context, DisputeService service,
        string userId = "user-1", string reason = "Unauthorised")
    {
        var (_, transaction) = await SeedUserAndTransaction(context, userId);
        await service.CreateDisputeAsync(userId, new CreateDisputeRequestDto
        {
            TransactionId = transaction.Id,
            Reason = reason,
            Summary = "Valid summary for cancellation test dispute."
        });
        return await context.Disputes.FirstAsync(d => d.UserId == userId);
    }

    [Fact]
    public async Task CancelDisputeAsync_sets_status_to_Cancelled_and_stores_reason()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var dispute = await SeedActiveDispute(context, service);

        var (success, error) = await service.CancelDisputeAsync(
            dispute.Id, "user-1", "I resolved it directly with the merchant.");

        success.Should().BeTrue();
        error.Should().BeNull();

        var updated = await context.Disputes.FindAsync(dispute.Id);
        updated!.Status.Should().Be(DisputeStatus.Cancelled);
        updated.CancellationReason.Should().Be("I resolved it directly with the merchant.");
        updated.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelDisputeAsync_records_Cancelled_status_history_entry()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var dispute = await SeedActiveDispute(context, service);
        var previousStatus = dispute.Status;

        await service.CancelDisputeAsync(dispute.Id, "user-1", "No longer needed.");

        var latestHistory = await context.DisputeStatusHistories
            .Where(h => h.DisputeId == dispute.Id)
            .OrderByDescending(h => h.CreatedAt)
            .FirstAsync();

        latestHistory.OldStatus.Should().Be(previousStatus);
        latestHistory.NewStatus.Should().Be(DisputeStatus.Cancelled);
        latestHistory.Notes.Should().Be("No longer needed.");
    }

    [Fact]
    public async Task CancelDisputeAsync_returns_error_when_dispute_not_found()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var (success, error) = await service.CancelDisputeAsync(
            Guid.NewGuid(), "user-1", "Some reason.");

        success.Should().BeFalse();
        error.Should().Be("Dispute not found.");
    }

    [Fact]
    public async Task CancelDisputeAsync_returns_error_when_user_does_not_own_dispute()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var dispute = await SeedActiveDispute(context, service, userId: "user-1");

        var (success, error) = await service.CancelDisputeAsync(
            dispute.Id, "other-user", "Trying to cancel someone else's dispute.");

        success.Should().BeFalse();
        error.Should().Contain("not authorised");
    }

    [Fact]
    public async Task CancelDisputeAsync_returns_error_when_dispute_is_already_resolved()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var dispute = await SeedActiveDispute(context, service);

        // Force the dispute into a terminal state
        dispute.Status = DisputeStatus.Resolved;
        await context.SaveChangesAsync();

        var (success, error) = await service.CancelDisputeAsync(
            dispute.Id, "user-1", "Trying to cancel a resolved dispute.");

        success.Should().BeFalse();
        error.Should().Contain("cannot be cancelled");
    }

    [Fact]
    public async Task CancelDisputeAsync_saves_translation_when_service_returns_a_result()
    {
        using var context = CreateContext();

        var translationMock = new Mock<ITranslationService>();
        translationMock
            .Setup(t => t.TranslateToEnglishAsync("Ek het die probleem opgelos."))
            .ReturnsAsync(("I resolved the issue.", "Afrikaans"));

        var service = CreateService(context, translationMock.Object);
        var dispute = await SeedActiveDispute(context, service);

        await service.CancelDisputeAsync(dispute.Id, "user-1", "Ek het die probleem opgelos.");

        var updated = await context.Disputes.FindAsync(dispute.Id);
        updated!.CancellationReasonEnglish.Should().Be("I resolved the issue.");
        updated.CancellationReasonLanguage.Should().Be("Afrikaans");
    }

    [Fact]
    public async Task CancelDisputeAsync_leaves_translation_fields_null_when_service_returns_null()
    {
        using var context = CreateContext();

        var translationMock = new Mock<ITranslationService>();
        translationMock
            .Setup(t => t.TranslateToEnglishAsync(It.IsAny<string>()))
            .ReturnsAsync((null, null));

        var service = CreateService(context, translationMock.Object);
        var dispute = await SeedActiveDispute(context, service);

        var (success, _) = await service.CancelDisputeAsync(
            dispute.Id, "user-1", "Already in English.");

        success.Should().BeTrue();

        var updated = await context.Disputes.FindAsync(dispute.Id);
        updated!.CancellationReasonEnglish.Should().BeNull();
        updated.CancellationReasonLanguage.Should().BeNull();
    }

    [Fact]
    public async Task CancelDisputeAsync_still_succeeds_when_translation_service_throws()
    {
        using var context = CreateContext();

        var translationMock = new Mock<ITranslationService>();
        translationMock
            .Setup(t => t.TranslateToEnglishAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService(context, translationMock.Object);
        var dispute = await SeedActiveDispute(context, service);

        var (success, error) = await service.CancelDisputeAsync(
            dispute.Id, "user-1", "Die betaling was foutief.");

        // Cancellation itself must succeed even if translation blows up
        success.Should().BeTrue();
        error.Should().BeNull();

        var updated = await context.Disputes.FindAsync(dispute.Id);
        updated!.Status.Should().Be(DisputeStatus.Cancelled);
        updated.CancellationReasonEnglish.Should().BeNull();
    }
}
