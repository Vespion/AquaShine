using AquaShine.ApiHub.Eventbrite;
using AquaShine.ApiHub.Eventbrite.Models;
using SnowMaker;
using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace AquaShine.ApiHub.Tests.Eventbrite
{
    public class ApiSerialiserTests
    {
        public const string CorruptJson = @"{""api_url"":""https://www.eventbriteapi.com/v3/orders/1428494963/"",""config"":{""user_RowKey"":""466077728041"",""webhoo}}";
        public const string WebhookJson1 = @"{""api_url"":""https://www.eventbriteapi.com/v3/orders/1428494963/"",""config"":{""user_RowKey"":""466077728041"",""webhook_RowKey"":""2379016"",""action"":""order.placed"",""endpoint_url"":""https://7100ef845a6cc805433b49b5fcfd807a.m.pipedream.net""}}";
        public const string InvalRowKeyJson = @"{""api_url"":""https://www.eventbriteapi.com/v3/orders/1428494963/""}";

        private readonly IUniqueIdGenerator _RowKeyGenerator;

        public ApiSerialiserTests()
        {
            if (File.Exists("./entrantRowKeys.txt")) File.Delete("./entrantRowKeys.txt");
            _RowKeyGenerator = new UniqueIdGenerator(new DebugOnlyFileDataStore("./"))
            {
                BatchSize = 5,
                MaxWriteAttempts = 1
            };
        }

        [Fact]
        public void DecodesWebhookPayload()
        {
            var serialiser = new ApiSerialiser(_RowKeyGenerator);

            var webhookBody1 = serialiser.DeserialiseWebhookPayload(WebhookJson1);

            Assert.Equal(new Uri(@"https://www.eventbriteapi.com/v3/orders/1428494963/"), webhookBody1.ApiUrl);
            Assert.Equal(@"order.placed", webhookBody1.Action);
        }

        [Fact]
        public void ThrowsExceptionWhenCorrupt()
        {
            var serialiser = new ApiSerialiser(_RowKeyGenerator);

            Assert.ThrowsAny<JsonException>(() => serialiser.DeserialiseWebhookPayload(CorruptJson));
        }

        [Fact]
        public void ThrowsExceptionWhenMissingProperties()
        {
            var serialiser = new ApiSerialiser(_RowKeyGenerator);

            var exception = Assert.Throws<JsonException>(() => serialiser.DeserialiseWebhookPayload(InvalRowKeyJson));
            Assert.StartsWith(StatusCodes.MissingJsonProperty.ToString(), exception.Message);
        }

        [Fact]
        public void ThrowsExceptionWhenNull()
        {
            var serialiser = new ApiSerialiser(_RowKeyGenerator);

            var exception = Assert.Throws<ArgumentNullException>(() => serialiser.ConvertEntrant(null));
        }

        [Fact]
        public void ConvertsAndMaintainsProperties()
        {
            var serialiser = new ApiSerialiser(_RowKeyGenerator);
            var attendee = new Attendee
            {
                Id = "1428494963",
                Profile = new AttendeeProfile
                {
                    Gender = "female",
                    Name = "Jonathon Spice",
                    Addresses = new Addresses
                    {
                        Ship = new Address
                        {
                            City = "Sheffield",
                            Region = "South Yorkshire",
                            PostalCode = "S61rt",
                            Address1 = "5 Eastgate",
                            Country = "GB"
                        }
                    },
                    Email = "jonathon.spice@gmail.com",
                    FirstName = "Jonathon",
                    LastName = "Spice"
                }
            };

            var entrant = serialiser.ConvertEntrant(attendee);

            Assert.Equal(attendee.Id, entrant.EventbriteId);
            Assert.Equal(attendee.Profile.Name, entrant.Name);
            Assert.Equal(attendee.Profile.Email, entrant.Email);
            Assert.Equal(attendee.Profile.Gender, entrant.BioGender.ToString().ToLower());
            Assert.Equal(JsonSerializer.Serialize(attendee.Profile.Addresses.Ship), JsonSerializer.Serialize(entrant.Address));
        }

        [Fact]
        public void EntrantRowKeysDoNotRepeat()
        {
            var serialiser = new ApiSerialiser(_RowKeyGenerator);
            var attendee = new Attendee
            {
                Id = "1428494963",
                Profile = new AttendeeProfile
                {
                    Gender = "female",
                    Name = "Jonathon Spice",
                    Addresses = new Addresses
                    {
                        Ship = new Address
                        {
                            City = "Sheffield",
                            Region = "South Yorkshire",
                            PostalCode = "S61rt",
                            Address1 = "5 Eastgate",
                            Country = "GB"
                        }
                    },
                    Email = "jonathon.spice@gmail.com",
                    FirstName = "Jonathon",
                    LastName = "Spice"
                }
            };

            var entrant = serialiser.ConvertEntrant(attendee);
            var entrant2 = serialiser.ConvertEntrant(attendee);
            var entrant3 = serialiser.ConvertEntrant(attendee);
            var entrant4 = serialiser.ConvertEntrant(attendee);
            var entrant5 = serialiser.ConvertEntrant(attendee);
            var entrant6 = serialiser.ConvertEntrant(attendee);
            var entrant7 = serialiser.ConvertEntrant(attendee);
            var entrant8 = serialiser.ConvertEntrant(attendee);
            var entrant9 = serialiser.ConvertEntrant(attendee);
            var entrant10 = serialiser.ConvertEntrant(attendee);

            Assert.NotEqual(entrant.RowKey, entrant2.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant3.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant4.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant5.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant6.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant7.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant8.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant9.RowKey);
            Assert.NotEqual(entrant.RowKey, entrant10.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant3.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant4.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant5.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant6.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant7.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant8.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant9.RowKey);
            Assert.NotEqual(entrant2.RowKey, entrant10.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant2.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant3.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant4.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant7.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant6.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant7.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant8.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant9.RowKey);
            Assert.NotEqual(entrant5.RowKey, entrant10.RowKey);
        }
    }
}