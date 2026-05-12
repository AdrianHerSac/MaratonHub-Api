using MaratonHub.Api.Users.Models;

namespace MaratonHub.Api.Users.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(string id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByGoogleIdAsync(string googleId);
        Task<User> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<List<User>> GetAllUsersAsync();
        Task<long> CountUsersAsync();
        Task<bool> DeleteUserAsync(string id);
    }
}
