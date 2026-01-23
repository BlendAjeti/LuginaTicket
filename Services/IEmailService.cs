namespace LuginaTicket.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
}

