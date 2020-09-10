using System;

namespace AquaShine.ApiHub.Eventbrite.Models
{
    /// <summary>
    /// Part of the body received from Eventbrite when a webhook is triggered
    /// </summary>
    public readonly struct WebhookPayload : IEquatable<WebhookPayload>
    {
        internal WebhookPayload(string action, Uri apiUrl)
        {
            Action = action;
            ApiUrl = apiUrl;
        }

        /// <summary>
        /// The action that caused the webhook to fire
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// The URL of the relevant data on Eventbrite
        /// </summary>
        public Uri ApiUrl { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is WebhookPayload payload)) return false;
            return Equals(payload);

        }

        /// <inheritdoc />
        public bool Equals(WebhookPayload other)
        {
            return Action == other.Action && ApiUrl.Equals(other.ApiUrl);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Action, ApiUrl);
        }

#pragma warning disable 1591
        public static bool operator== (WebhookPayload a, WebhookPayload b) => Equals(a, b);

        public static bool operator!= (WebhookPayload a, WebhookPayload b) => !Equals(a, b);
#pragma warning restore 1591
    }
}
