using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Đọc thông tin từ appsettings.json
        var host = _configuration["EmailSettings:SmtpServer"];
        var port = int.Parse(_configuration["EmailSettings:SmtpPort"]);
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];
        var senderEmail = _configuration["EmailSettings:SenderEmail"];

        using (var client = new SmtpClient(host, port))
        {
            client.Credentials = new NetworkCredential(username, password);
            client.EnableSsl = true; // Mailtrap hỗ trợ STARTTLS trên tất cả các cổng

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "LinguistAI Support"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
    }
}