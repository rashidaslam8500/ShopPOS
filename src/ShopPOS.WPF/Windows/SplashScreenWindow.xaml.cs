using System.Windows;
using System.Windows.Threading;

namespace ShopPOS.WPF.Windows;

public partial class SplashScreenWindow : Window
{
    private const string DeveloperCredit = "Software by Creative Software";
    private const int LogoAnimationMs = 1500;
    private const int TypingIntervalMs = 38;
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(10);

    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SplashScreenWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public Task WaitForCompletionAsync() => _completion.Task;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var closeTimer = new DispatcherTimer { Interval = DisplayDuration };
        closeTimer.Tick += (_, _) =>
        {
            closeTimer.Stop();
            Close();
            _completion.TrySetResult();
        };
        closeTimer.Start();

        await Task.Delay(LogoAnimationMs);

        for (var i = 1; i <= DeveloperCredit.Length; i++)
        {
            TypingTextBlock.Text = DeveloperCredit[..i];
            await Task.Delay(TypingIntervalMs);
        }
    }
}
