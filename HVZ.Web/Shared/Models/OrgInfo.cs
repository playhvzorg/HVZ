using HVZ.Persistence.Models;
using NodaTime;

namespace HVZ.Web.Shared.Models
{
    public class OrgInfo
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Url { get; set; }
        public required string Description { get; set; }
        public string? ActiveGameId { get; set; }
        public required Instant CreatedAt { get; set; }
        public string DefaultAvatar => $"https://ui-avatars.com/api/?name={Name.Replace(" ", "+")}";
        public bool RequirePlayerEmailConfirmed { get; set; }
        public bool RequirePlayerProfilePicture { get; set; }

        public static OrgInfo New(Organization org)
            => new OrgInfo
            {
                Id = org.Id,
                Name = org.Name,
                Url = org.Url,
                Description = org.Description,
                CreatedAt = org.CreatedAt,
                ActiveGameId = org.ActiveGameId,
                RequirePlayerEmailConfirmed = org.RequireVerifiedEmailForPlayer,
                RequirePlayerProfilePicture = org.RequireProfilePictureForPlayer
            };

        //public OrgInfo(Organization org)
        //{
        //    Id = org.Id;
        //    Name = org.Name;
        //    Url = org.Url;
        //    Description = org.Description;
        //    CreatedAt = org.CreatedAt;
        //}
    }
}
