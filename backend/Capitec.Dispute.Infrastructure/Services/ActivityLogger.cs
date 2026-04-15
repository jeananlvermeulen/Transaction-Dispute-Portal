using Capitec.Dispute.Application.Interfaces;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Capitec.Dispute.Infrastructure.Services;

public class ActivityLogger : IActivityLogger
{
    private readonly ILogger _customerLog;
    private readonly ILogger _employeeLog;

    private const string Template =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}{NewLine}";

    public ActivityLogger()
    {
        _customerLog = new LoggerConfiguration()
            .WriteTo.File(
                path: "logs/customer/activity-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: Template)
            .CreateLogger();

        _employeeLog = new LoggerConfiguration()
            .WriteTo.File(
                path: "logs/employee/activity-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: Template)
            .CreateLogger();
    }

    public void CustomerAction(string email, string action, string? detail = null)
    {
        var message = detail is null
            ? $"[CUSTOMER] {email} | {action}"
            : $"[CUSTOMER] {email} | {action} | {detail}";
        _customerLog.Information(message);
    }

    public void EmployeeAction(string employeeCode, string email, string action, string? detail = null)
    {
        var id = string.IsNullOrEmpty(employeeCode) ? email : employeeCode;
        var message = detail is null
            ? $"[EMPLOYEE] {id} | {action}"
            : $"[EMPLOYEE] {id} | {action} | {detail}";
        _employeeLog.Information(message);
    }
}
