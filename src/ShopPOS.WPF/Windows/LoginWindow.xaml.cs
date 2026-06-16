using System.Windows;

using System.Windows.Input;

using Microsoft.Extensions.DependencyInjection;

using ShopPOS.WPF.ViewModels;



namespace ShopPOS.WPF.Windows;



public partial class LoginWindow : Window

{

    private readonly LoginViewModel _vm;

    private readonly ForgotPasswordWindow _forgotPasswordWindow;



    public LoginWindow(LoginViewModel vm, ForgotPasswordWindow forgotPasswordWindow)

    {

        InitializeComponent();

        _vm = vm;

        _forgotPasswordWindow = forgotPasswordWindow;

        DataContext = vm;

        vm.LoginSucceeded += OnLoginSucceeded;

        Loaded += OnLoaded;

        KeyDown += (_, e) =>

        {

            if (e.Key == Key.Escape)

                Application.Current.Shutdown();

        };

    }



    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        Activate();
        UsernameBox.Focus();
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)

    {

        try

        {

            DialogResult = true;

            Close();

        }

        catch (Exception ex)

        {

            MessageBox.Show(

                $"Could not complete login.\n\n{ex.Message}",

                "Bhai Gee POS — Login Error",

                MessageBoxButton.OK,

                MessageBoxImage.Error);

        }

    }



    private async void SignIn_Click(object sender, RoutedEventArgs e) => await AttemptLoginAsync();



    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)

    {

        if (e.Key == Key.Enter)

        {

            e.Handled = true;

            await AttemptLoginAsync();

        }

    }



    private void UsernameBox_KeyDown(object sender, KeyEventArgs e)

    {

        if (e.Key == Key.Enter)

        {

            e.Handled = true;

            PasswordBox.Focus();

        }

    }



    private async Task AttemptLoginAsync()

    {

        var password = PasswordBox.Password;

        var success = await _vm.TryLoginAsync(password);

        if (success)

            return;



        PasswordBox.Clear();

        PasswordBox.Focus();

    }



    private void ForgotPassword_Click(object sender, RoutedEventArgs e)

    {

        _forgotPasswordWindow.Owner = this;

        _forgotPasswordWindow.ShowDialog();

    }

}

