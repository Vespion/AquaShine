using AquaShine.ApiHub.Eventbrite.Models;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace AquaShine.ApiHub.Data.Models
{
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
        /// Flag that indicates this entrant has been deleted.
        /// </summary>
        /// <remarks>Entrant data is preserved in case the deletion was accidental. If so it is easy to reverse by changing this flag</remarks>
        public bool SoftDelete { get; set; }

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
        public virtual Submission? Submission { get; set; }

        /// <summary>
        /// Email address for the entrant
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// The shipping address of the entrant
        /// </summary>
        public virtual Address Address { get; set; } = null!;

        /// <summary>
        /// The biological gender of the entrant
        /// </summary>
        public Gender BioGender { get; set; }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Framework method, parameters will not be null")]
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            Submission = new Submission();

            EventbriteId = properties[nameof(EventbriteId)].StringValue;
            Name = properties[nameof(Name)].StringValue;
            SoftDelete = properties[nameof(SoftDelete)].BooleanValue ?? false;

            Submission.TimeToComplete = properties.ContainsKey(nameof(Submission.TimeToComplete)) ?
                TimeSpan.Parse(properties[nameof(Submission.TimeToComplete)].StringValue, new NumberFormatInfo()) :
                (TimeSpan?)null;

            Submission.Verified = properties.ContainsKey(nameof(Submission.Verified)) && properties[nameof(Submission.Verified)].BooleanValue.GetValueOrDefault(false);
            Submission.Show = properties.ContainsKey(nameof(Submission.Show)) && properties[nameof(Submission.Show)].BooleanValue.GetValueOrDefault(false);
            Submission.Locked = properties.ContainsKey(nameof(Submission.Locked)) && properties[nameof(Submission.Locked)].BooleanValue.GetValueOrDefault(false);

            Submission.DisplayImgUrl = properties.ContainsKey(nameof(Submission.DisplayImgUrl)) ? new Uri(properties[nameof(Submission.DisplayImgUrl)].StringValue) : (Uri?)null;
            Submission.VerifyingImgUrl = properties.ContainsKey(nameof(Submission.VerifyingImgUrl)) ? new Uri(properties[nameof(Submission.VerifyingImgUrl)].StringValue) : (Uri?)null;
            Submission.DisplayName = properties.ContainsKey(nameof(Submission.DisplayName)) ? properties[nameof(Submission.DisplayName)].StringValue : null;

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
                {nameof(Submission.TimeToComplete), EntityProperty.GeneratePropertyForString(Submission?.TimeToComplete?.ToString("G", new NumberFormatInfo()))},
                {nameof(Submission.Verified), EntityProperty.GeneratePropertyForBool(Submission?.Verified)},
                {nameof(Submission.Show), EntityProperty.GeneratePropertyForBool(Submission?.Show)},
                {nameof(Submission.Locked), EntityProperty.GeneratePropertyForBool(Submission?.Locked)},
                {nameof(Submission.DisplayImgUrl), EntityProperty.GeneratePropertyForString(Submission?.DisplayImgUrl?.ToString())},
                {nameof(Submission.VerifyingImgUrl), EntityProperty.GeneratePropertyForString(Submission?.VerifyingImgUrl?.ToString())},
                {nameof(Submission.DisplayName), EntityProperty.GeneratePropertyForString(Submission?.DisplayName)},
                {nameof(Email), EntityProperty.GeneratePropertyForString(Email)},
                {nameof(Address), EntityProperty.GeneratePropertyForString(JsonSerializer.Serialize(Address))},
                {nameof(BioGender), EntityProperty.GeneratePropertyForString(BioGender.ToString("G"))},
                {nameof(SoftDelete), EntityProperty.GeneratePropertyForBool(SoftDelete)}
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
}