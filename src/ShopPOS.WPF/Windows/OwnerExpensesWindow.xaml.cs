using System.Windows;
using ShopPOS.WPF.ViewModels;

namespace ShopPOS.WPF.Windows;

public partial class OwnerExpensesWindow : Window
{
    public OwnerExpensesWindow(OwnerExpensesViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
