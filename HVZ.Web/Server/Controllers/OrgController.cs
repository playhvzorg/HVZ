using HVZ.Persistence;
using HVZ.Persistence.Models;
using HVZ.Web.Server.Identity;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HVZ.Web.Server.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class OrgController : ControllerBase
    {
        private readonly ILogger<OrgController> _logger;
        private readonly IOrgRepo _orgRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrgController(ILogger<OrgController> logger, IOrgRepo orgRepo, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _orgRepo = orgRepo;
            _userManager = userManager;
        }

        [HttpGet("{url}/id")]
        public async Task<ActionResult<string>> GetOrgIdFromUrl(string url)
        {
            Organization? org = await _orgRepo.FindOrgByUrl(url);
            if (org == null)
                return NotFound($"Could not find Organization with URL: {url}");

            return Ok(org.Id);
        }

        [HttpGet("{id}/info")]
        public async Task<ActionResult> GetOrgInfo(string id)
        {
            Organization? org = await _orgRepo.FindOrgById(id);
            if (org == null)
                return OrgNotFound(id);

            return Ok(OrgInfo.New(org));
        }

        [HttpGet("{id}/config")]
        public async Task<ActionResult<OrgConfig>> GetOrgConfig(string id)
        {
            Organization? org = await _orgRepo.FindOrgById(id);
            if (org == null)
                return OrgNotFound(id);

            if (!await UserIsAdmin(id, GetDatabaseId(User)))
                return Unauthorized();

            return Ok(new OrgConfig(org));
        }

        [HttpPost("{id}/settings/update")]
        public async Task<ActionResult<PostResult>> UpdateOrgSettings(string id, [FromBody] OrgSettingsUpdateRequest settings)
        {
            Organization? org = await _orgRepo.FindOrgById(id);
            if (org == null)
                return OrgNotFoundPostResult(id);

            if (!await UserIsAdmin(id, GetDatabaseId(User)))
                return Unauthorized(new PostResult
                {
                    Succeeded = false,
                    Error = $"You do not have permission to update settings for {org.Name}"
                });

            if (settings.Description is not null)
                await _orgRepo.SetOrgDescription(id, settings.Description);

            if (settings.RequireVerifiedEmail is not null)
                await _orgRepo.SetRequireVerifiedEmail(id, (bool)settings.RequireVerifiedEmail);

            if (settings.RequireProfilePicture is not null)
                await _orgRepo.SetRequireProfilePicture(id, (bool)settings.RequireProfilePicture);

            return Ok(new PostResult { Succeeded = true });
        }

        [HttpPost("{id}/game/create")]
        public async Task<ActionResult<CreateGameResult>> CreateNewGame(string id, [FromBody] CreateGameRequest create)
        {
            Organization? org = await _orgRepo.FindOrgById(id);
            if (org is null)
                return new CreateGameResult
                {
                    Succeeded = false,
                    Error = $"Could not find Organization with ID {id}"
                };

            string userId = GetDatabaseId(User);

            if (!await UserIsAdmin(id, GetDatabaseId(User)))
                return Unauthorized(new CreateGameResult
                {
                    Succeeded = false,
                    Error = $"You do not have permission to create a new game for {org.Name}"
                });

            var game = await _orgRepo.CreateGame(create.Name, id, userId, create.OzTags);

            return Ok(new CreateGameResult
            {
                Succeeded = true,
                GameId = game.Id
            });
        }

        [HttpPost("create")]
        public async Task<ActionResult<CreateOrgResult>> CreateNewOrg([FromBody] CreateOrgRequest create)
        {
            // TODO: User must have a verified email before creating an org
            if (!await UserEmailConfirmed(User))
                return Unauthorized(new CreateOrgResult
                {
                    Succeeded = false,
                    Error = "You must confirm your email before you can create a new Organization"
                });

            Organization? org = await _orgRepo.FindOrgByUrl(create.Url);
            if (org is not null)
                return BadRequest(new CreateOrgResult
                {
                    Succeeded = false,
                    Error = $"Org with URL {create.Url} already exists"
                });

            Organization createdOrg = await _orgRepo.CreateOrg(create.Name, create.Url, GetDatabaseId(User));

            return Ok(new CreateOrgResult
            {
                Succeeded = true,
                OrgId = createdOrg.Id,
            });
        }

        [HttpGet("{id}/authorization")]
        public async Task<ActionResult<OrgAuthorization>> GetUserAuthorization(string id)
        {
            Organization? org = await _orgRepo.FindOrgById(id);
            if (org == null)
                return OrgNotFound(id);

            return Ok(new OrgAuthorization
            {
                IsModerator = org.Moderators.Contains(GetDatabaseId(User)),
                IsAdmin = org.Administrators.Contains(GetDatabaseId(User))
            });
        }

        private ActionResult OrgNotFound(string id)
            => NotFound($"Could not find Organization with ID {id}");

        private ActionResult<PostResult> OrgNotFoundPostResult(string id)
            => NotFound(new PostResult
            {
                Succeeded = false,
                Error = $"Could not find Organization with ID {id}"
            });

        private async Task<bool> UserIsAdmin(string orgId, string userId)
            => await _orgRepo.IsAdminOfOrg(orgId, userId);

        private string GetDatabaseId(ClaimsPrincipal user)
            => user.Claims.FirstOrDefault(
                c => c.Type == "DatabaseId"
            )?.Value ?? string.Empty;

        private async Task<bool> UserEmailConfirmed(ClaimsPrincipal user)
            => (await _userManager.GetUserAsync(user))?.EmailConfirmed ?? false;

    }
}
