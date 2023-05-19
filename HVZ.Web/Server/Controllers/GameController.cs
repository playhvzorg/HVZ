using HVZ.Persistence;
using HVZ.Persistence.Models;
using Microsoft.AspNetCore.Mvc;

namespace HVZ.Web.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly IGameRepo _gameRepo;
        private readonly HttpContextAccessor _contextAccessor;

        public GameController(ILogger<GameController> logger, IGameRepo gameRepo)
        {
            _logger = logger;
            _gameRepo = gameRepo;
            _contextAccessor = new HttpContextAccessor();
        }

        /// <summary>
        /// Get the list of players in a game
        /// </summary>
        /// <param name="id">ID for the game</param>
        /// <returns></returns>
        /// <response code="200">Returns a list of players</response>
        /// <response code="404">Could not find a game with the specified ID</response>
        [HttpGet("/{id}/players")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<Player>>> GetPlayers(string id)
        {
            Game game;
            try
            {
                game = await _gameRepo.GetGameById(id);
            }
            catch
            {
                return NotFound($"Could not find game with ID: {id}");
            }

            // TODO remove sensitive info for non mods
            return Ok(game.Players);
        }

        /// <summary>
        /// Get the event log for a game
        /// </summary>
        /// <param name="id">ID for the game</param>
        /// <returns></returns>
        /// <response code="200">Returns a list of event log entries</response>
        /// <response code="404">COuld not find a game with the specified ID</response>
        [HttpGet("/{id}/events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<GameEventLog>>> GetEventLog(string id)
        {
            Game game;
            try
            {
                game = await _gameRepo.GetGameById(id);
            }
            catch
            {
                return NotFound($"Could not find game with ID: {id}");
            }

            // TODO remove sensitive info for non mods
            return Ok(game.EventLog);
        }
    }
}
