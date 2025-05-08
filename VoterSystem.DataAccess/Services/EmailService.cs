using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using VoterSystem.DataAccess.Config;
using VoterSystem.DataAccess.Functional;

namespace VoterSystem.DataAccess.Services;

public class EmailService(IOptions<EmailSettings> emailSettingsOptions) : IEmailService
{
    private readonly EmailSettings _emailSettings = emailSettingsOptions.Value;
    
    public async Task<Option<ServiceError>> SendEmailAsync(string to, string subject, string body)
    {
        using MailMessage mail = new MailMessage();
        mail.From = new MailAddress(_emailSettings.FromEmail);
        mail.To.Add(to);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;
        
        using SmtpClient smtp = new SmtpClient(_emailSettings.Host, _emailSettings.Port);
        smtp.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
        smtp.EnableSsl = true;

        try
        {
            await smtp.SendMailAsync(mail);
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError(ex.Message);
        }
    }
}