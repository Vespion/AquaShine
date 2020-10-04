using AquaShine.Emails.Templates;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

namespace AquaShine.Emails.Client
{
    public class SmtpRelayClient : IMailClient
    {
        private readonly ILogger<SmtpRelayClient> _logger;
        private readonly IOptions<EmailConfig> _config;

        public SmtpRelayClient(ILogger<SmtpRelayClient> logger, IOptions<EmailConfig> config)
        {
            _logger = logger;
            _config = config;
        }

        private async Task<MimeMessage> ConstructMessage<TMessage>(TMessage message, MailboxAddress sender, MailboxAddress target) where TMessage : IEmailMessage
        {
            _logger.LogInformation("Constructing MIME message from template");
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(target);
            mimeMessage.From.Add(sender);
            mimeMessage.Subject = message.Subject;
            mimeMessage.Body = new TextPart(TextFormat.Html)
            {
                Text = await message.Generate()
            };
            _logger.LogDebug("Construction complete");
            return mimeMessage;
        }

        public async Task SendMessage<TMessage>(TMessage message, MailboxAddress sender, MailboxAddress target) where TMessage : IEmailMessage
        {
            using (_logger.BeginScope("Sending message of type {msgType}", typeof(TMessage)))
            {
                var mimeMessage = await ConstructMessage(message, sender, target);
                _logger.LogTrace("Constructing SmtpClient");
                using var client = new SmtpClient();

                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                //client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                try
                {
                    _logger.LogDebug("Smtp client connecting to {targetDomain} on port {port} with ssl flag set to {ssl}",
                        _config.Value.MailServerAddress, _config.Value.MailServerPort, _config.Value.Tls);

                    await client.ConnectAsync(_config.Value.MailServerAddress, _config.Value.MailServerPort, _config.Value.Tls);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error connecting to remote mail server");
                }

                // Note: only needed if the SMTP server requires authentication
                if (!string.IsNullOrWhiteSpace(_config.Value.UserId) || !string.IsNullOrWhiteSpace(_config.Value.UserPassword))
                {
                    _logger.LogDebug("Auth values not null. Authenticating with server");
                    await client.AuthenticateAsync(_config.Value.UserId, _config.Value.UserPassword);
                }

                try
                {
                    _logger.LogInformation("Sending email to target server @ {server}", _config.Value.MailServerAddress);
                    await client.SendAsync(mimeMessage);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error sending mail to remote server");
                }

                _logger.LogInformation("Disconnecting from mail server");
                await client.DisconnectAsync(true);
            }
        }
    }
}