using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace HVZ.Web.Server.Identity
{
    [CollectionName("IdentityUsers")]
    public class ApplicationUser : MongoIdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public string DatabaseId { get; set; } = string.Empty;
        public string DiscordId { get; set; } = string.Empty;
    }
}
