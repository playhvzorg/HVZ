namespace HVZ.Web.Identity.Models;

using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

/// <summary>
///     Class used to store Identity roles
///     Required by Identity
/// </summary>
[CollectionName("IdentityRoles")]
public class ApplicationRole : MongoIdentityRole<Guid> {
}