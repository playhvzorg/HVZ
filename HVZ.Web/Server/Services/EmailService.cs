using HVZ.Web.Server.Services.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Reflection;

namespace HVZ.Web.Server.Services
{
    public class EmailService
    {
        private readonly SmtpClient smtpClient;
        private readonly MailAddress mailAddress;
        private readonly string domainName;
        private readonly Dictionary<EmailType, string> contentTemplates;
        private readonly string emailTemplate;

        public EmailService(IOptions<EmailConfig> options, IOptions<WebConfig> webConfig)
        {
            var opts = options.Value;
            domainName = webConfig.Value.DomainName;
            smtpClient = new SmtpClient(opts.SmtpHost, opts.Port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(opts.EmailId, opts.Password)
            };
            mailAddress = new MailAddress(opts.EmailId, opts.EmailAlais);
            smtpClient.EnableSsl = true;

            // Load Resources
            emailTemplate = ReadTextResourceFromAssembly("HVZ.Web.Server.Services.Resources.email_template.html");
            contentTemplates = new Dictionary<EmailType, string> {
                { EmailType.ConfirmEmail, ReadEmailTemplate("confirm_email") },
                { EmailType.ForgotPassword, ReadEmailTemplate("forgot_password") },
                { EmailType.PasswordChanged, ReadEmailTemplate("password_changed") }
            };
        }

        public async Task SendVerificationEmailAsync(string to, string name, string requestId)
        {
            string requestUrl = $"{domainName}/Account/Verify?requestId={requestId}";
            string htmlBody = emailTemplate.Replace("%BODY%", string.Format(contentTemplates[EmailType.ConfirmEmail], name, requestUrl));
            await SendHtmlEmailAsync(to, "PlayHVZ: Confirm your email", htmlBody);
        }

        public async Task SendForgotPasswordEmailAsync(string to, string name, string requestId, string userId)
        {
            string requestUrl = $"{domainName}/Account/Reset?requestId={requestId}&userId={userId}";
            string htmlBody = emailTemplate.Replace("%BODY%", string.Format(contentTemplates[EmailType.ForgotPassword], name, requestUrl));
            await SendHtmlEmailAsync(to, "PlayHVZ: Reset your password", htmlBody);
        }

        public async Task SendPasswordChangedEmailAsync(string to, string name)
        {
            string htmlBody = emailTemplate.Replace("%BODY%", string.Format(contentTemplates[EmailType.PasswordChanged], name));
            await SendHtmlEmailAsync(to, "PlayHVZ: Password changed", htmlBody);
        }

        private async Task SendHtmlEmailAsync(string to, string subject, string body)
        {
            string formattedBody = emailTemplate.Replace("%BODY%", body);
            MailMessage msg = new()
            {
                Subject = subject,
                Body = formattedBody
            };
            msg.To.Add(to);
            msg.From = mailAddress;
            msg.IsBodyHtml = true;

            await smtpClient.SendMailAsync(msg);
        }

        private static string ReadTextResourceFromAssembly(string name)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) 
                ?? throw new ArgumentException($"Resource '{name}' not found", nameof(name));
            return new StreamReader(stream).ReadToEnd();
        }

        private static string ReadEmailTemplate(string fileName)
        {
            return ReadTextResourceFromAssembly($"HVZ.Web.Server.Services.Resources.{fileName}.html");
        }

        private enum EmailType
        {
            ConfirmEmail,
            ForgotPassword,
            PasswordChanged
        }
    }
}
