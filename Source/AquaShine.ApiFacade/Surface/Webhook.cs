using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using AquaShine.ApiHub.Data.Access;
using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite;
using AquaShine.ApiHub.Eventbrite.Models;
using AquaShine.Emails.Client;
using AquaShine.Emails.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;

namespace AquaShine.ApiFacade.Surface
{
    public class Webhook
    {
        private readonly ApiSerialiser _apiSerialiser;
        private readonly ApiClient _apiClient;
        private readonly IDataContext _dataContext;
        private readonly IMailClient _smtpClient;
        private readonly IOptions<EmailConfig> _emailOptions;

        public Webhook(ApiSerialiser apiSerialiser, ApiClient apiClient, IDataContext dataContext, IMailClient smtpClient, IOptions<EmailConfig> emailOptions)
        {
            _apiSerialiser = apiSerialiser;
            _apiClient = apiClient;
            _dataContext = dataContext;
            _smtpClient = smtpClient;
            _emailOptions = emailOptions;
        }

        [FunctionName("Webhook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook")]
            HttpRequest req, ILogger log)
        {
            var payload = _apiSerialiser.DeserialiseWebhookPayload(await req.ReadAsStringAsync());
            return payload.Action switch
            {
                "order.placed" => await OrderPlaced(payload),
                "order.updated" => await OrderUpdated(payload),
                "order.refunded" => await OrderRefunded(payload),
                _ => new StatusCodeResult(StatusCodes.Status501NotImplemented)
            };
        }

        private async Task<IActionResult> OrderRefunded(WebhookPayload payload)
        {
            var order = await _apiClient.FetchOrderFromWebhook(payload.ApiUrl).ConfigureAwait(false);
            foreach (var orderAttendee in order.Attendees!)
            {
                await _dataContext.Delete(_apiSerialiser.ConvertEntrant(orderAttendee, false)).ConfigureAwait(false);
            }
            return new StatusCodeResult(StatusCodes.Status202Accepted);
        }

        private async Task<IActionResult> OrderUpdated(WebhookPayload payload)
        {
            return new StatusCodeResult(StatusCodes.Status100Continue);
        }

        private async Task<IActionResult> OrderPlaced(WebhookPayload payload)
        {
            var order = await _apiClient.FetchOrderFromWebhook(payload.ApiUrl).ConfigureAwait(false);
            foreach (var orderAttendee in order.Attendees!)
            {
                var entrant = _apiSerialiser.ConvertEntrant(orderAttendee);
                await _dataContext.Create(entrant).ConfigureAwait(false);
                await _smtpClient.SendMessage(GenerateMailMessage(entrant),
                    new MailboxAddress(_emailOptions.Value.FromName, _emailOptions.Value.FromAddress),
                    new MailboxAddress(entrant.Name, entrant.Email));
            }
            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        private static IEmailMessage GenerateMailMessage(Entrant entrant)
        {
            var localRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            var azureRoot = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";

            var actualRoot = localRoot ?? azureRoot;
            var msg = new EntrantCreated(Path.Combine(actualRoot, "Compiled")) {Subject = "Thanks for joining! Here's your number"};
            msg.EmailVariables.FirstName = entrant.Name;
            msg.EmailVariables.EntrantNum = long.Parse(entrant.RowKey, NumberStyles.HexNumber, new NumberFormatInfo());
            return msg;
        }
    }
}
