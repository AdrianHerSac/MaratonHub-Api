using MaratonHub.Api.Users.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.Users.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
        }

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByGoogleIdAsync(string googleId)
        {
            return await _users.Find(u => u.GoogleId == googleId).FirstOrDefaultAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await _users.InsertOneAsync(user);
            return user;
        }
    }
}
