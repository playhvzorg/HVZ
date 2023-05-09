using System.Security.Claims;
using HVZ.Persistence;
using HVZ.Persistence.Models;
using HVZ.Web.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HVZ.Web.Data;

[Route("api/v1/[action]")]
public class RestApiController : ControllerBase
{
    private IMongoDatabase _mongoDatabase;
    private IOrgRepo _orgRepo;
    private UserManager<ApplicationUser> _userManager;

    public RestApiController(IMongoDatabase mongoDatabase, IOrgRepo orgRepo, UserManager<ApplicationUser> userManager)
    {
        _mongoDatabase = mongoDatabase;
        _orgRepo = orgRepo;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult health()
    {
        return _mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000)
            ? new JsonResult("Healthy")
            : new JsonResult("Unable to reach database") { StatusCode = 500 };
    }

    [ApiKey]
    [HttpPost]
    public async Task<IActionResult> DiscordServerLink()
    {
        try
        {
            var json = JObject.Parse(await new StreamReader(Request.Body).ReadToEndAsync());
            string discordUserId = json["discordUserId"]?.Value<string>()!;
            string discordServerId = json["discordServerId"]?.Value<string>()!;
            string orgName = json["orgName"]?.Value<string>()!;

            var user = (await _userManager.GetUsersForClaimAsync(new Claim("discordId", discordUserId)))
                .FirstOrDefault();

            if (user is null)
                return BadRequest($"User not found with discordid {discordUserId}");

            try
            {
                Organization org = await _orgRepo.GetOrgByName(orgName);
                await _orgRepo.SetOrgDiscordServerId(org.Id, discordServerId);
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }
        catch (NullReferenceException)
        {
            return BadRequest();
        }
        catch (JsonReaderException)
        {
            return BadRequest();
        }
    }
}