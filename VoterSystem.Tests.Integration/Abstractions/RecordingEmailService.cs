using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using VoterSystem.DataAccess.Services;
using VoterSystem.Shared.Functional;

namespace VoterSystem.Tests.Integration.Abstractions;

public class RecordingEmailService : IEmailService
{
    private readonly ConcurrentQueue<SentEmail> _sentEmails = new();

    public Task<Option<ServiceError>> SendEmailAsync(string to, string subject, string body)
    {
        _sentEmails.Enqueue(new SentEmail
        {
            To = to,
            Subject = subject,
            Body = body
        });

        return Task.FromResult<Option<ServiceError>>(new Option<ServiceError>.None());
    }

    public void Clear()
    {
        while (_sentEmails.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<SentEmail> GetAll()
    {
        return _sentEmails.ToList();
    }

    public string ExtractLatestTwoFactorCode(string to)
    {
        var email = _sentEmails.LastOrDefault(m => m.To == to && m.Subject.Contains("two-factor", StringComparison.OrdinalIgnoreCase));
        if (email is null)
        {
            throw new InvalidOperationException($"No two-factor email found for {to}");
        }

        var match = Regex.Match(email.Body, @"\b\d{6}\b");
        if (!match.Success)
        {
            throw new InvalidOperationException("No 6 digit code found in email body");
        }

        return match.Value;
    }

    public sealed record SentEmail
    {
        public required string To { get; init; }
        public required string Subject { get; init; }
        public required string Body { get; init; }
    }
}
