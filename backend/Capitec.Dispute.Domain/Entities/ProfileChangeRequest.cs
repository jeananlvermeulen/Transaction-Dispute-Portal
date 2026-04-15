namespace Capitec.Dispute.Domain.Entities;

public class ProfileChangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string PendingFirstName { get; set; } = string.Empty;
    public string PendingLastName { get; set; } = string.Empty;
    public string PendingPhoneNumber { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
