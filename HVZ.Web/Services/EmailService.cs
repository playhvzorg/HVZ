using HVZ.Web.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Reflection;

namespace HVZ.Web.Services
{
    public class EmailService
    {
        private SmtpClient smtpClient;
        private MailAddress mailAddress;
        private string domainName;
        private Dictionary<EmailType, string> contentTemplates;
        private string emailTemplate;

        public EmailService(IOptions<EmailServiceOptions> options, IOptions<WebConfig> webConfig)
        {
            var opts = options.Value;
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
            mailAddress = new MailAddress(opts.EmailId, opts.EmailAlias);
            smtpClient.EnableSsl = true;

            // Load resources
            contentTemplates = new Dictionary<EmailType, string>();
            emailTemplate = ReadTextResourceFromAssembly("HVZ.Web.Services.Templates.email_template.html");

            contentTemplates.Add(EmailType.PasswordReset, ReadHtmlTemplateFromAssembly("password_reset"));
            contentTemplates.Add(EmailType.PasswordChange, ReadHtmlTemplateFromAssembly("password_changed"));
            contentTemplates.Add(EmailType.ConfirmEmail, ReadHtmlTemplateFromAssembly("confirm_email"));

        }

        public async Task SendVerificationEmailAsync(string to, string name, string requestId)
        {
            string requestUrl = string.Format("{0}/Account/Verify?requestId={1}", domainName, requestId);
            string htmlBody = string.Format(contentTemplates[EmailType.ConfirmEmail], name, requestUrl);
            await SendHtmlEmailAsync(to, "PLayHVZ: Confirm Email Address", htmlBody);
        }

        public async Task SendPasswordChangeConfirmationEmailAsync(string to, string name)
        {
            string htmlBody = string.Format(contentTemplates[EmailType.PasswordChange], name);
            await SendHtmlEmailAsync(to, "PlayHVZ: Password Changed", htmlBody);
        }

        public async Task SendPasswordResetEmailAsync(string to, string name, string requestId, string userId)
        {
            string requestUrl = string.Format("{0}/Account/Reset?requestId={1}&userId={2}", domainName, requestId, userId);
            string htmlBody = string.Format(contentTemplates[EmailType.PasswordReset], name, requestUrl);
            await SendHtmlEmailAsync(to, "PlayHVZ: Reset Password", htmlBody);
        }

        private async Task SendHtmlEmailAsync(string to, string subject, string body)
        {
            string formattedBody = emailTemplate.Replace("%BODY%", body);
            MailMessage msg = new MailMessage();
            msg.Subject = subject;
            msg.Body = formattedBody;
            msg.To.Add(to);
            msg.From = mailAddress;
            msg.IsBodyHtml = true;

            await smtpClient.SendMailAsync(msg);
        }

        private string ReadEmailWithTemplate(string template, string name)
        {
            return template.Replace("%BODY%", ReadTextResourceFromAssembly($"HVZ.Web.Services.Templates.{name}.html"));
        }

        string ReadHtmlTemplateFromAssembly(string templateName)
        {
            return ReadTextResourceFromAssembly($"HVZ.Web.Services.Templates.{templateName}.html");
        }

        // Solution from: https://stackoverflow.com/a/28558647
        string ReadTextResourceFromAssembly(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream is null)
                    throw new ArgumentException($"Resource '{name}' not found", "name");
                return new StreamReader(stream).ReadToEnd();
            }
        }

        private enum EmailType
        {
            ConfirmEmail,
            PasswordReset,
            PasswordChange
        }
    }
}