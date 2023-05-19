using HVZ.Persistence;
using HVZ.Persistence.Models;
using Microsoft.AspNetCore.Mvc;

namespace HVZ.Web.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrgController : ControllerBase
    {
        private readonly ILogger<OrgController> _logger;
        private readonly IOrgRepo _orgRepo;
        private readonly HttpContextAccessor _contextAccessor;

        public OrgController(ILogger<OrgController> logger, IOrgRepo orgRepo)
        {
            _logger = logger;
            _orgRepo = orgRepo;
            _contextAccessor = new HttpContextAccessor();
        }

        /// <summary>
        /// Get an organization by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Returns the Organization object</response>
        /// <response code="400">Could not find an org with the specified ID</response>
        [HttpGet("/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Organization>> GetOrg(string id)
        {
            Organization org;
            try
            {
                org = await _orgRepo.GetOrgById(id);
            }
            catch
            {
                return NotFound($"Could not find organization with ID: {id}");
            }

            return Ok(org);
        }
    }
}
