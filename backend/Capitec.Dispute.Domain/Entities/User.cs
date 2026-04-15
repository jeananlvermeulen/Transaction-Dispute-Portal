using Microsoft.AspNetCore.Identity;

namespace Capitec.Dispute.Domain.Entities;

public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty; // Obfuscated for POPI
    public bool IsMfaEnabled { get; set; }

    // Navigation
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
}