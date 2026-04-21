using MaratonHub.Api.Users.Models;

namespace MaratonHub.Api.Users.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(string id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByGoogleIdAsync(string googleId);
        Task<User> CreateUserAsync(User user);
    }
}
