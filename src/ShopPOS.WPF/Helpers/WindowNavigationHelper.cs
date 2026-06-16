using System.Windows;

namespace ShopPOS.WPF.Helpers;

public static class WindowNavigationHelper
{
    public static T? FindOpenWindow<T>() where T : Window =>
        Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.IsLoaded);

    public static bool ActivateIfOpen<T>(Action<T>? refreshExisting = null) where T : Window
    {
        var existing = FindOpenWindow<T>();
        if (existing is null)
            return false;

        refreshExisting?.Invoke(existing);

        if (existing.WindowState == WindowState.Minimized)
            existing.WindowState = WindowState.Normal;

        existing.Show();
        existing.Activate();
        existing.Focus();
        return true;
    }

    public static bool ShowDialogUnique<T>(
        Func<T> createWindow,
        Action<T>? configure = null,
        Action<T>? refreshExisting = null) where T : Window
    {
        if (ActivateIfOpen(refreshExisting))
            return existingDialogResult<T>();

        var window = createWindow();
        configure?.Invoke(window);
        return window.ShowDialog() == true;
    }

    public static void ShowUnique<T>(
        Func<T> createWindow,
        Action<T>? configure = null,
        Action<T>? refreshExisting = null) where T : Window
    {
        if (ActivateIfOpen(refreshExisting))
            return;

        var window = createWindow();
        configure?.Invoke(window);
        window.Show();
    }

    private static bool existingDialogResult<T>() where T : Window
    {
        var existing = FindOpenWindow<T>();
        return existing?.DialogResult == true;
    }
}
