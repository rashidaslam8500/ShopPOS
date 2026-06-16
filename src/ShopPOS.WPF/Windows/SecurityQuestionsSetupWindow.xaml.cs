using System.Windows;

using ShopPOS.WPF.ViewModels;



namespace ShopPOS.WPF.Windows;



public partial class SecurityQuestionsSetupWindow : Window

{

    private readonly SecurityQuestionsSetupViewModel _vm;



    public SecurityQuestionsSetupWindow(SecurityQuestionsSetupViewModel vm)

    {

        InitializeComponent();

        _vm = vm;

        DataContext = vm;

    }



    private async void Save_Click(object sender, RoutedEventArgs e)

    {

        var success = await _vm.SaveAsync(Answer1Box.Text, Answer2Box.Text);

        if (!success)

            return;



        MessageBox.Show(

            "Security questions saved. You can use them to recover your password if needed.",

            "Security Setup",

            MessageBoxButton.OK,

            MessageBoxImage.Information);



        DialogResult = true;

        Close();

    }

}

