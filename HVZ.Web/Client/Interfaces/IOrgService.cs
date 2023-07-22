using FluentResults;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace HVZ.Web.Client.Interfaces
{
    public interface IOrgService
    {
        public Task<Result<string>> GetOrgIdFromUrl(string url);
        public Task<Result<OrgInfo>> GetOrgInfo(string orgId);
        public Task<Result<bool>> IsAdmin(string orgId, string userId);
        public Task<Result<bool>> IsMod(string orgId, string userId);
        public Task<Result<string>> CreateGame(string orgId, CreateGameRequest request);
        public Task<Result<OrgAuthorization>> GetAuthorization(string orgId);
        public Task<Result> SetImage(string orgId, IBrowserFile file);
        public Task<Result> UpdateOrgInfo(string orgId, OrgInfo info);
    }
}
