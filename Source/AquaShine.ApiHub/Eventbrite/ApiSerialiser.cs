using System;
using System.Text.Json;
using AquaShine.ApiHub.Eventbrite.Models;

namespace AquaShine.ApiHub.Eventbrite
{
    /// <summary>
    /// Wrapper around the JSON serialiser to provide more performant operations
    /// </summary>
    public class ApiSerialiser
    {
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
    }
}
