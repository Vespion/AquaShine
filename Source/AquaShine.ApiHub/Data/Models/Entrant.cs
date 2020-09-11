using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using AquaShine.ApiHub.Eventbrite.Models;
using Microsoft.Azure.Cosmos.Table;

namespace AquaShine.ApiHub.Data.Models
{
    /// <summary>
    /// An entrants submission into the virtual run
    /// </summary>
    public class Submission
    {
        /// <summary>
        /// The read only URL to the image used for verification purposes
        /// </summary>
        public Uri? VerifyingImgUrl { get; set; }

        /// <summary>
        /// The read only URL to the image used for display
        /// </summary>
        public Uri? DisplayImgUrl { get; set; }
        
        /// <summary>
        /// The time it took for a entrant to complete the challenge
        /// </summary>
        public TimeSpan? TimeToComplete { get; set; }
        
        /// <summary>
        /// Has the submission been verified by an admin
        /// </summary>
        public bool Verified { get; set; }
        
        /// <summary>
        /// The submission is complete and can no longer be modified
        /// </summary>
        public bool Locked { get; set; }
        
        /// <summary>
        /// Is the submission publicly shown?
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Use a display name instead of the entrants name
        /// </summary>
        public string? DisplayName { get; set; }
    }
    
    /// <summary>
    /// An entrant in the virtual run
    /// </summary>
    public class Entrant : ITableEntity
    {
        ///// <summary>
        ///// Id used for searching
        ///// </summary>
        //public long Id { get => long.Parse(RowKey, new NumberFormatInfo()); set => RowKey = value.ToString("X", new NumberFormatInfo()); }
        
        
        /// <summary>
        /// Id of the order. If order is refunded this user is deleted
        /// </summary>
        public string EventbriteId { get; set; } = null!;

        /// <summary>
        /// Name of the entrant
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The submission for the user
        /// </summary>
        public Submission? Details { get; set; }

        /// <summary>
        /// Email address for the entrant
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// The shipping address of the entrant
        /// </summary>
        public Address Address { get; set; } = null!;

        /// <summary>
        /// The biological gender of the entrant
        /// </summary>
        public Gender BioGender { get; set; }


        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Framework method, parameters will not be null")]
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            Details = new Submission();

            EventbriteId = properties[nameof(EventbriteId)].StringValue;
            Name = properties[nameof(Name)].StringValue;

            Details.TimeToComplete = properties.ContainsKey(nameof(Details.TimeToComplete)) ? 
                TimeSpan.Parse(properties[nameof(Details.TimeToComplete)].StringValue, new NumberFormatInfo()) : 
                (TimeSpan?)null;

            Details.Verified = properties.ContainsKey(nameof(Details.Verified)) && properties[nameof(Details.Verified)].BooleanValue.GetValueOrDefault(false);
            Details.Show = properties.ContainsKey(nameof(Details.Show)) && properties[nameof(Details.Show)].BooleanValue.GetValueOrDefault(false);
            Details.Locked = properties.ContainsKey(nameof(Details.Locked)) && properties[nameof(Details.Locked)].BooleanValue.GetValueOrDefault(false);

            Details.DisplayImgUrl = properties.ContainsKey(nameof(Details.DisplayImgUrl)) ? new Uri(properties[nameof(Details.DisplayImgUrl)].StringValue) : (Uri?) null;
            Details.VerifyingImgUrl = properties.ContainsKey(nameof(Details.VerifyingImgUrl)) ? new Uri(properties[nameof(Details.VerifyingImgUrl)].StringValue) : (Uri?) null;
            Details.DisplayName = properties.ContainsKey(nameof(Details.DisplayName)) ? properties[nameof(Details.DisplayName)].StringValue : null;
            
            Email = properties[nameof(Email)].StringValue;
            Address = JsonSerializer.Deserialize<Address>(properties[nameof(Address)].StringValue);
            BioGender = Enum.Parse<Gender>(properties[nameof(BioGender)].StringValue);
        }

        /// <inheritdoc />
        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return new Dictionary<string, EntityProperty>
            {
                {nameof(EventbriteId), EntityProperty.GeneratePropertyForString(EventbriteId)},
                {nameof(Name), EntityProperty.GeneratePropertyForString(Name)},
                {nameof(Details.TimeToComplete), EntityProperty.GeneratePropertyForString(Details?.TimeToComplete?.ToString("G", new NumberFormatInfo()))},
                {nameof(Details.Verified), EntityProperty.GeneratePropertyForBool(Details?.Verified)},
                {nameof(Details.Show), EntityProperty.GeneratePropertyForBool(Details?.Show)},
                {nameof(Details.Locked), EntityProperty.GeneratePropertyForBool(Details?.Locked)},
                {nameof(Details.DisplayImgUrl), EntityProperty.GeneratePropertyForString(Details?.DisplayImgUrl?.ToString())},
                {nameof(Details.VerifyingImgUrl), EntityProperty.GeneratePropertyForString(Details?.VerifyingImgUrl?.ToString())},
                {nameof(Details.DisplayName), EntityProperty.GeneratePropertyForString(Details?.DisplayName)},
                {nameof(Email), EntityProperty.GeneratePropertyForString(Email)},
                {nameof(Address), EntityProperty.GeneratePropertyForString(JsonSerializer.Serialize(Address))},
                {nameof(BioGender), EntityProperty.GeneratePropertyForString(BioGender.ToString("G"))}
            };
        }

        /// <inheritdoc />
        [System.Diagnostics.DebuggerNonUserCode]
        public string PartitionKey { get; set; } = null!;

        /// <inheritdoc />
        public string RowKey { get; set; } = null!;

        /// <inheritdoc />
        public DateTimeOffset Timestamp { get; set; }

        /// <inheritdoc />
        public string? ETag { get; set; }
    }

    /// <summary>
    /// Accepted gender values
    /// </summary>
    public enum Gender
    {
        /// <summary>
        /// Other
        /// </summary>
        Other,
        /// <summary>
        /// Male
        /// </summary>
        Male,
        /// <summary>
        /// Female
        /// </summary>
        Female
    }
}