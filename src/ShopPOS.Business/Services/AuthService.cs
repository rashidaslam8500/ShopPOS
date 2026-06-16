using ShopPOS.Data.Security;

using ShopPOS.Domain.Enums;

using ShopPOS.Domain.Interfaces;

using ShopPOS.Domain.Models;

using ShopPOS.Domain.Security;



namespace ShopPOS.Business.Services;



public interface IAuthService

{

    Task<UserSession?> LoginAsync(string username, string password);

    Task LogoutAsync();

    Task ChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword);

    Task<bool> VerifyOwnerPasswordAsync(string password);

    Task<bool> HasSecurityQuestionsAsync(int? userId = null);

    Task<SecurityRecoveryQuestions?> GetRecoveryQuestionsAsync(string username);

    Task SetSecurityQuestionsAsync(string question1, string answer1, string question2, string answer2);

    Task ResetPasswordViaSecurityQuestionsAsync(

        string username,

        string answer1,

        string answer2,

        string newPassword,

        string confirmPassword);

}



public class AuthService : IAuthService

{

    private readonly IUserRepository _users;

    private readonly CurrentSession _session;

    private readonly IAuditService _audit;



    public AuthService(IUserRepository users, CurrentSession session, IAuditService audit)

    {

        _users = users;

        _session = session;

        _audit = audit;

    }



    public async Task<UserSession?> LoginAsync(string username, string password)

    {

        var user = await _users.GetByUsernameAsync(username.Trim());

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))

            return null;



        var session = new UserSession

        {

            UserId = user.Id,

            Username = user.Username,

            DisplayName = user.DisplayName,

            Role = user.Role

        };



        _session.SetUser(session);

        await _audit.LogAsync(AuditActionType.Login, "User", user.Id.ToString(), $"{user.Username} logged in as {user.Role}");

        return session;

    }



    public async Task LogoutAsync()

    {

        if (_session.User is not null)

            await _audit.LogAsync(AuditActionType.Logout, "User", _session.User.UserId.ToString(), $"{_session.User.Username} logged out");

        _session.Clear();

    }



    public async Task ChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword)

    {

        if (_session.User is null)

            throw new InvalidOperationException("Not authenticated.");



        ValidateNewPassword(newPassword, confirmPassword);



        var user = await _users.GetByIdAsync(_session.User.UserId)

            ?? throw new InvalidOperationException("User account not found.");



        if (!PasswordHasher.Verify(oldPassword, user.PasswordHash, user.PasswordSalt))

            throw new InvalidOperationException("Current password is incorrect.");



        var (hash, salt) = PasswordHasher.HashPassword(newPassword);

        await _users.UpdatePasswordAsync(user.Id, hash, salt);

        await _audit.LogAsync(AuditActionType.SettingsChanged, "User", user.Id.ToString(), "Password changed");

    }



    public async Task<bool> VerifyOwnerPasswordAsync(string password)

    {

        var owner = await _users.GetByUsernameAsync("owner");

        if (owner is null)

            return false;



        return PasswordHasher.Verify(password, owner.PasswordHash, owner.PasswordSalt);

    }



    public async Task<bool> HasSecurityQuestionsAsync(int? userId = null)

    {

        var id = userId ?? _session.User?.UserId;

        if (id is null)

            return false;



        var user = await _users.GetByIdAsync(id.Value);

        return user is not null && HasSecurityQuestions(user);

    }



    public async Task<SecurityRecoveryQuestions?> GetRecoveryQuestionsAsync(string username)

    {

        var user = await _users.GetByUsernameAsync(username.Trim());

        if (user is null || !HasSecurityQuestions(user))

            return null;



        return new SecurityRecoveryQuestions

        {

            Question1 = user.SecurityQuestion1!,

            Question2 = user.SecurityQuestion2!

        };

    }



    public async Task SetSecurityQuestionsAsync(string question1, string answer1, string question2, string answer2)

    {

        if (_session.User is null)

            throw new InvalidOperationException("Not authenticated.");



        ValidateSecurityQuestionSetup(question1, answer1, question2, answer2);



        var (hash1, salt1) = HashSecurityAnswer(answer1);

        var (hash2, salt2) = HashSecurityAnswer(answer2);



        await _users.UpdateSecurityQuestionsAsync(

            _session.User.UserId,

            question1.Trim(),

            hash1,

            salt1,

            question2.Trim(),

            hash2,

            salt2);



        await _audit.LogAsync(

            AuditActionType.SettingsChanged,

            "User",

            _session.User.UserId.ToString(),

            "Security recovery questions updated");

    }



    public async Task ResetPasswordViaSecurityQuestionsAsync(

        string username,

        string answer1,

        string answer2,

        string newPassword,

        string confirmPassword)

    {

        ValidateNewPassword(newPassword, confirmPassword);



        var user = await _users.GetByUsernameAsync(username.Trim())

            ?? throw new InvalidOperationException("Account not found.");



        if (!HasSecurityQuestions(user))

            throw new InvalidOperationException("Security questions are not configured for this account. Contact the shop owner.");



        if (string.IsNullOrWhiteSpace(answer1) || string.IsNullOrWhiteSpace(answer2))

            throw new InvalidOperationException("Answer both security questions.");



        if (!VerifySecurityAnswer(answer1, user.SecurityAnswer1Hash!, user.SecurityAnswer1Salt!)

            || !VerifySecurityAnswer(answer2, user.SecurityAnswer2Hash!, user.SecurityAnswer2Salt!))

            throw new InvalidOperationException("Security answers are incorrect.");



        var (hash, salt) = PasswordHasher.HashPassword(newPassword);

        await _users.UpdatePasswordAsync(user.Id, hash, salt);

        await _audit.LogAsync(AuditActionType.SettingsChanged, "User", user.Id.ToString(), "Password reset via security questions");

    }



    private static bool HasSecurityQuestions(Domain.Entities.User user) =>

        !string.IsNullOrWhiteSpace(user.SecurityQuestion1)

        && !string.IsNullOrWhiteSpace(user.SecurityAnswer1Hash)

        && !string.IsNullOrWhiteSpace(user.SecurityAnswer1Salt)

        && !string.IsNullOrWhiteSpace(user.SecurityQuestion2)

        && !string.IsNullOrWhiteSpace(user.SecurityAnswer2Hash)

        && !string.IsNullOrWhiteSpace(user.SecurityAnswer2Salt);



    private static void ValidateSecurityQuestionSetup(string question1, string answer1, string question2, string answer2)

    {

        if (!SecurityQuestionCatalog.StandardQuestions.Contains(question1.Trim())

            || !SecurityQuestionCatalog.StandardQuestions.Contains(question2.Trim()))

            throw new InvalidOperationException("Choose two standard security questions from the list.");



        if (string.Equals(question1.Trim(), question2.Trim(), StringComparison.OrdinalIgnoreCase))

            throw new InvalidOperationException("Choose two different security questions.");



        if (string.IsNullOrWhiteSpace(answer1) || string.IsNullOrWhiteSpace(answer2))

            throw new InvalidOperationException("Provide an answer for each security question.");



        if (answer1.Trim().Length < 2 || answer2.Trim().Length < 2)

            throw new InvalidOperationException("Security answers must be at least 2 characters.");

    }



    private static void ValidateNewPassword(string newPassword, string confirmPassword)

    {

        if (string.IsNullOrWhiteSpace(newPassword))

            throw new InvalidOperationException("Enter a new password.");



        if (newPassword.Length < 6)

            throw new InvalidOperationException("New password must be at least 6 characters.");



        if (newPassword != confirmPassword)

            throw new InvalidOperationException("New password and confirmation do not match.");

    }



    private static (string Hash, string Salt) HashSecurityAnswer(string answer) =>

        PasswordHasher.HashPassword(NormalizeSecurityAnswer(answer));



    private static bool VerifySecurityAnswer(string answer, string hash, string salt) =>

        PasswordHasher.Verify(NormalizeSecurityAnswer(answer), hash, salt);



    private static string NormalizeSecurityAnswer(string answer) =>

        answer.Trim().ToLowerInvariant();

}

