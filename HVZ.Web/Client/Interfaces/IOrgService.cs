using FluentResults;
using HVZ.Web.Shared.Models;

namespace HVZ.Web.Client.Interfaces
{
    public interface IOrgService
    {
        public Task<Result<string>> GetOrgIdFromUrl(string url);

        public Task<Result<OrgInfo>> GetOrgInfo(string orgId);

        public Task<Result<bool>> IsAdmin(string? userId);

        public Task<Result<bool>> IsMod(string? userId);

        public Task<Result<string>> CreateGame(string orgId, CreateGameRequest request);

        public Task<Result<OrgAuthorization>> GetAuthorization(string orgId);
    }
}
