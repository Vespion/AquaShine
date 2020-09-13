using AquaShine.ApiHub.Eventbrite;
using AquaShine.ApiHub.Eventbrite.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AquaShine.ApiHub.Tests.Eventbrite
{
    public class ApiClientTests : IDisposable
    {
        public static readonly WebhookPayload Payload = new WebhookPayload("order.placed", new Uri("https://www.eventbriteapi.com/v3/orders/1428494963/"));

        public const string TestResponseBody =
            @"{""costs"": {""base_price"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""eventbrite_fee"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""gross"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""payment_fee"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""tax"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}}, ""resource_uri"": ""https://www.eventbriteapi.com/v3/orders/1428494963/"", ""id"": ""1428494963"", ""changed"": ""2020-08-20T15:27:25Z"", ""created"": ""2020-08-20T15:27:23Z"", ""name"": ""Jonathon Spice"", ""first_name"": ""Jonathon"", ""last_name"": ""Spice"", ""email"": ""jonathon.spice@gmail.com"", ""status"": ""placed"", ""payment_type"": null, ""time_remaining"": null, ""event_id"": ""116518944299"", ""attendees"": [{""team"": null, ""costs"": {""base_price"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""eventbrite_fee"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""gross"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""payment_fee"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}, ""tax"": {""display"": ""\u00a30.00"", ""currency"": ""GBP"", ""value"": 0, ""major_value"": ""0.00""}}, ""resource_uri"": ""https://www.eventbriteapi.com/v3/events/116518944299/attendees/1993680763/"", ""id"": ""1993680763"", ""changed"": ""2020-08-20T15:27:23Z"", ""created"": ""2020-08-20T15:27:23Z"", ""quantity"": 1, ""variant_id"": null, ""profile"": {""first_name"": ""Jonathon"", ""last_name"": ""Spice"", ""addresses"": {""ship"": {""city"": ""Sheffield"", ""region"": ""South Yorkshire"", ""postal_code"": ""S61rt"", ""address_1"": ""5 Eastgate"", ""country"": ""GB""}}, ""gender"": ""female"", ""email"": ""jonathon.spice@gmail.com"", ""name"": ""Jonathon Spice""}, ""barcodes"": [{""status"": ""unused"", ""barcode"": ""14284949631993680763001"", ""created"": ""2020-08-20T15:27:25Z"", ""changed"": ""2020-08-20T15:27:25Z"", ""checkin_type"": 0, ""is_printed"": false}], ""answers"": [], ""checked_in"": false, ""cancelled"": false, ""refunded"": false, ""affiliate"": ""oddtdteb"", ""guestlist_id"": null, ""invited_by"": null, ""status"": ""Attending"", ""ticket_class_name"": ""General Admission 2"", ""delivery_method"": ""electronic"", ""event_id"": ""116518944299"", ""order_id"": ""1428494963"", ""ticket_class_id"": ""199421599""}]}";

        private Mock<HttpMessageHandler> CreateHttpClientMock()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.SetupRequest(request =>
            {
                var pass = request.RequestUri == new Uri(@"https://www.eventbriteapi.com/v3/orders/1428494963/?expand=attendees") &&
                           request.Headers.Authorization.Parameter == Configuration["EventbriteToken"] && request.Headers.Authorization.Scheme == "Bearer" &&
                    request.Method == HttpMethod.Get;

                return pass;
            }).ReturnsResponse(HttpStatusCode.OK, message =>
            {
                message.Content =
                        new StringContent(TestResponseBody, Encoding.UTF8, MediaTypeNames.Application.Json);
            });
            return mock;
        }

        public ApiClientTests()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<ApiClientTests>();

            Configuration = builder.Build();
            _mockedClient = CreateHttpClientMock();
        }

        public IConfigurationRoot Configuration { get; set; }

        private readonly Mock<HttpMessageHandler> _mockedClient;

        [Fact]
        public async Task FetchesAndDecodes()
        {
            using var http = _mockedClient.CreateClient();
            var client = new ApiClient(http, Configuration);
            var order = await client.FetchOrderFromWebhook(Payload.ApiUrl);

            Assert.Equal(JsonSerializer.Serialize(new[] { new Attendee { Id = "1993680763", Profile = new AttendeeProfile { Email = "jonathon.spice@gmail.com", FirstName = "Jonathon", Gender = "female", LastName = "Spice", Name = "Jonathon Spice", Addresses = new Addresses { Ship = new Address { City = "Sheffield", Address1 = "5 Eastgate", PostalCode = "S61rt", Region = "South Yorkshire", Country = "GB" } } } } }), JsonSerializer.Serialize(order.Attendees));
        }

        [Fact]
        public async Task ThrowsIncorrectOrderUri()
        {
            using var http = _mockedClient.CreateClient();
            var client = new ApiClient(http, Configuration);
            await Assert.ThrowsAsync<ArgumentException>(() => client.FetchOrderFromWebhook("TestString"));
        }

        public void Dispose()
        {
            _mockedClient.Object?.Dispose();
        }
    }
}