using FluentResults;
using HVZ.Web.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace HVZ.Web.Client.Interfaces
{
    public interface IUserService
    {
        public Task<Result<UserData>> GetCurrentUser();

        public Task<Result<bool>> EmailIsConfirmed();

        public Task<Result<UserData>> GetUserById(string userId);

        public Task<Result<bool>> SetImage(IBrowserFile image);
        public Task<Result> ForgotPassword(ForgotPasswordRequest request);
    }
}
