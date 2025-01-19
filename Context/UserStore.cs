using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

public class UserStore : IUserStore<User>
{
    private readonly MongoDbContext m_Context;
    public UserStore(MongoDbContext context)
    {
        m_Context = context;
    }
    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {

        FilterDefinition<User> filter = Builders<User>.Filter.Where(
            u => u.ProviderId == user.ProviderId
        );
        User? existingUser = (await m_Context.Users.FindAsync(filter)).FirstOrDefault();
        if (existingUser != null)
        {
            return IdentityResult.Failed([new () { Code = "UserTaken" }]);
        }
        await m_Context.Users.InsertOneAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        await m_Context.Users.DeleteOneAsync(Builders<User>.Filter.Where(u => u.Id == user.Id));
        return IdentityResult.Success;
    }

    public void Dispose()
    {

    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return (await m_Context.Users.FindAsync(Builders<User>.Filter.Where(u => u.Id == userId))).FirstOrDefault();
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var filter = Builders<User>.Filter.Where(u => u.Username == normalizedUserName);
        return (await m_Context.Users.FindAsync(filter)).FirstOrDefault();
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username);
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username);
    }

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        if (userName != null)
        {
            user.Username = userName;
        }
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        ObjectId userId;
        if (ObjectId.TryParse(user.Id, out userId) == false || userId == ObjectId.Empty)
        {
            return IdentityResult.Failed([new() {
                Code = "MissingParam",
                Description = "Request is missing required parameter(s)"
            }]);
        }
        var filter = Builders<User>.Filter.Where(u => u.Id == user.Id);
        await m_Context.Users.ReplaceOneAsync(filter, user);
        return IdentityResult.Success;
    }
}