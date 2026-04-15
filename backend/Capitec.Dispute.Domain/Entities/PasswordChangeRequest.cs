namespace Capitec.Dispute.Domain.Entities;

public class PasswordChangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPasswordHash { get; set; } = string.Empty; // store hashed new pw until confirmed
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
