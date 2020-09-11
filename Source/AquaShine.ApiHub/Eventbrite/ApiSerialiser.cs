using System;
using System.Globalization;
using System.Text.Json;
using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite.Models;
using SnowMaker;

namespace AquaShine.ApiHub.Eventbrite
{
    /// <summary>
    /// Wrapper around the JSON serialiser to provide more performent operations
    /// </summary>
    public class ApiSerialiser
    {
        private readonly IUniqueIdGenerator _idGenerator;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="idGenerator">The generator to use for entrant IDs</param>
        public ApiSerialiser(IUniqueIdGenerator idGenerator)
        {
            _idGenerator = idGenerator;
        }

        /// <summary>
        /// Deserialise the body of a webhook
        /// </summary>
        /// <param name="webhook">A string containing the webhook JSON</param>
        /// <returns></returns>
#pragma warning disable CA1822 // Mark members as static
        public WebhookPayload DeserialiseWebhookPayload(string webhook)
#pragma warning restore CA1822 // Mark members as static
        {
            using var doc = JsonDocument.Parse(webhook);
            using var objectEnumerator = doc.RootElement.EnumerateObject();
            Uri? apiUrl = null;
            string? action = null;
            foreach (var property in objectEnumerator)
            {
                if (property.Name == ("api_url"))
                {
                    apiUrl = new Uri(property.Value.GetString());
                }
                else if (property.NameEquals("config"))
                {
                    using var configObject = property.Value.EnumerateObject();
                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var configProperty in configObject)
                    {
                        // ReSharper disable once InvertIf
                        if (configProperty.NameEquals("action"))
                        {
                            action = configProperty.Value.GetString();
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(apiUrl?.OriginalString) && !string.IsNullOrWhiteSpace(action))
                {
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(apiUrl?.OriginalString) && !string.IsNullOrWhiteSpace(action))
            {
                return new WebhookPayload(action, apiUrl);
            }
            else
            {
                throw new JsonException($"{StatusCodes.MissingJsonProperty} {StatusCodes.MissingJsonProperty.Message}");
            }
        }

        /// <summary>
        /// Converts an attendee into an entrant. This method generates a unique ID for the entrant and should only be used once per attendee
        /// </summary>
        /// <param name="attendee"></param>
        /// <returns></returns>
        public Entrant ConvertEntrant(Attendee attendee)
        {
            if (attendee == null) throw new ArgumentNullException(nameof(attendee));
            var entrant = new Entrant
            {
                RowKey = _idGenerator.NextId("entrantIds").ToString("X", new NumberFormatInfo()),
                EventbriteId = attendee.Id,
                Address = attendee.Profile!.Addresses!.Ship!,
                BioGender = Enum.Parse<Gender>(attendee.Profile.Gender, true),
                Email = attendee.Profile.Email!,
                Name = attendee.Profile.Name!,
            };
            return entrant;
        }
    }
}
