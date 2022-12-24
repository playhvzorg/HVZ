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

        public EmailService(IOptions<EmailServiceOptions> options)
        {
            var opts = options.Value;
            client = new SmtpClient(opts.SmtpHost, opts.Port);
            client.UseDefaultCredentials = false;
            if (opts.EmailId == null)
                throw new ArgumentException("EmailId cannot be null.\nSpecify EmailId with 'dotnet user-secrets set \"EmailServiceOptions:EmailId\" {Your email Id}'\nFor more information about user secrets see https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0");
            if (opts.Password == null)
                throw new ArgumentException("Password cannot be null.\nSpecify Password with 'dotnet user-secrets set \"EmailServiceOptions:Password\" {Your password}'\nFor more information about user secrets see https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0");
            client.Credentials = new NetworkCredential(opts.EmailId, opts.Password);
            address = new MailAddress(opts.EmailId);
            client.EnableSsl = true;
        }

        public void SendDebug()
        {
            try
            {
                MailMessage msg = new MailMessage();
                msg.Subject = "Sent from my web app!";
                msg.Body = "Hello from my web app!";
                msg.To.Add(new MailAddress("mcninja232@gmail.com"));
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

        public void SendTemplateDebug()
        {
            string htmlBody = "<link rel=\"stylesheet\" href=\"https://bootstrapbuildspace.sfo2.cdn.digitaloceanspaces.com//gIYUvFGaMeFj/iHIvwvploonO/bootstrap.min.css\" />" +
            "<h1 class=\"jumbotron\">Hello</h1>";

            MailMessage msg = new MailMessage();
            msg.Subject = "Sent from my web app!";
            msg.Body = htmlBody;
            msg.To.Add(new MailAddress("mcninja232@gmail.com"));
            msg.From = new MailAddress("mcninja232@gmail.com", "App");
            msg.IsBodyHtml = true;

            client.Send(msg);
            msg.Dispose();
        }

        public async Task SendVerificationEmailAsync(string to, string requestId)
        {
            // TODO: Implement
        }

        public async Task SendPasswordChangeEmailAsync(string to, string requestId)
        {
            // TODO: Implement
        }
    }
}