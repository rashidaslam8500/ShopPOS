using System.Windows;
using System.Windows.Controls;
using ShopPOS.WPF.Assets;

namespace ShopPOS.WPF.Controls;

public partial class BrandHeader : UserControl
{
    public static readonly DependencyProperty CompactProperty =
        DependencyProperty.Register(
            nameof(Compact),
            typeof(bool),
            typeof(BrandHeader),
            new PropertyMetadata(false, OnCompactChanged));

    public bool Compact
    {
        get => (bool)GetValue(CompactProperty);
        set => SetValue(CompactProperty, value);
    }

    public BrandHeader()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var logo = LogoHelper.CreateColorLogo();
        LogoImage.Source = logo;
        CompactLogoImage.Source = logo;
        ApplyLayoutMode();
    }

    private static void OnCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((BrandHeader)d).ApplyLayoutMode();

    private void ApplyLayoutMode()
    {
        if (HorizontalLayout is null || CompactLayout is null)
            return;

        HorizontalLayout.Visibility = Compact ? Visibility.Collapsed : Visibility.Visible;
        CompactLayout.Visibility = Compact ? Visibility.Visible : Visibility.Collapsed;
    }
}
