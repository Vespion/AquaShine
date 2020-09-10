using System.Text.Json.Serialization;
#pragma warning disable 1591

namespace AquaShine.ApiHub.Eventbrite.Models
{
    /// <summary>
    /// A profile on an attendee
    /// </summary>
#pragma warning disable 1724
    public class AttendeeProfile
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("addresses")]
        public Addresses? Addresses { get; set; }
        
        [JsonPropertyName("gender")]
        public string? Gender { get; set; }
    }
}