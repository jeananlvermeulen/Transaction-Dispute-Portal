using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Domain.Enums;

namespace Capitec.Dispute.Domain.Entities;

public class DisputeStatusHistory : BaseEntity
{
    public Guid DisputeId { get; set; }
    public DisputeStatus OldStatus { get; set; }
    public DisputeStatus NewStatus { get; set; }
    public Guid? ChangedByEmployeeId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Dispute Dispute { get; set; } = null!;
    public Employee? ChangedByEmployee { get; set; }
}