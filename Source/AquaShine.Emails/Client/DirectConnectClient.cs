using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AquaShine.Emails.Templates;
using DnsClient;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;

namespace AquaShine.Emails.Client
{
    public class DirectConnectClient : IMailClient
    {
        private readonly ILogger<DirectConnectClient> _logger;
        private readonly ILookupClient _lookupClient;
        private static bool _loggerInit;

        public DirectConnectClient(ILogger<DirectConnectClient> logger, ILoggerFactory factory)
        {
            _logger = logger;
            if (!_loggerInit)
            {
                _logger.LogDebug("DNSClient logging not wrapped. Constructing wrapper from factory");
                Logging.LoggerFactory = new LoggerFactoryWrapper(factory);
                _loggerInit = true;
            }
            _lookupClient = new LookupClient();
        }

        public async Task SendMessage<TMessage>(TMessage message, MailboxAddress sender, MailboxAddress target) where TMessage : IEmailMessage
        {
            using (_logger.BeginScope("Sending message of type {msgType}", typeof(TMessage)))
            {
                var mimeMessage = await ConstructMessage(message, sender, target);
                _logger.LogTrace("Constructing SmtpClient");
                using var client = new SmtpClient();
                await foreach (var targetMailServerDomain in FetchMxRecordsForDomain(target))
                {
                    const int port = 25;
                    const bool sslFlag = false;
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)  
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    try
                    {
                        _logger.LogDebug(
                            "Smtp client connecting to {targetDomain} on port {port} with ssl flag set to {ssl}",
                            targetMailServerDomain, port, sslFlag);
                        await client.ConnectAsync(targetMailServerDomain, port, sslFlag);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error connecting to remote mail server");
                        continue;
                    }

                    // Note: only needed if the SMTP server requires authentication  
                    //client.Authenticate("joey", "password");  
                    try
                    {
                        _logger.LogInformation("Sending email to target server @ {server}", targetMailServerDomain);
                        await client.SendAsync(mimeMessage);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error sending mail to remote server");
                    }

                    _logger.LogInformation("Disconnecting from mail server");
                    await client.DisconnectAsync(true);
                    break; // assuming we get to this point, we have sent the message... else will fail  
                }
            }
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

        private async IAsyncEnumerable<DnsString> FetchMxRecordsForDomain(MailboxAddress target)
        {
            _logger.LogInformation("Searching for target mail server");
            var host = target.Address.Split('@')[1];
            _logger.LogDebug("Extracted host from address ({host})", host);
            if (host.StartsWith("."))
            {
                _logger.LogTrace("Host had a leading period. Removing");
                host = host.Remove(0, 1);
            }
            _logger.LogDebug("Running DNS query");
            var result = await _lookupClient.QueryAsync(host, QueryType.MX);
            var mxRecords = result.Answers.MxRecords().OrderBy(x => x.Preference);
            foreach (var record in mxRecords)
            {
                yield return record.Exchange;
            }
        }
    }
}
