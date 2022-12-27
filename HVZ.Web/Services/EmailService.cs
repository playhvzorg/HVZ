using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using HVZ.Web.Settings;

namespace HVZ.Web.Services
{
    public class EmailService
    {
        private SmtpClient client;
        private MailAddress address;
        private string domainName;

        public EmailService(IOptions<EmailServiceOptions> options, IOptions<WebConfig> webConfig)
        {
            var opts = options.Value;
            if (webConfig.Value.DomainName == null)
                throw new ArgumentNullException("DomainName cannot be null");
            domainName = webConfig.Value.DomainName;
            client = new SmtpClient(opts.SmtpHost, opts.Port);
            client.UseDefaultCredentials = false;
            if (opts.EmailId == null)
                throw new ArgumentNullException("EmailId cannot be null.\nSpecify EmailId with 'dotnet user-secrets set \"EmailServiceOptions:EmailId\" {Your email Id}'\nFor more information about user secrets see https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0");
            if (opts.Password == null)
                throw new ArgumentNullException("Password cannot be null.\nSpecify Password with 'dotnet user-secrets set \"EmailServiceOptions:Password\" {Your password}'\nFor more information about user secrets see https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0");
            client.Credentials = new NetworkCredential(opts.EmailId, opts.Password);
            address = new MailAddress(opts.EmailId);
            client.EnableSsl = true;
        }

        public void SendDebug(string email)
        {
            try
            {
                MailMessage msg = new MailMessage();
                msg.Subject = "Sent from my web app!";
                msg.Body = "Hello from my web app!";
                msg.To.Add(email);
                msg.From = address;

                client.Send(msg);
                msg.Dispose();
            }
            catch (SmtpException e)
            {
                // TODO: use logger instead
                System.Console.WriteLine(e);
            }
        }

        public void SendTemplateDebug(string email, string name)
        {
            string htmlBody = String.Format(@"
            
            <div style=""font-family: Arial"">
                <h1>Hello, {0}</h1>
            </div>
            
            ", name);

            MailMessage msg = new MailMessage();
            msg.Subject = "Sent from my web app!";
            msg.Body = htmlBody;
            msg.To.Add(new MailAddress(email));
            msg.From = address;
            msg.IsBodyHtml = true;

            client.Send(msg);
            msg.Dispose();
        }

        public async Task SendVerificationEmailAsync(string to, string name, string requestId)
        {
            string htmlBody = String.Format(@"
            
            <div style=""font-family: Arial"">
                <h1>Confirm your email</h1>
            </div>
            <hr>
            <p style=""font: 14pt Arial;"">Hello {0}</p>
            <p style=""font: 14pt Arial;"">Thank you for signing up at playhvz.org!</p>
            <p style=""font: 14pt Arial;"">Please confirm your identity by verifying your email address</p>

            <a
            style=""
            font: 14pt Arial;
            text-decoration: none;
            background-color: rgb(83, 124, 216);
            color: white;
            padding: 10px 14px 10px 14px;
            border-radius: .25rem;
            ""
            href=""{2}Account/Verify?requestId={1}""
            target=""_blank"">Verify email</a>
            <p style=""margin-bottom: 1px; font: 14pt Arial;"">Or copy and paste this URL into your browser:</p>
            <p style=""font: 14pt Arial;"">{2}Account/Verify?requestId={1}</p>
            ", name, requestId, domainName);

            MailMessage msg = new MailMessage();
            msg.Subject = "PlayHVZ: Confirm email address";
            msg.Body = htmlBody;
            msg.To.Add(new MailAddress(to));
            msg.From = address;
            msg.IsBodyHtml = true;

            client.SendAsync(msg, requestId);
        }

        public async Task SendPasswordChangeEmailAsync(string to, string name, string requestId)
        {
            string htmlBody = String.Format(@"

            <div style=""font-family: Arial"">
                <h1>Reset your password</h1>
            </div>

            <hr>
            <p style=""font: 14pt Arial;"">Hello {0}</p>
            <p style=""font: 14pt Arial;"">Click this button to reset your PlayHVZ account password</p>
            <a
            style=""
            font: 14pt Arial;
            text-decoration: none;
            background-color: rgb(83, 124, 216);
            color: white;
            padding: 10px 14px 10px 14px;
            border-radius: .25rem;
            ""
            href=""{2}Account/Reset?requestId={1}""
            target=""_blank"">Reset password</a>
            <p style=""margin-bottom: 1px; font: 14pt Arial;"">Or copy and paste this URL into your browser:</p>
            <p style=""font: 14pt Arial;"">{2}Account/Reset?requestId={1}</p>
            <hr>
            <p style=""font: 14pt Arial;"">If you did not request a password reset, you can safely delete this email.</p>
            ", name, requestId, domainName);

            MailMessage msg = new MailMessage();
            msg.Subject = "PlayHVZ: Verify email address";
            msg.Body = htmlBody;
            msg.To.Add(new MailAddress(to));
            msg.From = address;
            msg.IsBodyHtml = true;

            client.SendAsync(msg, requestId);
        }
    }
}