using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PosDbContext _db;
    public UserRepository(PosDbContext db) => _db = db;

    public Task<User?> GetByUsernameAsync(string username) =>
        _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

    public Task<User?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

    public async Task UpdatePasswordAsync(int userId, string passwordHash, string passwordSalt)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateSecurityQuestionsAsync(
        int userId,
        string question1,
        string answer1Hash,
        string answer1Salt,
        string question2,
        string answer2Hash,
        string answer2Salt)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.SecurityQuestion1 = question1;
        user.SecurityAnswer1Hash = answer1Hash;
        user.SecurityAnswer1Salt = answer1Salt;
        user.SecurityQuestion2 = question2;
        user.SecurityAnswer2Hash = answer2Hash;
        user.SecurityAnswer2Salt = answer2Salt;
        await _db.SaveChangesAsync();
    }
}
