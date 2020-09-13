using System;

namespace AquaShine.WebSupport
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
        /// Use a display name instead of the entrants name
        /// </summary>
        public string? DisplayName { get; set; }
    }
}