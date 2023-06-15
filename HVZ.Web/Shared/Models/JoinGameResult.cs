namespace HVZ.Web.Shared.Models
{
    public class JoinGameResult
    {
        public bool Succeeded { get; set; }
        public IEnumerable<string>? Errors { get; set; }
        public PlayerData? CreatedPlayer { get; set; }
    }
}
