using Capitec.Dispute.Application.Interfaces;
using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Capitec.Dispute.Infrastructure.Services;

public class DisputeService : IDisputeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DisputeService> _logger;
    private readonly ITranslationService _translationService;

    public DisputeService(ApplicationDbContext dbContext, IEmailService emailService, UserManager<User> userManager, ILogger<DisputeService> logger, ITranslationService translationService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
        _translationService = translationService;
    }

    public async Task<DisputeDto> CreateDisputeAsync(string userId, CreateDisputeRequestDto request)
    {
        try
        {
            if (!Enum.TryParse<DisputeReason>(request.Reason, out var reason))
                throw new Exception("Invalid dispute reason");

            var dispute = new Capitec.Dispute.Domain.Entities.Dispute
            {
                UserId = userId,
                TransactionId = request.TransactionId,
                Reason = reason,
                CustomReason = request.CustomReason,
                Summary = request.Summary,
                Status = DisputeStatus.Submitted
            };

            _dbContext.Disputes.Add(dispute);
            await _dbContext.SaveChangesAsync();

            // Create initial status history
            var history = new DisputeStatusHistory
            {
                DisputeId = dispute.Id,
                OldStatus = DisputeStatus.Submitted,
                NewStatus = DisputeStatus.Submitted,
                Notes = "Dispute created"
            };

            _dbContext.DisputeStatusHistories.Add(history);
            await _dbContext.SaveChangesAsync();

            // Translate summary to English so employees can read it regardless of submission language.
            // Non-blocking: if translation fails the dispute is unaffected.
            if (!string.IsNullOrWhiteSpace(request.Summary))
            {
                try
                {
                    var (translated, sourceLang) = await _translationService.TranslateToEnglishAsync(request.Summary);
                    if (!string.IsNullOrWhiteSpace(translated) && translated != request.Summary)
                    {
                        dispute.SummaryEnglish = translated;
                        dispute.SummaryLanguage = sourceLang;
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Summary translation failed for dispute {DisputeId}", dispute.Id);
                }
            }

            _logger.LogInformation("Dispute {DisputeId} created for user {UserId}", dispute.Id, userId);

            var dto = new DisputeDto
            {
                Id = dispute.Id,
                TransactionId = dispute.TransactionId,
                Reason = dispute.Reason.ToString(),
                CustomReason = dispute.CustomReason,
                Summary = dispute.Summary,
                SummaryEnglish = dispute.SummaryEnglish,
                Status = dispute.Status.ToString(),
                IncidentReference = dispute.IncidentReference,
                CreatedAt = dispute.CreatedAt
            };

            // Send confirmation email (non-blocking — failure does not affect the dispute)
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email != null)
                _ = _emailService.SendDisputeConfirmationAsync(
                    user.Email,
                    user.FirstName,
                    dispute.IncidentReference,
                    dispute.Reason.ToString(),
                    dispute.Summary);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dispute for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DisputeDto?> GetDisputeByIdAsync(Guid disputeId)
    {
        try
        {
            var dispute = await _dbContext.Disputes
                .Include(d => d.Transaction)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null) return null;

            return new DisputeDto
            {
                Id = dispute.Id,
                TransactionId = dispute.TransactionId,
                Reason = dispute.Reason.ToString(),
                CustomReason = dispute.CustomReason,
                Summary = dispute.Summary,
                Status = dispute.Status.ToString(),
                IncidentReference = dispute.IncidentReference,
                CreatedAt = dispute.CreatedAt,
                ResolvedAt = dispute.ResolvedAt,
                Transaction = new TransactionDto
                {
                    Id = dispute.Transaction!.Id,
                    Amount = dispute.Transaction.Amount,
                    Currency = dispute.Transaction.Currency,
                    Description = dispute.Transaction.Description,
                    Date = dispute.Transaction.Date,
                    Status = dispute.Transaction.Status
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute {DisputeId}", disputeId);
            throw;
        }
    }

    public async Task<DisputeListDto> GetUserDisputesAsync(string userId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var totalCount = await _dbContext.Disputes
                .Where(d => d.UserId == userId)
                .CountAsync();

            var disputes = await _dbContext.Disputes
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DisputeDto
                {
                    Id = d.Id,
                    TransactionId = d.TransactionId,
                    Reason = d.Reason.ToString(),
                    Summary = d.Summary,
                    Status = d.Status.ToString(),
                    IncidentReference = d.IncidentReference,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    ResolvedAt = d.ResolvedAt,
                    CancellationReason = d.CancellationReason
                })
                .ToListAsync();

            return new DisputeListDto
            {
                Disputes = disputes,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DisputeListDto> GetAllDisputesAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            var totalCount = await _dbContext.Disputes.CountAsync();

            var disputes = await _dbContext.Disputes
                .Include(d => d.User)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DisputeDto
                {
                    Id = d.Id,
                    TransactionId = d.TransactionId,
                    Reason = d.Reason.ToString(),
                    Summary = d.Summary,
                    SummaryEnglish = d.SummaryEnglish,
                    SummaryLanguage = d.SummaryLanguage,
                    Status = d.Status.ToString(),
                    IncidentReference = d.IncidentReference,
                    CreatedAt = d.CreatedAt,
                    ResolvedAt = d.ResolvedAt,
                    CancellationReason = d.CancellationReason,
                    CancellationReasonEnglish = d.CancellationReasonEnglish,
                    CancellationReasonLanguage = d.CancellationReasonLanguage,
                    CustomerEmail = d.User != null ? d.User.Email : null
                })
                .ToListAsync();

            return new DisputeListDto { Disputes = disputes, TotalCount = totalCount };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all disputes");
            throw;
        }
    }

    public async Task<DisputeListDto> GetEmployeeDisputesAsync(string employeeId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var employeeGuid = Guid.Parse(employeeId);
            var totalCount = await _dbContext.Disputes
                .Where(d => d.AssignedEmployeeId == employeeGuid)
                .CountAsync();

            var disputes = await _dbContext.Disputes
                .Where(d => d.AssignedEmployeeId == employeeGuid)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DisputeDto
                {
                    Id = d.Id,
                    TransactionId = d.TransactionId,
                    Reason = d.Reason.ToString(),
                    Summary = d.Summary,
                    Status = d.Status.ToString(),
                    IncidentReference = d.IncidentReference,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return new DisputeListDto
            {
                Disputes = disputes,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<bool> UpdateDisputeStatusAsync(Guid disputeId, string employeeId, UpdateDisputeStatusDto request)
    {
        try
        {
            var dispute = await _dbContext.Disputes
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute == null) return false;

            if (!Enum.TryParse<DisputeStatus>(request.NewStatus, out var newStatus))
                throw new Exception("Invalid status");

            var oldStatus = dispute.Status;
            dispute.Status = newStatus;
            dispute.UpdatedAt = DateTime.UtcNow;
            // Resolve Identity user ID to domain Employee record
            var identityUser = await _userManager.FindByIdAsync(employeeId);
            var employee = identityUser != null
                ? await _dbContext.Employees.FirstOrDefaultAsync(e => e.Email == identityUser.Email)
                : null;

            if (employee != null)
                dispute.AssignedEmployeeId = employee.Id;

            if (newStatus == DisputeStatus.Resolved || newStatus == DisputeStatus.Rejected)
                dispute.ResolvedAt = DateTime.UtcNow;

            _dbContext.Disputes.Update(dispute);

            var history = new DisputeStatusHistory
            {
                DisputeId = disputeId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByEmployeeId = employee?.Id,
                Notes = request.Notes
            };

            _dbContext.DisputeStatusHistories.Add(history);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Dispute {DisputeId} status updated to {Status}", disputeId, newStatus);

            // Send status update email to customer (non-blocking)
            if (dispute.User?.Email != null)
                _ = _emailService.SendStatusUpdateAsync(
                    dispute.User.Email,
                    dispute.User.FirstName ?? "Customer",
                    dispute.IncidentReference,
                    oldStatus.ToString(),
                    newStatus.ToString(),
                    request.Notes,
                    request.BookCall);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dispute {DisputeId}", disputeId);
            throw;
        }
    }

    public async Task<DisputeDetailDto?> GetDisputeDetailAsync(Guid disputeId)
    {
        try
        {
            var dispute = await _dbContext.Disputes
                .Include(d => d.Transaction)
                .Include(d => d.StatusHistory)
                .ThenInclude(h => h.ChangedByEmployee)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null) return null;

            var disputeDto = new DisputeDto
            {
                Id = dispute.Id,
                TransactionId = dispute.TransactionId,
                Reason = dispute.Reason.ToString(),
                CustomReason = dispute.CustomReason,
                Summary = dispute.Summary,
                Status = dispute.Status.ToString(),
                IncidentReference = dispute.IncidentReference,
                CreatedAt = dispute.CreatedAt,
                ResolvedAt = dispute.ResolvedAt,
                CancellationReason = dispute.CancellationReason,
                CancellationReasonEnglish = dispute.CancellationReasonEnglish,
                CancellationReasonLanguage = dispute.CancellationReasonLanguage
            };

            var historyDtos = dispute.StatusHistory.Select(h => new DisputeStatusHistoryDto
            {
                Id = h.Id,
                OldStatus = h.OldStatus.ToString(),
                NewStatus = h.NewStatus.ToString(),
                EmployeeName = h.ChangedByEmployee != null ? $"{h.ChangedByEmployee.FirstName} {h.ChangedByEmployee.LastName}" : "System",
                Notes = h.Notes,
                CreatedAt = h.CreatedAt
            }).ToList();

            return new DisputeDetailDto
            {
                Dispute = disputeDto,
                StatusHistory = historyDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute detail {DisputeId}", disputeId);
            throw;
        }
    }

    public async Task<DisputeDto?> GetDisputeByReferenceAsync(string reference)
    {
        try
        {
            var dispute = await _dbContext.Disputes
                .Include(d => d.Transaction)
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.IncidentReference == reference.Trim().ToUpper());

            if (dispute == null) return null;

            return new DisputeDto
            {
                Id = dispute.Id,
                TransactionId = dispute.TransactionId,
                Reason = dispute.Reason.ToString(),
                CustomReason = dispute.CustomReason,
                Summary = dispute.Summary,
                SummaryEnglish = dispute.SummaryEnglish,
                SummaryLanguage = dispute.SummaryLanguage,
                Status = dispute.Status.ToString(),
                IncidentReference = dispute.IncidentReference,
                CreatedAt = dispute.CreatedAt,
                ResolvedAt = dispute.ResolvedAt,
                CancellationReason = dispute.CancellationReason,
                CancellationReasonEnglish = dispute.CancellationReasonEnglish,
                CancellationReasonLanguage = dispute.CancellationReasonLanguage,
                CustomerEmail = dispute.User?.Email,
                Transaction = dispute.Transaction != null ? new TransactionDto
                {
                    Id = dispute.Transaction.Id,
                    Amount = dispute.Transaction.Amount,
                    Currency = dispute.Transaction.Currency,
                    Description = dispute.Transaction.Description,
                    Date = dispute.Transaction.Date,
                    Status = dispute.Transaction.Status
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute by reference {Reference}", reference);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> CancelDisputeAsync(Guid disputeId, string userId, string cancellationReason)
    {
        try
        {
            // Load without .Include(d => d.User) so the Identity User entity (which carries a
            // ConcurrencyStamp concurrency token) is never brought into the tracking context.
            // This prevents a DbUpdateConcurrencyException on the second SaveChangesAsync
            // that saves the translation fields.
            var dispute = await _dbContext.Disputes
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute == null)
                return (false, "Dispute not found.");

            if (dispute.UserId != userId)
                return (false, "You are not authorised to cancel this dispute.");

            var cancellableStatuses = new[] { DisputeStatus.Submitted, DisputeStatus.Pending, DisputeStatus.UnderReview };
            if (!cancellableStatuses.Contains(dispute.Status))
                return (false, "This dispute cannot be cancelled in its current status.");

            var oldStatus = dispute.Status;
            dispute.Status = DisputeStatus.Cancelled;
            dispute.CancellationReason = cancellationReason;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.UpdatedAt = DateTime.UtcNow;

            var history = new DisputeStatusHistory
            {
                DisputeId = disputeId,
                OldStatus = oldStatus,
                NewStatus = DisputeStatus.Cancelled,
                Notes = cancellationReason
            };

            _dbContext.DisputeStatusHistories.Add(history);
            await _dbContext.SaveChangesAsync();

            // Translate cancellation reason to English so employees can read it regardless of language.
            try
            {
                var (translated, sourceLang) = await _translationService.TranslateToEnglishAsync(cancellationReason);
if (!string.IsNullOrWhiteSpace(translated))
                {
                    dispute.CancellationReasonEnglish = translated;
                    dispute.CancellationReasonLanguage = sourceLang;
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cancellation reason translation failed for dispute {DisputeId}", disputeId);
            }

            _logger.LogInformation("Dispute {DisputeId} cancelled by user {UserId}", disputeId, userId);

            // Load the user now (separately from the dispute) purely for the email send.
            var user = await _userManager.FindByIdAsync(dispute.UserId);
            if (user?.Email != null)
                _ = _emailService.SendDisputeCancellationAsync(
                    user.Email,
                    user.FirstName ?? "Customer",
                    dispute.IncidentReference,
                    cancellationReason);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling dispute {DisputeId}", disputeId);
            throw;
        }
    }
}