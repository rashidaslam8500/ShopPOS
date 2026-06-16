using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using ShopPOS.Business.Services;

using ShopPOS.Domain.Models;

using ShopPOS.Domain.Security;



namespace ShopPOS.WPF.ViewModels;



public partial class LoginViewModel : ObservableObject

{

    private readonly IAuthService _auth;



    [ObservableProperty] private string _username = string.Empty;

    [ObservableProperty] private string? _errorMessage;



    public UserSession? Session { get; private set; }



    public LoginViewModel(IAuthService auth) => _auth = auth;



    public async Task<bool> TryLoginAsync(string password)

    {

        ErrorMessage = null;



        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))

        {

            ErrorMessage = "Enter username and password.";

            return false;

        }



        try

        {

            Session = await _auth.LoginAsync(Username, password);

            if (Session is null)

            {

                ErrorMessage = "Invalid username or password.";

                return false;

            }



            LoginSucceeded?.Invoke(this, EventArgs.Empty);

            return true;

        }

        catch (Exception ex)

        {

            ErrorMessage = "Unable to sign in. Please try again.";

            System.Windows.MessageBox.Show(

                $"Login failed.\n\n{ex.Message}\n\n" +

                "Check that SQL Server is running and the BhaiGeePOS database is accessible.",

                "Bhai Gee POS — Login Error",

                System.Windows.MessageBoxButton.OK,

                System.Windows.MessageBoxImage.Error);

            return false;

        }

    }



    public event EventHandler? LoginSucceeded;

}



public partial class ForgotPasswordViewModel : ObservableObject

{

    private readonly IAuthService _auth;



    [ObservableProperty] private string _username = string.Empty;

    [ObservableProperty] private string? _question1;

    [ObservableProperty] private string? _question2;

    [ObservableProperty] private string? _statusMessage;

    [ObservableProperty] private bool _questionsLoaded;



    public ForgotPasswordViewModel(IAuthService auth) => _auth = auth;



    [RelayCommand]

    private async Task LoadQuestionsAsync()

    {

        StatusMessage = null;

        QuestionsLoaded = false;

        Question1 = null;

        Question2 = null;



        if (string.IsNullOrWhiteSpace(Username))

        {

            StatusMessage = "Enter your username first.";

            return;

        }



        try

        {

            var questions = await _auth.GetRecoveryQuestionsAsync(Username);

            if (questions is null)

            {

                StatusMessage = "No security questions are set for this account. Contact the shop owner.";

                return;

            }



            Question1 = questions.Question1;

            Question2 = questions.Question2;

            QuestionsLoaded = true;

        }

        catch (Exception ex)

        {

            StatusMessage = ex.Message;

        }

    }



    public async Task<bool> ResetPasswordAsync(string answer1, string answer2, string newPassword, string confirmPassword)

    {

        StatusMessage = null;



        if (!QuestionsLoaded)

        {

            StatusMessage = "Load your security questions first.";

            return false;

        }



        try

        {

            await _auth.ResetPasswordViaSecurityQuestionsAsync(

                Username,

                answer1,

                answer2,

                newPassword,

                confirmPassword);



            return true;

        }

        catch (Exception ex)

        {

            StatusMessage = ex.Message;

            return false;

        }

    }

}



public partial class SecurityQuestionsSetupViewModel : ObservableObject

{

    private readonly IAuthService _auth;



    public IReadOnlyList<string> AvailableQuestions => SecurityQuestionCatalog.StandardQuestions;



    [ObservableProperty] private string? _selectedQuestion1;

    [ObservableProperty] private string? _selectedQuestion2;

    [ObservableProperty] private string? _statusMessage;



    public SecurityQuestionsSetupViewModel(IAuthService auth) => _auth = auth;



    public async Task<bool> SaveAsync(string answer1, string answer2)

    {

        StatusMessage = null;



        if (string.IsNullOrWhiteSpace(SelectedQuestion1) || string.IsNullOrWhiteSpace(SelectedQuestion2))

        {

            StatusMessage = "Select two security questions.";

            return false;

        }



        try

        {

            await _auth.SetSecurityQuestionsAsync(

                SelectedQuestion1,

                answer1,

                SelectedQuestion2,

                answer2);



            return true;

        }

        catch (Exception ex)

        {

            StatusMessage = ex.Message;

            return false;

        }

    }

}

