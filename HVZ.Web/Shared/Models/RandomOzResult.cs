namespace HVZ.Web.Shared.Models
{
    public class RandomOzResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }
        public IEnumerable<UserData>? RandomOzs { get; set; }
    }
}
