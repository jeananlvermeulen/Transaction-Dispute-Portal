using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Domain.Enums;

namespace Capitec.Dispute.Domain.Entities;

public class Dispute : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid TransactionId { get; set; }
    public DisputeReason Reason { get; set; }
    public string? CustomReason { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SummaryEnglish { get; set; }
    public string? SummaryLanguage { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Submitted;
    public Guid? AssignedEmployeeId { get; set; }
    public string IncidentReference { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    public DateTime? ResolvedAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationReasonEnglish { get; set; }
    public string? CancellationReasonLanguage { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Transaction Transaction { get; set; } = null!;
    public Employee? AssignedEmployee { get; set; }
    public ICollection<DisputeStatusHistory> StatusHistory { get; set; } = new List<DisputeStatusHistory>();
}