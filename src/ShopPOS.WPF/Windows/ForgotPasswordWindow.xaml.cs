using System.Windows;

using ShopPOS.WPF.ViewModels;



namespace ShopPOS.WPF.Windows;



public partial class ForgotPasswordWindow : Window

{

    private readonly ForgotPasswordViewModel _vm;



    public ForgotPasswordWindow(ForgotPasswordViewModel vm)

    {

        InitializeComponent();

        _vm = vm;

        DataContext = vm;

    }



    private async void Reset_Click(object sender, RoutedEventArgs e)

    {

        var success = await _vm.ResetPasswordAsync(

            Answer1Box.Text,

            Answer2Box.Text,

            NewPasswordBox.Password,

            ConfirmPasswordBox.Password);



        if (!success)

            return;



        MessageBox.Show(

            "Your password has been reset. Sign in with your new password.",

            "Password Recovery",

            MessageBoxButton.OK,

            MessageBoxImage.Information);



        DialogResult = true;

        Close();

    }



    private void Cancel_Click(object sender, RoutedEventArgs e)

    {

        DialogResult = false;

        Close();

    }

}

