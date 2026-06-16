using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ShopPOS.WPF.ViewModels;

public partial class TabPageViewModel : ObservableObject, IDisposable
{
    private readonly IServiceScope _scope;
    private readonly Action<TabPageViewModel> _closeHandler;
    private bool _disposed;

    public string Title { get; }
    public TabFeature Feature { get; }
    public ViewModelBase Content { get; }

    public TabPageViewModel(
        string title,
        TabFeature feature,
        ViewModelBase content,
        IServiceScope scope,
        Action<TabPageViewModel> closeHandler)
    {
        Title = title;
        Feature = feature;
        Content = content;
        _scope = scope;
        _closeHandler = closeHandler;
    }

    [RelayCommand]
    private void CloseTab()
    {
        if (_disposed)
            return;

        _closeHandler(this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (Content is IDisposable disposableContent)
            disposableContent.Dispose();

        _scope.Dispose();
    }
}
