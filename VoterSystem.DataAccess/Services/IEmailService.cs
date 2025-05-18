using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public interface IEmailService
{
    Task<Option<ServiceError>> SendEmailAsync(string to, string subject, string body);
}