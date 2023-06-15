namespace HVZ.Web.Shared.Models
{
    public class CreateGameRequest
    {
        public required string Name { get; set; }
        public int OzTags { get; set; } = 3;
    }
}
