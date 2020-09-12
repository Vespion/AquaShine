using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AquaShine.ApiHub.Eventbrite.Models;
using Microsoft.Extensions.Configuration;

namespace AquaShine.ApiHub.Eventbrite
{
    /// <summary>
    /// A client for communicating with the Eventbrite API
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="client">HTTP Client</param>
        /// <param name="configurationRoot">Configuration to retrieve Eventbrite token from</param>
        public ApiClient(HttpClient client, IConfiguration configurationRoot)
        {
            if (configurationRoot == null) throw new ArgumentNullException(nameof(configurationRoot));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configurationRoot["EventbriteToken"]);
        }

        private static readonly Regex OrderCheckRegex = new Regex(@"https:\/\/www\.eventbriteapi\.com\/v3\/orders\/[0-9]*?\/", RegexOptions.Compiled);

        internal static Uri ConstructOrderUrl(string apiUrl)
        {
            if (OrderCheckRegex.IsMatch(apiUrl))
            {
                return new Uri($"{apiUrl}?expand=attendees");
            }
            throw new ArgumentException($"{StatusCodes.InvalidArg} {StatusCodes.InvalidArg.Message}");
        }

        /// <summary>
        /// Fetches an order using the Url in a <see cref="WebhookPayload"/>
        /// </summary>
        /// <param name="payloadApiUrl">The URL in a webhook body</param>
        /// <returns>A parsed API response</returns>
        /// <exception cref="ArgumentException">Thrown if the supplied Url does not match the expectation for a webhook url</exception>
        public async Task<OrderApiResponse> FetchOrderFromWebhook(string payloadApiUrl)
        {
            var targetUri = ConstructOrderUrl(payloadApiUrl);
            var response = await _client.GetAsync(targetUri).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            var order = await JsonSerializer.DeserializeAsync<OrderApiResponse>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
            return order;
        }

        /// <inheritdoc cref="FetchOrderFromWebhook(string)" />
        public Task<OrderApiResponse> FetchOrderFromWebhook(Uri payloadApiUrl)
        {
            if (payloadApiUrl == null) throw new ArgumentNullException(nameof(payloadApiUrl));
            return FetchOrderFromWebhook(payloadApiUrl.ToString());
        }
    }
}
