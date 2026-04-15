namespace Capitec.Dispute.Application.DTOs;

public class DisputeDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CustomReason { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SummaryEnglish { get; set; }
    public string? SummaryLanguage { get; set; }
    public string Status { get; set; } = string.Empty;
    public string IncidentReference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationReasonEnglish { get; set; }
    public string? CancellationReasonLanguage { get; set; }
    public TransactionDto? Transaction { get; set; }
    public string? CustomerEmail { get; set; }
}

public class CreateDisputeRequestDto
{
    public Guid TransactionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CustomReason { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class CancelDisputeRequestDto
{
    public string CancellationReason { get; set; } = string.Empty;
}

public class UpdateDisputeStatusDto
{
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool BookCall { get; set; }
}

public class DisputeListDto
{
    public IEnumerable<DisputeDto> Disputes { get; set; } = new List<DisputeDto>();
    public int TotalCount { get; set; }
}

public class DisputeDetailDto
{
    public DisputeDto Dispute { get; set; } = new();
    public IEnumerable<DisputeStatusHistoryDto> StatusHistory { get; set; } = new List<DisputeStatusHistoryDto>();
}

public class DisputeStatusHistoryDto
{
    public Guid Id { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}