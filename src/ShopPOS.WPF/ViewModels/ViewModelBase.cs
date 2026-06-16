using CommunityToolkit.Mvvm.ComponentModel;

namespace ShopPOS.WPF.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string? _statusMessage;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    protected async Task<bool> RunSafeAsync(Func<Task> action, string? successMessage = null)
    {
        try
        {
            IsBusy = true;
            StatusMessage = null;
            await action();
            if (successMessage is not null)
                StatusMessage = successMessage;
            return true;
        }
        catch (Exception ex)
        {
            var message = GetDisplayMessage(ex);
            StatusMessage = message;
            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetDisplayMessage(Exception ex)
    {
        var deepest = ex;
        while (deepest.InnerException is not null)
            deepest = deepest.InnerException;

        if (ReferenceEquals(deepest, ex))
            return ex.Message;

        return $"{ex.Message}\n\n{deepest.Message}";
    }
}
