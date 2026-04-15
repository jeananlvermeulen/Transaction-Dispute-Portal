using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Capitec.Dispute.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Capitec.Dispute.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(ApplicationDbContext dbContext, ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId, string userId)
    {
        try
        {
            var transaction = await _dbContext.Transactions
                .Where(t => t.Id == transactionId && t.UserId == userId)
                .FirstOrDefaultAsync();

            if (transaction == null) return null;

            return new TransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Description = transaction.Description,
                Date = transaction.Date,
                Status = transaction.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<TransactionListDto> GetUserTransactionsAsync(string userId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var totalCount = await _dbContext.Transactions
                .Where(t => t.UserId == userId)
                .CountAsync();

            var transactions = await _dbContext.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Description = t.Description,
                    Date = t.Date,
                    Status = t.Status
                })
                .ToListAsync();

            return new TransactionListDto
            {
                Transactions = transactions,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Guid> CreateSimulatedTransactionAsync(string userId, decimal amount, string description)
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var totalSeconds = (long)(now - startOfMonth).TotalSeconds;
            var randomSeconds = totalSeconds > 0 ? Random.Shared.NextInt64(0, totalSeconds) : 0;
            var randomDate = startOfMonth.AddSeconds(randomSeconds);

            var transaction = new Transaction
            {
                UserId = userId,
                Amount = amount,
                Currency = "ZAR",
                Description = description,
                Date = randomDate,
                Status = "Completed"
            };

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Simulated transaction created for user {UserId}", userId);
            return transaction.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating simulated transaction for user {UserId}", userId);
            throw;
        }
    }
}