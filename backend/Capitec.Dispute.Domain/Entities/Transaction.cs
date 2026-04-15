using Capitec.Dispute.Domain.Entities;

namespace Capitec.Dispute.Domain.Entities;

public class Transaction : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 0;
    public string Currency { get; set; } = "ZAR";
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Completed"; // e.g., Completed, Pending

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
}