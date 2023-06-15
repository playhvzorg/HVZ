using HVZ.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HVZ.Web.Server.Hubs
{
    [Authorize]
    public class GameHub : Hub, IDisposable
    {
        // TODO: Authentication
        // TODO: Better client management

        private readonly IGameRepo _gameRepo;

        public GameHub(IGameRepo gameRepo)
        {
            _gameRepo = gameRepo;
            _gameRepo.PlayerJoinedGame += PlayerJoinedGame;
            _gameRepo.PlayerJoinedOzPool += PlayerJoinedOzPool;
            _gameRepo.PlayerLeftOzPool += PlayerLeftOzPool;
            _gameRepo.TagLogged += TagLogged;
            _gameRepo.GameActiveStatusChanged += GameActiveStatusChanged;
            _gameRepo.GameCreated += GameCreated;
            _gameRepo.GameSettingsChanged += GameSettingsChanged;
            _gameRepo.RandomOzsSet += RandomOzsSet;
        }

        private void PlayerJoinedGame(object? sender, PlayerUpdatedEventArgs args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("PlayerJoined");
            });
        }

        private void PlayerJoinedOzPool(object? sender, OzPoolUpdatedEventArgs args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("OzPoolPlayerJoined");
            });
        }

        private void PlayerLeftOzPool(object? sender, OzPoolUpdatedEventArgs args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("OzPoolPlayerLeft");
            });
        }

        private void TagLogged(object? sender, TagEventArgs args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("TagLogged");
                await Clients.All.SendAsync("RoleChanged");
            });
        }

        private void GameActiveStatusChanged(object? sender, GameStatusChangedEvent args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("GameStatusChanged");
            });
        }

        private void GameCreated(object? sender, GameUpdatedEventArgs args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("GameCreated");
            });
        }

        private void GameSettingsChanged(object? sender, GameUpdatedEventArgs args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("GameSettingsChanged");
            });
        }

        private void RandomOzsSet(object? sender, RandomOzEventArgs args)
        {
            Task.Run(async () =>
            {
                await Clients.All.SendAsync("RandomOzsSet");
            });
        }

        public new void Dispose()
        {
            _gameRepo.PlayerJoinedGame -= PlayerJoinedGame;
            _gameRepo.PlayerJoinedOzPool -= PlayerJoinedOzPool;
            _gameRepo.PlayerLeftOzPool -= PlayerLeftOzPool;
            _gameRepo.TagLogged -= TagLogged;
            _gameRepo.GameActiveStatusChanged -= GameActiveStatusChanged;
            _gameRepo.GameCreated -= GameCreated;
            _gameRepo.GameSettingsChanged -= GameSettingsChanged;
            _gameRepo.RandomOzsSet -= RandomOzsSet;

            base.Dispose();
        }
    }
}
