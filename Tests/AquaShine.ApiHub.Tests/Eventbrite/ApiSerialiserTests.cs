using System;
using System.Text.Json;
using AquaShine.ApiHub.Eventbrite;
using Xunit;

namespace AquaShine.ApiHub.Tests.Eventbrite
{
    public class ApiSerialiserTests
    {
        public const string CorruptJson = @"{""api_url"":""https://www.eventbriteapi.com/v3/orders/1428494963/"",""config"":{""user_id"":""466077728041"",""webhoo}}";
        public const string WebhookJson1 = @"{""api_url"":""https://www.eventbriteapi.com/v3/orders/1428494963/"",""config"":{""user_id"":""466077728041"",""webhook_id"":""2379016"",""action"":""order.placed"",""endpoint_url"":""https://7100ef845a6cc805433b49b5fcfd807a.m.pipedream.net""}}";
        public const string InvalidJson = @"{""api_url"":""https://www.eventbriteapi.com/v3/orders/1428494963/""}";

        [Fact]
        public void DecodesWebhookPayload()
        {
            var serialiser = new ApiSerialiser();

            var webhookBody1 = serialiser.DeserialiseWebhookPayload(WebhookJson1);

            Assert.Equal(new Uri(@"https://www.eventbriteapi.com/v3/orders/1428494963/"), webhookBody1.ApiUrl);
            Assert.Equal(@"order.placed", webhookBody1.Action);
        }

        [Fact]
        public void ThrowsExceptionWhenCorrupt()
        {
            var serialiser = new ApiSerialiser();

            Assert.ThrowsAny<JsonException>(() => serialiser.DeserialiseWebhookPayload(CorruptJson));
        }

        [Fact]
        public void ThrowsExceptionWhenMissingProperties()
        {
            var serialiser = new ApiSerialiser();

            var exception = Assert.Throws<JsonException>(() => serialiser.DeserialiseWebhookPayload(InvalidJson));
            Assert.StartsWith(StatusCodes.MissingJsonProperty.ToString(), exception.Message);
        }
    }
}
