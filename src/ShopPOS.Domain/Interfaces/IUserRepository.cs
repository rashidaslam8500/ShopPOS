using ShopPOS.Domain.Entities;



namespace ShopPOS.Domain.Interfaces;



public interface IUserRepository

{

    Task<User?> GetByUsernameAsync(string username);

    Task<User?> GetByIdAsync(int id);

    Task UpdatePasswordAsync(int userId, string passwordHash, string passwordSalt);

    Task UpdateSecurityQuestionsAsync(

        int userId,

        string question1,

        string answer1Hash,

        string answer1Salt,

        string question2,

        string answer2Hash,

        string answer2Salt);

}

