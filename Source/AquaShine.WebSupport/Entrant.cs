namespace AquaShine.WebSupport
{
    public class Entrant
    {
        public int Id { get; set; }

        /// <summary>
        /// Name of the entrant
        /// </summary>
        public string? Name { get; set; } = null!;

        /// <summary>
        /// The submission for the user
        /// </summary>
        public Submission? Submission { get; set; }

        /// <summary>
        /// The biological gender of the entrant
        /// </summary>
        public Gender BioGender { get; set; }
    }
}