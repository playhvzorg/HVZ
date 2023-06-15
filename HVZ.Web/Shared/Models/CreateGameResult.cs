namespace HVZ.Web.Shared.Models
{
    public class CreateGameResult
    {
        public bool Succeeded { get; set; }
        public string? GameId { get; set; }
        public string? Error { get; set; }
    }
}
