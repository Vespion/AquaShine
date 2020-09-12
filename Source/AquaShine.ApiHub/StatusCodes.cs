using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("AquaShine.ApiHub.Tests")]
namespace AquaShine.ApiHub
{
    /// <summary>
    /// Standardized <see cref="EventId"/>'s
    /// </summary>
    public static class StatusCodes
    {
        /// <summary>
        /// A property required for deserialization was missing from the JSON object.
        /// </summary>
        public static readonly EventId MissingJsonProperty = new EventId(1, "A property required for deserialization was missing from the JSON object.");
        /// <summary>
        /// Invalid argument supplied.
        /// </summary>
        public static readonly EventId InvalidArg = new EventId(2, "Invalid argument supplied.");
    }

    /// <summary>
    /// Allows for quick reference to a standardised event occurring
    /// </summary>
    public readonly struct EventId : IEquatable<EventId>
    {
        internal EventId(int number, string message)
        {
            Number = number;
            Message = message;
        }

        /// <summary>
        /// Numeric ID for the event
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// A message describing this event
        /// </summary>
        public string Message { get; }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{Number:X}]";
        }

        /// <inheritdoc />
        public bool Equals(EventId other)
        {
            return Number == other.Number && Message == other.Message;
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Number, Message);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is EventId eventId) return Equals(eventId);
            return false;
        }

#pragma warning disable 1591
        public static bool operator ==(EventId left, EventId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventId left, EventId right)
        {
            return !(left == right);
        }
#pragma warning restore 1591
    }
}
