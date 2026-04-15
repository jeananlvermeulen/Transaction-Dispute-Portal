namespace Capitec.Dispute.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error)> SendRegistrationCodeAsync(string email, string firstName);
    Task<string> RegisterAsync(string email, string password, string firstName, string lastName, string phoneNumber, string verificationCode);
    Task<(bool Success, string Token, bool RequiresMfa, string? MfaQrCode)> LoginAsync(string email, string password);
    Task<bool> VerifyMfaCodeAsync(string userId, string code);
    Task<string> GenerateMfaSecretAsync(string userId);
    Task<bool> EnableMfaAsync(string userId, string token);
    Task<bool> DisableMfaAsync(string userId);
    Task<(bool Success, string Token)> VerifyMfaCodeAndGetTokenAsync(string email, string mfaCode);
    Task<(bool Success, string? Error)> RequestPasswordResetAsync(string email);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string code, string newPassword);
}

public interface IUserService
{
    Task<DTOs.UserDto?> GetUserByIdAsync(string userId);
    Task<bool> UpdateUserAsync(string userId, string firstName, string lastName, string phoneNumber);
    Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<(bool Success, string? Error)> RequestPasswordChangeAsync(string userId, string currentPassword, string newPassword);
    Task<(bool Success, string? Error)> ConfirmPasswordChangeAsync(string userId, string code);
    Task<(bool Success, string? Error)> RequestProfileChangeAsync(string userId, string firstName, string lastName, string phoneNumber);
    Task<(bool Success, string? Error)> ConfirmProfileChangeAsync(string userId, string code);
}

public interface ITransactionService
{
    Task<DTOs.TransactionDto?> GetTransactionByIdAsync(Guid transactionId, string userId);
    Task<DTOs.TransactionListDto> GetUserTransactionsAsync(string userId, int pageNumber = 1, int pageSize = 10);
    Task<Guid> CreateSimulatedTransactionAsync(string userId, decimal amount, string description);
}

public interface IEmailService
{
    Task SendDisputeConfirmationAsync(string toEmail, string firstName, string incidentReference, string reason, string summary);
    Task SendStatusUpdateAsync(string toEmail, string firstName, string incidentReference, string oldStatus, string newStatus, string? notes, bool bookCall = false);
    Task SendPasswordChangeCodeAsync(string toEmail, string firstName, string code);
    Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code);
    Task SendRegistrationVerificationCodeAsync(string toEmail, string firstName, string code);
    Task SendDisputeCancellationAsync(string toEmail, string firstName, string incidentReference, string cancellationReason);
    Task SendProfileChangeCodeAsync(string toEmail, string firstName, string code);
}

public interface IEmployeeAuthService
{
    Task<(bool Success, string? Error)> SendRegistrationCodeAsync(string email, string firstName);
    Task<string> RegisterEmployeeAsync(string email, string password, string firstName, string lastName, string phoneNumber, string department, string verificationCode);
    Task<(bool Success, string Token, string EmployeeId)> LoginEmployeeAsync(string email, string password);
}

public interface ITranslationService
{
    Task<(string? TranslatedText, string? SourceLanguage)> TranslateToEnglishAsync(string text);
}

public interface IDisputeService
{
    Task<DTOs.DisputeDto> CreateDisputeAsync(string userId, DTOs.CreateDisputeRequestDto request);
    Task<DTOs.DisputeDto?> GetDisputeByIdAsync(Guid disputeId);
    Task<DTOs.DisputeListDto> GetUserDisputesAsync(string userId, int pageNumber = 1, int pageSize = 10);
    Task<DTOs.DisputeListDto> GetAllDisputesAsync(int pageNumber = 1, int pageSize = 20);
    Task<DTOs.DisputeListDto> GetEmployeeDisputesAsync(string employeeId, int pageNumber = 1, int pageSize = 10);
    Task<bool> UpdateDisputeStatusAsync(Guid disputeId, string employeeId, DTOs.UpdateDisputeStatusDto request);
    Task<DTOs.DisputeDetailDto?> GetDisputeDetailAsync(Guid disputeId);
    Task<DTOs.DisputeDto?> GetDisputeByReferenceAsync(string reference);
    Task<(bool Success, string? Error)> CancelDisputeAsync(Guid disputeId, string userId, string cancellationReason);
}