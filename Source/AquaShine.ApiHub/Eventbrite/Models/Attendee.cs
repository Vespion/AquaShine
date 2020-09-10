using System.Text.Json.Serialization;

namespace AquaShine.ApiHub.Eventbrite.Models
{
    /// <summary>
    /// Someone who is attending an event
    /// </summary>
    public class Attendee
    {
        /// <summary>
        /// Eventbrite ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        //[JsonPropertyName("changed")]
        //public DateTimeOffset Changed { get; set; }

        //[JsonPropertyName("created")]
        //public DateTimeOffset Created { get; set; }

        /// <summary>
        /// General information about the attendant
        /// </summary>
        [JsonPropertyName("profile")]
        public AttendeeProfile? Profile { get; set; }

        //[JsonPropertyName("answers")]
        //public object[] Answers { get; set; }

        //[JsonPropertyName("cancelled")]
        //public bool Cancelled { get; set; }

        //[JsonPropertyName("refunded")]
        //public bool Refunded { get; set; }

        //[JsonPropertyName("status")]
        //public string Status { get; set; }

        //[JsonPropertyName("order_id")]
        //public string OrderId { get; set; }
    }
}