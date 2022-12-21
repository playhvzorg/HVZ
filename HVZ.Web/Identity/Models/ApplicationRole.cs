
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using System;

namespace HVZ.Web.Identity.Models
{
    /// <summary>
    /// Class used to store Identity roles
    /// Required by Identity
    /// </summary>
    [CollectionName("Roles")]
    public class ApplicationRole : MongoIdentityRole<Guid>
    {

    }
}