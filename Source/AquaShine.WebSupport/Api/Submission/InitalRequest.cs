using System;
using System.Text.Json.Serialization;

namespace AquaShine.WebSupport.Api.Submission
{
    public class InitalRequest
    {
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan TimeTaken => (TimeSpan)TimeSpan;

        [JsonIgnore]
        public EditableTimeSpan TimeSpan { get; set; } = new EditableTimeSpan();

        public string? DisplayName { get; set; }

        public bool GenerateDisplayImg { get; set; }

        public bool Public { get; set; } = true;
    }
}