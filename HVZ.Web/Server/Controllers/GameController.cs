using HVZ.Persistence;
using HVZ.Persistence.Models;
using HVZ.Web.Server.Identity;
using HVZ.Web.Server.Services;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HVZ.Web.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly IGameRepo _gameRepo;
        private readonly IOrgRepo _orgRepo;
        private readonly IUserRepo _userRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ImageService _imageService;

        public GameController(ILogger<GameController> logger, IGameRepo gameRepo, IOrgRepo orgRepo, IUserRepo userRepo, UserManager<ApplicationUser> userManager, ImageService imageService)
        {
            _logger = logger;
            _gameRepo = gameRepo;
            _orgRepo = orgRepo;
            _userRepo = userRepo;
            _userManager = userManager;
            _imageService = imageService;
        }

        /// <summary>
        /// Get the list of players in a game
        /// </summary>
        /// <param name="id">ID for the game</param>
        /// <returns></returns>
        /// <response code="200">Returns a list of players</response>
        /// <response code="404">Could not find a game with the specified ID</response>
        [HttpGet("{id}/players")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PlayerData>>> GetPlayers(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null)
                return NotFound($"Could not find game with ID: {id}");

            bool userIsMod = false;
            bool userIsOz = false;
            string userId = GetDatabaseId(User);

            userIsMod = await UserIsModerator(game.OrgId, userId);
            Console.WriteLine(userIsMod);
            Player? player = await _gameRepo.FindPlayerByUserId(id, userId);
            if (player is not null)
            {
                userIsOz = player.Role == Player.gameRole.Oz;
            }

            IEnumerable<PlayerData> playerData = await Task.Run(async ()
                => await FormatPlayerList(game.Players));

            if (userIsMod || userIsOz)
                return Ok(playerData);

            playerData = await Task.Run(() =>
            {
                foreach (var player in playerData)
                {
                    if (player.Role == Player.gameRole.Oz)
                    {
                        player.Role = Player.gameRole.Human;
                        player.Tags = 0;
                    }

                    player.GameId = null!;
                }

                return playerData;
            });

            return Ok(playerData);
        }

        private async Task<IEnumerable<PlayerData>> FormatPlayerList(IEnumerable<Player> players)
        {
            List<PlayerData> playerList = new List<PlayerData>();

            foreach (var player in players)
            {
                var playerUser = await _userRepo.GetUserById(player.UserId);
                playerList.Add(new PlayerData
                {
                    User = new UserData
                    {
                        FullName = playerUser.FullName,
                        Email = playerUser.Email,
                        UserId = player.UserId,
                    },
                    GameId = player.GameId,
                    Tags = player.Tags,
                    Role = player.Role,
                });
            }

            return playerList;
        }

        /// <summary>
        /// Get the event log for a game
        /// </summary>
        /// <remarks>
        /// Strips OZ related events for all non moderator/oz users
        /// </remarks>
        /// <param name="id">ID for the game</param>
        /// <returns></returns>
        /// <response code="200">Returns a list of event log entries</response>
        /// <response code="404">Could not find a game with the specified ID</response>
        [HttpGet("{id}/events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<GameLogData>>> GetEventLog(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null)
                return NotFound($"Could not find Game with ID: {id}");

            bool userIsMod = false;
            bool userIsOz = false;
            string userId = GetDatabaseId(User);

            userIsMod = await UserIsModerator(game.OrgId, userId);
            Player? player = await _gameRepo.FindPlayerByUserId(id, userId);
            if (player is not null)
            {
                userIsOz = player.Role == Player.gameRole.Oz;
            }

            IEnumerable<GameLogData> logData = await Task.Run(async ()
                => await FormatLogData(game.EventLog));

            if (userIsMod || userIsOz)
                return Ok(logData.ToList());

            // Filter the log data
            var filteredLogData = await Task.Run(() =>
            {
                var data = logData.Where(log =>
                    FilterLogData(log)
                );

                UserData ozUser = new UserData
                {
                    FullName = "OZ",
                    Email = string.Empty,
                    UserId = string.Empty
                };

                foreach (var item in data)
                {
                    if (item.EventType is GameEvent.Tag)
                    {
                        if ((item as TagEventLogData)?.OzTagger ?? true)
                        {
                            item.User = ozUser;
                        }
                    }
                }

                return data;
            });

            return Ok(filteredLogData.ToList());
        }

        private bool FilterLogData(GameLogData data)
        {

            if (data.EventType is not GameEvent.PlayerRoleChangedByMod) return true;

            RoleSetEventLogData? logData = data as RoleSetEventLogData;
            if (logData is null) return false;

            return logData.Role is not Player.gameRole.Oz && logData.Instigator is not null;
        }

        private async Task<UserData?> GetUserDataFromId(string id)
        {
            // Null mod if the max tags is reached
            if (id is "ozmaxtagsreached") return null;
            User target = await _userRepo.GetUserById(id);
            return new UserData
            {
                FullName = target.FullName,
                Email = target.Email,
                UserId = id
            };
        }

        private async Task<IEnumerable<GameLogData>> FormatLogData(IEnumerable<GameEventLog> log)
        {
            List<GameLogData> logData = new();
            foreach (GameEventLog logEvent in log)
            {
                var logUser = await _userRepo.GetUserById(logEvent.UserId);
                UserData userData = new()
                {
                    FullName = logUser.FullName,
                    Email = logUser.Email,
                    UserId = logEvent.UserId,
                };

                GameLogData logInfo = logEvent switch
                {
                    { GameEvent: GameEvent.GameCreated } => new GameCreatedLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData,
                        GameName = (string)logEvent.AdditionalInfo["name"]
                    },
                    { GameEvent: GameEvent.GameStarted } => new GameStartedEventLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData
                    },
                    { GameEvent: GameEvent.Tag } => new TagEventLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData,
                        TaggerTagCount = (int)logEvent.AdditionalInfo["taggertagcount"],
                        TagReceiver = (await GetUserDataFromId((string)logEvent.AdditionalInfo["tagreciever"]))!,
                        OzTagger = (bool)logEvent.AdditionalInfo["oztagger"]
                    },
                    { GameEvent: GameEvent.PlayerRoleChangedByMod } => new RoleSetEventLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData,
                        Role = (Player.gameRole)logEvent.AdditionalInfo["role"],
                        Instigator = await GetUserDataFromId((string)logEvent.AdditionalInfo["modid"])
                    },
                    { GameEvent: GameEvent.ActiveStatusChanged } => new ActiveStatusChangedEventLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData,
                        Status = (Game.GameStatus)logEvent.AdditionalInfo["status"]
                    },
                    { GameEvent: GameEvent.PlayerJoined } => new PlayerJoinedEventLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData
                    },
                    { GameEvent: GameEvent.PlayerLeft } => new PlayerLeftEventLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData
                    },
                    _ => new PlayerJoinedEventLogData
                    {
                        Timestamp = logEvent.Timestamp,
                        User = userData,
                    }
                };

                logData.Add(logInfo);
            }

            return logData;
        }

        /// <summary>
        /// Log a tag
        /// </summary>
        /// <param name="id">The ID for the game</param>
        /// <param name="tag">The model containing tag information</param>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /tag
        ///     {
        ///         "receiverGameId": "1234"
        ///     }
        ///     
        /// </remarks>
        /// <response code="200">Returns information about the logged tag</response>
        /// <response code="400">Missing parameter in tag</response>
        /// <response code="401">Could not log tag; returns error message</response>
        /// <response code="404">Could not find game or tag receiver</response>
        [HttpPost("{id}/tag")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TagResult>> Tag(string id, [FromBody] TagModel tag)
        {
            string userId = GetDatabaseId(this.User);

            Game? game = await _gameRepo.FindGameById(id);

            if (game is null) return NotFound(
                new TagResult
                {
                    Succeeded = false,
                    Error = $"Could not find Game with ID: {id}"
                });

            Player? tagger = await _gameRepo.FindPlayerByUserId(id, userId);

            if (tagger is null)
                return Unauthorized(new TagResult
                {
                    Succeeded = false,
                    Error = "Not in game"
                });

            if (tagger.Role == Player.gameRole.Human)
                return Unauthorized(new TagResult
                {
                    Succeeded = false,
                    Error = "You are a human, you cannot tag players"
                });

            if (tagger.Role == Player.gameRole.Oz && tagger.Tags >= game.OzMaxTags)
                return Unauthorized(new TagResult
                {
                    Succeeded = false,
                    Error = "You have reached the maximum number of OZ tags"
                });

            if (tag.ReceiverGameId is null)
                return BadRequest(new TagResult
                {
                    Succeeded = false,
                    Error = "Missing parameter: \"receiverGameId\""
                });

            Player? receiver = await _gameRepo.FindPlayerByGameId(id, tag.ReceiverGameId);

            if (receiver is null)
                return NotFound(new TagResult
                {
                    Succeeded = false,
                    Error = $"Could not find player with GameId: {tag.ReceiverGameId}"
                });

            if (receiver.Role != Player.gameRole.Human)
                return Unauthorized(new TagResult
                {
                    Succeeded = false,
                    Error = "The player you are trying to tag is not a human"
                });

            await _gameRepo.LogTag(id, userId, tag.ReceiverGameId);

            return Ok(new TagResult
            {
                Succeeded = true,
                Tags = tagger.Tags + 1,
                ReceiverUserId = receiver.UserId,
                OzTags = tagger.Role == Player.gameRole.Oz ? game.OzMaxTags : null
            });
        }

        /// <summary>
        /// Get info about a specific game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Returns info about the game</response>
        /// <response code="404">Could not find game</response>
        [HttpGet("{id}/info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GameInfo>> GetGameInfo(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound($"Could not find Game with ID: {id}");

            return Ok(new GameInfo(game));
        }

        /// <summary>
        /// Get the configuration for a specific game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Returns the game configuration</response>
        /// <response code="401">User does not have moderator permissions</response>
        /// <response code="404">Could not find game</response>
        [HttpGet("{id}/config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GameConfig>> GetGameConfig(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound($"Could not find Game with ID: {id}");

            if (!await UserIsModerator(game.OrgId, GetDatabaseId(User)))
                return Unauthorized("Missing required permissions");

            return Ok(new GameConfig(game));
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="id"></param>
        /// <param name="gameConfig"></param>
        /// <returns></returns>
        [HttpPost("{id}/config")]
        public async Task<ActionResult> SetGameConfig(string id, [FromBody] GameConfig gameConfig)
        {
            if (!UserIsAuthenticated(HttpContext))
                return Forbid();

            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound($"Could not find Game with ID: {id}");

            // TODO authorization

            // TODO update settings
            return Ok();
        }

        /// <summary>
        /// Set a random number of OZs
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <remarks>
        /// TODO: Add example
        /// </remarks>
        /// <response code="200">Returns the randomly set OZs</response>
        /// <response code="400">Could not complete request</response>
        /// <response code="401">User is does not have moderator permissions</response>
        /// <response code="404">Could not find game</response>
        [HttpPost("{id}/randomoz")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RandomOzResult>> SetRandomOzs(string id, [FromBody] RandomOzModel request)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound(new RandomOzResult
            {
                Succeeded = false,
                Error = $"Could not find Game with ID: {id}"
            });

            string userId = GetDatabaseId(User);

            if (!await UserIsModerator(game.OrgId, userId))
                return Unauthorized(new RandomOzResult
                {
                    Succeeded = false,
                    Error = "Missing required permissions"
                });

            if (request.NumRandomOzs <= 0)
                return BadRequest(new RandomOzResult
                {
                    Succeeded = false,
                    Error = $"Cannot set {request.NumRandomOzs} OZs. Must be greater than 0."
                });

            if (request.NumRandomOzs > game.OzPool.Count)
                return BadRequest(new RandomOzResult
                {
                    Succeeded = false,
                    Error = $"Cannot set {request.NumRandomOzs} OZs. There are only {game.OzPool.Count} OZs in the pool."
                });

            var updatedGame = await _gameRepo.AssignRandomOzs(id, request.NumRandomOzs, userId);

            IEnumerable<string> setOzIds = game.OzPool.Except(updatedGame.OzPool);

            List<UserData> setOzs = new();
            foreach (var ozUserId in setOzIds)
            {
                var user = await _userRepo.GetUserById(ozUserId);
                setOzs.Add(new UserData
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    UserId = user.Id
                });
            }

            return Ok(new RandomOzResult
            {
                Succeeded = true,
                RandomOzs = setOzs
            });
        }

        /// <summary>
        /// Get the list of users in the OZ pool
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Returns the list of users in the OZ pool</response>
        /// <response code="401">User does not have moderator permissions</response>
        /// <response code="404">Could not find game</response>
        [HttpGet("{id}/ozs/list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<UserData>>> GetOzPool(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound($"Could not find Game with ID: {id}");

            string userId = GetDatabaseId(User);

            if (!await UserIsModerator(game.OrgId, userId))
            {
                return Unauthorized("You do not have permission to view this");
            }

            IEnumerable<UserData> users = await Task.Run(async ()
                => await UserIdsToUserData(game.OzPool));

            return Ok(users);
        }

        private async Task<IEnumerable<UserData>> UserIdsToUserData(IEnumerable<string> userIds)
        {
            var users = new List<UserData>();

            foreach (var userId in userIds)
            {
                var u = await _userRepo.GetUserById(userId);
                users.Add(new UserData
                {
                    FullName = u.FullName,
                    UserId = userId,
                    Email = u.Email,
                });
            }

            return users;
        }

        /// <summary>
        /// Add authenticated user to the OZ pool
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Returns success</response>
        /// <response code="400">User is already in OZ pool</response>
        /// <response code="401">User is not in the game</response>
        /// <response code="404">Could not find game</response>
        [HttpPost("{id}/ozs/join")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PostResult>> JoinOzPool(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound(new PostResult
            {
                Succeeded = false,
                Error = $"Could not find Game with ID: {id}"
            });

            string userId = GetDatabaseId(User);

            Player? player = await _gameRepo.FindPlayerByUserId(id, userId);
            if (player is null)
                return Unauthorized(new PostResult
                {
                    Succeeded = false,
                    Error = "Not in game"
                });

            if (game.OzPool.Contains(userId))
                return BadRequest(new PostResult
                {
                    Succeeded = false,
                    Error = "Already in OZ pool"
                });

            await _gameRepo.AddPlayerToOzPool(id, userId);

            return Ok(new PostResult { Succeeded = true });
        }

        /// <summary>
        /// Remove authenticated user from the OZ pool
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Returns success</response>
        /// <response code="400">User is not in OZ pool</response>
        /// <response code="401">User is not in the game</response>
        /// <response code="404">Could not find game</response>
        [HttpPost("{id}/ozs/leave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PostResult>> LeaveOzPool(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound(new PostResult
            {
                Succeeded = false,
                Error = $"Could not find Game with ID: {id}"
            });

            string userId = GetDatabaseId(User);

            Player? player = await _gameRepo.FindPlayerByUserId(id, userId);
            if (player is null)
                return Unauthorized(new PostResult
                {
                    Succeeded = false,
                    Error = "Not in game"
                });

            if (!game.OzPool.Contains(userId))
                return BadRequest(new PostResult
                {
                    Succeeded = false,
                    Error = "Not in OZ pool"
                });

            await _gameRepo.RemovePlayerFromOzPool(id, userId);

            return Ok(new PostResult { Succeeded = true });
        }

        [HttpGet("{id}/ozs/inpool")]
        public async Task<ActionResult<bool>> UserInOzPool(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound();
            string userId = GetDatabaseId(User);
            return Ok(game.OzPool.Contains(userId));
        }

        /// <summary>
        /// Start a new game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <remarks>
        /// Only games with the status 'New' can be started.
        /// </remarks>
        /// <response code="200">Returns success</response>
        /// <response code="400">Could not complete request: Game already started</response>
        /// <response code="401">User does not have moderator permissions</response>
        /// <response code="404">Could not find game</response>
        [HttpPost("{id}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PostResult>> StartGame(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound(new PostResult
            {
                Succeeded = false,
                Error = $"Could not find game with ID {id}"
            });

            string userId = GetDatabaseId(User);

            if (!await UserIsModerator(game.OrgId, userId))
                return Unauthorized(new PostResult
                {
                    Succeeded = false,
                    Error = "Missing required permissions"
                });

            if (game.Status != Game.GameStatus.New)
                return BadRequest(new PostResult
                {
                    Succeeded = false,
                    Error = "Game already started"
                });

            await _gameRepo.StartGame(id, userId);

            return Ok(new PostResult { Succeeded = true });
        }

        /// <summary>
        /// Pause an active game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <remarks>
        /// Only a game with the status 'Active' can be paused.
        /// </remarks>
        /// <response code="200">Returns success</response>
        /// <response code="400">Could not complete request: Game game is not 'Active'</response>
        /// <response code="401">User does not have moderator permissions</response>
        /// <response code="404">Could not find game</response>
        [HttpPost("{id}/pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PostResult>> PauseGame(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound(new PostResult
            {
                Succeeded = false,
                Error = $"Could not find Game with ID: {id}"
            });

            string userId = GetDatabaseId(User);

            if (!await UserIsModerator(game.OrgId, userId))
                return Unauthorized(new PostResult
                {
                    Succeeded = false,
                    Error = "Missing required permissions"
                });

            if (game.Status != Game.GameStatus.Active)
                return BadRequest(new PostResult
                {
                    Succeeded = false,
                    Error = "Game is not 'Active'"
                });

            await _gameRepo.PauseGame(id, userId);

            return Ok(new PostResult { Succeeded = true });
        }

        /// <summary>
        /// Resume a paused game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <remarks>
        /// Only a paused game can be resumed.
        /// </remarks>
        /// <response code="200">Returns success</response>
        /// <response code="400">Could not complete request: Game game is not 'Paused'</response>
        /// <response code="401">User does not have moderator permissions</response>
        /// <response code="404">Could not find game</response>
        [HttpPost("{id}/resume")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PostResult>> ResumeGame(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound(new PostResult
            {
                Succeeded = false,
                Error = $"Could not find Game with ID: {id}"
            });

            string userId = GetDatabaseId(User);

            if (!await UserIsModerator(game.OrgId, userId))
                return Unauthorized(new PostResult
                {
                    Succeeded = false,
                    Error = "Missing required permissions"
                });

            if (game.Status != Game.GameStatus.Paused)
                return BadRequest(new PostResult
                {
                    Succeeded = false,
                    Error = "Game is not 'Paused'"
                });

            await _gameRepo.ResumeGame(id, userId);

            return Ok(new PostResult { Succeeded = true });
        }

        /// <summary>
        /// Add the currently authenticated user to the specified game
        /// </summary>
        /// <param name="id">The ID of the game to join</param>
        /// <returns></returns>
        [HttpPost("{id}/join")]
        public async Task<ActionResult<JoinGameResult>> JoinGame(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound(new PostResult
            {
                Succeeded = false,
                Error = $"Could not find Game with ID: {id}"
            });

            string userId = GetDatabaseId(User);

            // Game is not current -> BadRequest
            if (!game.IsCurrent) return BadRequest(new JoinGameResult
            {
                Succeeded = false,
                Errors = new List<string> { $"Cannot join {game.Name} because it has ended" }
            });

            // Player in game -> BadRequest
            Player? player = await _gameRepo.FindPlayerByUserId(id, userId);
            if (player is not null) return BadRequest(new JoinGameResult
            {
                Succeeded = false,
                Errors = new List<string> { $"Already in {game.Name}" }
            });

            Organization org = await _orgRepo.GetOrgById(game.OrgId);

            List<string> errors = new();
            if (org.RequireVerifiedEmailForPlayer)
            {
                bool userEmailVerified = await EmailConfirmed(User);
                if (!userEmailVerified)
                    errors.Add("Confirmed email required");
            }

            if (org.RequireProfilePictureForPlayer)
            {
                bool hasProfilePicture = _imageService.GetImagePath(userId, "user") is not null;
                if (!hasProfilePicture)
                    errors.Add("Profile picture required");
            }

            if (errors.Count != 0)
                return BadRequest(new JoinGameResult
                {
                    Succeeded = false,
                    Errors = errors
                });

            try
            {
                await _gameRepo.AddPlayer(id, userId);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest(new JoinGameResult
                {
                    Succeeded = false,
                    Errors = new List<string> { $"Unexpected error, unable to add player to {game.Name}" }
                });
            }

            player = await _gameRepo.GetPlayerByGameId(id, userId);
            User user = await _userRepo.GetUserById(userId);

            return Ok(new JoinGameResult
            {
                Succeeded = true,
                CreatedPlayer = new PlayerData
                {
                    GameId = player.GameId,
                    Tags = player.Tags,
                    Role = player.Role,
                    User = new UserData
                    {
                        Email = user.Email,
                        FullName = user.FullName,
                        UserId = userId,
                    }
                }
            });
        }

        [HttpPost("{id}/setrole")]
        public async Task<ActionResult> SetGameRole(string id, SetGameRoleRequest request)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null)
                return NotFound();

            string userId = GetDatabaseId(User);
            if (!await UserIsModerator(game.OrgId, userId))
                return Forbid();

            Player? player = await _gameRepo.FindPlayerByUserId(id, request.UserId);
            if (player is null)
                return BadRequest();

            await _gameRepo.SetPlayerToRole(id, request.UserId, request.Role, GetDatabaseId(User));

            return Ok();
        }

        [HttpGet("{id}/myinfo")]
        public async Task<ActionResult<PlayerData>> Me(string id)
        {
            Game? game = await _gameRepo.FindGameById(id);
            if (game is null) return NotFound();

            string userId = GetDatabaseId(User);

            Player? player = await _gameRepo.FindPlayerByUserId(id, userId);
            if (player is null) return NoContent();

            User user = await _userRepo.GetUserById(userId);

            return Ok(new PlayerData
            {
                GameId = player.GameId,
                Role = player.Role,
                Tags = player.Tags,
                User = new UserData
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    UserId = user.Id
                }
            });
        }

        private async Task<bool> UserIsModerator(string orgId, string userId)
        {
            return await _orgRepo.IsModOfOrg(orgId, userId) ||
            await _orgRepo.IsAdminOfOrg(orgId, userId);
        }

        private bool UserIsAuthenticated(HttpContext context)
            => context.User.Identity?.IsAuthenticated ?? false;

        private string GetDatabaseId(ClaimsPrincipal user)
        {
            return user.Claims.FirstOrDefault(
                c => c.Type == "DatabaseId"
            )?.Value ?? "Can't find ID";
        }
        //=> user.Claims.FirstOrDefault(
        //    c => c.Type == "DatabaseId"
        //)?.Value ?? "Can't find ID";

        private async Task<bool> EmailConfirmed(ClaimsPrincipal user)
            => (await _userManager.GetUserAsync(user))?.EmailConfirmed ?? false;

        private async Task<bool> GameExists(string id)
            => await _gameRepo.FindGameById(id) is not null;
    }
}
