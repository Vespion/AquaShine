namespace AquaShine.WebSupport.Api.DataTable
{
    public class Request
    {
        public string? NameFilter { get; set; }
        public bool? VerificationFilter { get; set; }
        public int Count { get; set; }
        public int Page { get; set; }
    }
}