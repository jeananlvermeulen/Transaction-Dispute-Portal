namespace Capitec.Dispute.Application.Interfaces;

public interface IActivityLogger
{
    void CustomerAction(string email, string action, string? detail = null);
    void EmployeeAction(string employeeCode, string email, string action, string? detail = null);
}
