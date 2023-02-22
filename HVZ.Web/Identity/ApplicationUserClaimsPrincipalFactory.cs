namespace HVZ.Web.Identity;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Models;

public class ApplicationClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser> {
    public ApplicationClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> options
    ) : base(userManager, options)
    {

    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);

        // Claims that we will likely need
        identity.AddClaim(new Claim("DatabaseId", user.DatabaseId));
        identity.AddClaim(new Claim("FullName", user.FullName));
        identity.AddClaim(new Claim("DiscordId", user.DiscordId));

        return identity;
    }
}