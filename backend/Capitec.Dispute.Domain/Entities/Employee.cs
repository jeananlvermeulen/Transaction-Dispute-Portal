using Capitec.Dispute.Domain.Entities;

namespace Capitec.Dispute.Domain.Entities;

public class Employee : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;

    // Navigation
    public ICollection<Dispute> AssignedDisputes { get; set; } = new List<Dispute>();
}