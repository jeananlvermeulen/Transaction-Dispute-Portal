using Capitec.Dispute.Domain.Entities;

namespace Capitec.Dispute.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = "Email"; // Email or SMS
    public string Message { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed

    // Navigation
    public User User { get; set; } = null!;
}