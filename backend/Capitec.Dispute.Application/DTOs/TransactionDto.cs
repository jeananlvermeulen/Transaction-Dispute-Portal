namespace Capitec.Dispute.Application.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Currency { get; set; } = "ZAR";
}

public class TransactionListDto
{
    public IEnumerable<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    public int TotalCount { get; set; }
}