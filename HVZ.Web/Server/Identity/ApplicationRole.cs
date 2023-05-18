using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace HVZ.Web.Server.Identity
{
    [CollectionName("IdentityRoles")]
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
    }
}
