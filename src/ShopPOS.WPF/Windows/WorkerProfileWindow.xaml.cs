using System.Windows;
using ShopPOS.WPF.ViewModels;

namespace ShopPOS.WPF.Windows;

public partial class WorkerProfileWindow : Window
{
    private readonly WorkerProfileViewModel _vm;

    public WorkerProfileWindow(WorkerProfileViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public void Initialize(int workerId)
    {
        Title = "Worker Profile";
        _vm.Initialize(workerId);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
