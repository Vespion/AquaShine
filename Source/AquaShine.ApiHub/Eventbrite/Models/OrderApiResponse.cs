using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace AquaShine.ApiHub.Eventbrite.Models
{
    /// <summary>
    /// Part of the response recieved from Eventbrite when querying for an order
    /// </summary>
    public class OrderApiResponse
    {
        //[JsonPropertyName("costs")]
        //public Costs Costs { get; set; }

        //[JsonPropertyName("resource_uri")]
        //public Uri ResourceUri { get; set; }

        /// <summary>
        /// The ID of the order
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        // [JsonPropertyName("changed")]
        // public DateTimeOffset Changed { get; set; }
        //
        // [JsonPropertyName("created")]
        // public DateTimeOffset Created { get; set; }
        //
        // [JsonPropertyName("name")]
        // public string Name { get; set; }
        //
        // [JsonPropertyName("first_name")]
        // public string FirstName { get; set; }
        //
        // [JsonPropertyName("last_name")]
        // public string LastName { get; set; }
        //
        // [JsonPropertyName("email")]
        // public string Email { get; set; }
        //
        // [JsonPropertyName("status")]
        // public string Status { get; set; }
        //
        // [JsonPropertyName("time_remaining")]
        // public object TimeRemaining { get; set; }
        //
        // [JsonPropertyName("event_id")]
        // public string EventId { get; set; }

        /// <summary>
        /// An array of attendees on the order
        /// </summary>
        /// <remarks>This is null if the attendees expansion is not present on the API request query string</remarks>
        [JsonPropertyName("attendees")]
#pragma warning disable CA2227 // Collection properties should be read only. Needs to be settable to be parsed
        public ICollection<Attendee>? Attendees { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only
    }
}