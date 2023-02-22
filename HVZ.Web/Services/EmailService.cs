namespace HVZ.Web.Services;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Settings;

public class EmailService {
    private readonly string domainName;
    private readonly MailAddress mailAddress;
    private readonly SmtpClient smtpClient;

    public EmailService(IOptions<EmailServiceOptions> options, IOptions<WebConfig> webConfig)
    {
        EmailServiceOptions opts = options.Value;
        if (webConfig.Value.DomainName == null)
            throw new ArgumentNullException("DomainName cannot be null");
        domainName = webConfig.Value.DomainName;
        smtpClient = new SmtpClient(opts.SmtpHost, opts.Port);
        smtpClient.UseDefaultCredentials = false;
        if (opts.EmailId == null)
            throw new ArgumentNullException("EmailId cannot be null.\nSpecify EmailId with 'dotnet user-secrets set \"EmailServiceOptions:EmailId\" {Your email Id}'\nFor more information about user secrets see https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0");
        if (opts.Password == null)
            throw new ArgumentNullException("Password cannot be null.\nSpecify Password with 'dotnet user-secrets set \"EmailServiceOptions:Password\" {Your password}'\nFor more information about user secrets see https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0");
        smtpClient.Credentials = new NetworkCredential(opts.EmailId, opts.Password);
        mailAddress = new MailAddress(opts.EmailId);
        smtpClient.EnableSsl = true;
    }



    public async Task SendVerificationEmailAsync(string to, string name, string requestId)
    {
        string htmlBody = await ReadEmailTemplateAsync("VerificationEmailTemplate.html");

        var msg = new MailMessage();
        msg.Subject = "PlayHVZ: Confirm email address";
        msg.Body = string.Format(htmlBody, name, requestId, domainName);
        msg.To.Add(new MailAddress(to));
        msg.From = mailAddress;
        msg.IsBodyHtml = true;

        smtpClient.SendAsync(msg, requestId);
    }

    public async Task SendPasswordChangeEmailAsync(string to, string name, string requestId)
    {
        string htmlBody = await ReadEmailTemplateAsync("PasswordResetEmailTemplate.html");

        var msg = new MailMessage();
        msg.Subject = "PlayHVZ: Verify email address";
        msg.Body = string.Format(htmlBody, name, requestId, domainName);
        msg.To.Add(new MailAddress(to));
        msg.From = mailAddress;
        msg.IsBodyHtml = true;

        smtpClient.SendAsync(msg, requestId);
    }

    private async Task<string> ReadEmailTemplateAsync(string templateName)
    {
        return await File.ReadAllTextAsync($"Services/Templates/{templateName}");
    }
}