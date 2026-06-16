using System.Windows;

using System.Windows.Input;



namespace ShopPOS.WPF.Windows;



public partial class OwnerPasswordDialog : Window

{

    public string Password { get; private set; } = string.Empty;



    public OwnerPasswordDialog(

        string titleText = "Permanent Delete Authorization",

        string promptText = "Enter the owner account password to permanently purge selected invoices.",

        string confirmButtonText = "Confirm Delete")

    {

        InitializeComponent();

        TitleTextBlock.Text = titleText;

        PromptTextBlock.Text = promptText;

        ConfirmButton.Content = confirmButtonText;

        Loaded += (_, _) => PasswordBox.Focus();

    }



    private void Confirm_Click(object sender, RoutedEventArgs e)

    {

        Password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(Password))

        {

            MessageBox.Show("Enter the owner password.", "Authorization");

            return;

        }



        DialogResult = true;

        Close();

    }



    private void Cancel_Click(object sender, RoutedEventArgs e)

    {

        DialogResult = false;

        Close();

    }



    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)

    {

        if (e.Key != Key.Enter)

            return;



        e.Handled = true;

        Confirm_Click(sender, e);

    }

}

