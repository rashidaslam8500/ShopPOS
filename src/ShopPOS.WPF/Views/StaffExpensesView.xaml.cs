using System.Windows.Controls;
using ShopPOS.Domain.Entities;
using ShopPOS.WPF.ViewModels;
namespace ShopPOS.WPF.Views;

public partial class StaffExpensesView : UserControl
{
    public StaffExpensesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private StaffExpensesViewModel? Vm => DataContext as StaffExpensesViewModel;

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) => Vm?.StartScanner();

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Vm?.StopScanner();
        Vm?.Dispose();
    }

    private void WorkersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (WorkersGrid.SelectedItem is Worker worker)
            Vm?.OpenWorkerProfileCommand.Execute(worker);
    }
}
