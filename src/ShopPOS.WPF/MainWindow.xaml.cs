using System.Windows;
using System.Windows.Input;
using ShopPOS.WPF.ViewModels;

namespace ShopPOS.WPF;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TabCloseButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { DataContext: TabPageViewModel tab })
            return;

        tab.CloseTabCommand.Execute(null);
    }
}
