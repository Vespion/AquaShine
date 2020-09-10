using System.Text.Json.Serialization;

namespace AquaShine.ApiHub.Eventbrite.Models
{
    /// <summary>
    /// A set of addresses with meaning
    /// </summary>
    public class Addresses
    {
        /// <summary>
        /// Shipping address
        /// </summary>
        [JsonPropertyName("ship")]
        public Address? Ship { get; set; }
    }
}