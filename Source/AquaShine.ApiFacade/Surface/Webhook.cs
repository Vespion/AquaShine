using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using AquaShine.ApiHub.Data.Access;
using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite;
using AquaShine.ApiHub.Eventbrite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AquaShine.ApiFacade.Surface
{
    public class Webhook
    {
        private readonly ApiSerialiser _apiSerialiser;
        private readonly ApiClient _apiClient;
        private readonly IDataContext _dataContext;
        private readonly SmtpClient _smtpClient;

        public Webhook(ApiSerialiser apiSerialiser, ApiClient apiClient, IDataContext dataContext, SmtpClient smtpClient)
        {
            _apiSerialiser = apiSerialiser;
            _apiClient = apiClient;
            _dataContext = dataContext;
            _smtpClient = smtpClient;
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
            var mailTasks = new List<Task>(order.Attendees!.Count);
            foreach (var orderAttendee in order.Attendees!)
            {
                var entrant = _apiSerialiser.ConvertEntrant(orderAttendee);
                var dbTask = _dataContext.Create(entrant).ConfigureAwait(false);
                mailTasks.Add(_smtpClient.SendMailAsync(await GenerateMailMessage(entrant)));
                await dbTask;
            }

            await Task.WhenAll(mailTasks);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        private async Task<MailMessage> GenerateMailMessage(Entrant entrant)
        {
            throw new NotImplementedException();
        }
    }
}
