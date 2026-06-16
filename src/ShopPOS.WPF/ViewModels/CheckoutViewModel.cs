using System.Collections.ObjectModel;

using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.DependencyInjection;

using ShopPOS.Business.Services;

using ShopPOS.Domain.Entities;

using ShopPOS.Domain.Enums;

using ShopPOS.Domain.Models;



namespace ShopPOS.WPF.ViewModels;



public partial class CheckoutViewModel : ViewModelBase

{

    private readonly IProductService _products;

    private readonly ISaleService _sales;

    private readonly ISettingsService _settings;

    private readonly IReceiptPrintService _printer;

    private readonly IServiceScopeFactory _scopeFactory;

    private ShopConfig _config = new();

    private readonly Dictionary<int, Product> _catalog = new();

    private CancellationTokenSource? _searchCts;



    [ObservableProperty] private string _searchQuery = string.Empty;

    [ObservableProperty] private string _discountAmountInput = string.Empty;

    [ObservableProperty] private string _customerPhoneInput = string.Empty;

    [ObservableProperty] private string _customerEmailInput = string.Empty;

    [ObservableProperty] private string _amountReceivedInput = string.Empty;

    [ObservableProperty] private PaymentMethod _paymentMethod = PaymentMethod.Cash;

    [ObservableProperty] private string _subtotalText = string.Empty;

    [ObservableProperty] private string _taxText = string.Empty;

    [ObservableProperty] private string _totalText = string.Empty;

    [ObservableProperty] private string _changeText = string.Empty;

    [ObservableProperty] private SearchSuggestionViewModel? _selectedSuggestion;

    [ObservableProperty] private bool _isDropDownOpen;



    public ObservableCollection<SearchSuggestionViewModel> SearchSuggestions { get; } = new();

    public ObservableCollection<CartLineViewModel> Cart { get; } = new();



    public CheckoutViewModel(
        IProductService products,
        ISaleService sales,
        ISettingsService settings,
        IReceiptPrintService printer,
        IServiceScopeFactory scopeFactory)

    {

        _products = products;

        _sales = sales;

        _settings = settings;

        _printer = printer;

        _scopeFactory = scopeFactory;

        _ = InitializeAsync();

    }



    private async Task InitializeAsync()

    {

        await RunSafeAsync(async () =>

        {

            _config = await _settings.GetConfigAsync();

            await RefreshCatalogAsync();

            RecalculateTotals();

        });

    }



    partial void OnSearchQueryChanged(string value) => _ = DebouncedSearchAsync(value);



    partial void OnSelectedSuggestionChanged(SearchSuggestionViewModel? value)

    {

        if (value?.Product is null)

            return;



        AddToCart(value.Product);

        ClearSearch();

    }



    private decimal _amountReceived;



    public decimal AmountReceived

    {

        get => _amountReceived;

        private set => SetProperty(ref _amountReceived, value);

    }



    private decimal _discountAmount;



    public decimal DiscountAmount

    {

        get => _discountAmount;

        private set => SetProperty(ref _discountAmount, value);

    }



    partial void OnDiscountAmountInputChanged(string value)

    {

        if (string.IsNullOrWhiteSpace(value))

            DiscountAmount = 0;

        else if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)

                 || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))

            DiscountAmount = Math.Round(parsed, 0, MidpointRounding.AwayFromZero);

        else

            DiscountAmount = 0;



        RecalculateTotals();

    }



    partial void OnAmountReceivedInputChanged(string value)

    {

        if (string.IsNullOrWhiteSpace(value))

            AmountReceived = 0;

        else if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)

                 || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))

            AmountReceived = Math.Round(parsed, 0, MidpointRounding.AwayFromZero);

        else

            AmountReceived = 0;



        RecalculateTotals();

    }



    partial void OnPaymentMethodChanged(PaymentMethod value) => RecalculateTotals();



    [RelayCommand]

    private async Task DebouncedSearchAsync(string query)

    {

        _searchCts?.Cancel();

        _searchCts = new CancellationTokenSource();

        var token = _searchCts.Token;



        try

        {

            await Task.Delay(150, token);

            await UpdateSearchResultsAsync(query);

        }

        catch (OperationCanceledException)

        {

            // Expected when the user keeps typing.

        }

    }



    private async Task RefreshCatalogAsync()

    {

        try

        {

            var items = await _products.GetProductsAsync();

            _catalog.Clear();

            foreach (var p in items)

                _catalog[p.Id] = p;

        }

        catch (Exception ex)

        {

            StatusMessage = $"Could not load product catalog: {ex.Message}";

            throw;

        }

    }



    private async Task UpdateSearchResultsAsync(string query)

    {

        try

        {

            IsBusy = true;

            StatusMessage = null;



            var term = query?.Trim() ?? string.Empty;

            SearchSuggestions.Clear();



            if (string.IsNullOrWhiteSpace(term))

            {

                IsDropDownOpen = false;

                return;

            }



            var items = await _products.GetProductsAsync(term, null);

            foreach (var p in items.Take(20))

                SearchSuggestions.Add(new SearchSuggestionViewModel(p, _settings, _config));



            IsDropDownOpen = SearchSuggestions.Count > 0;

        }

        catch (Exception ex)

        {

            IsDropDownOpen = false;

            StatusMessage = ex.Message;

        }

        finally

        {

            IsBusy = false;

        }

    }



    [RelayCommand]

    private async Task SubmitSearchAsync()

    {

        if (string.IsNullOrWhiteSpace(SearchQuery))

            return;



        try

        {

            var trimmed = SearchQuery.Trim();



            var exact = await _products.GetByLookupCodeAsync(trimmed);

            if (exact is not null)

            {

                AddToCart(exact);

                ClearSearch();

                return;

            }



            if (SelectedSuggestion?.Product is Product selected)

            {

                AddToCart(selected);

                ClearSearch();

                return;

            }



            if (SearchSuggestions.FirstOrDefault()?.Product is Product first)

            {

                AddToCart(first);

                ClearSearch();

                return;

            }



            System.Windows.MessageBox.Show(

                $"No item found for \"{trimmed}\".",

                "Bhai Gee POS",

                System.Windows.MessageBoxButton.OK,

                System.Windows.MessageBoxImage.Information);

        }

        catch (Exception ex)

        {

            System.Windows.MessageBox.Show(

                $"Could not add item.\n\n{ex.Message}",

                "Bhai Gee POS — Billing",

                System.Windows.MessageBoxButton.OK,

                System.Windows.MessageBoxImage.Warning);

        }

    }



    [RelayCommand]

    private void AddToCart(Product? product)

    {

        if (product is null || product.Stock <= 0)

            return;



        if (!_catalog.TryGetValue(product.Id, out var catalogProduct))

            catalogProduct = product;



        if (catalogProduct.Stock <= 0)

            return;



        var existing = Cart.FirstOrDefault(c => c.ProductId == product.Id);

        if (existing is not null)

        {

            if (existing.Quantity >= catalogProduct.Stock)

                return;

            existing.Quantity++;

        }

        else

        {

            Cart.Add(CartLineViewModel.FromProduct(catalogProduct));

        }



        RecalculateTotals();

    }



    [RelayCommand]

    private void IncreaseQty(CartLineViewModel? item)

    {

        if (item is null)

            return;



        if (_catalog.TryGetValue(item.ProductId, out var product) && item.Quantity >= product.Stock)

            return;



        item.Quantity++;

        RecalculateTotals();

    }



    [RelayCommand]

    private void DecreaseQty(CartLineViewModel? item)

    {

        if (item is null)

            return;



        if (item.Quantity <= 1)

            Cart.Remove(item);

        else

            item.Quantity--;



        RecalculateTotals();

    }



    [RelayCommand]

    private void ClearCart()

    {

        Cart.Clear();

        DiscountAmountInput = string.Empty;

        DiscountAmount = 0;

        CustomerPhoneInput = string.Empty;

        CustomerEmailInput = string.Empty;

        AmountReceivedInput = string.Empty;

        AmountReceived = 0;

        RecalculateTotals();

    }



    [RelayCommand]

    private void SetPaymentMethod(string method)

    {

        PaymentMethod = method switch

        {

            "Card" => PaymentMethod.Card,

            "MobileWallet" => PaymentMethod.MobileWallet,

            _ => PaymentMethod.Cash

        };

    }



    [RelayCommand]

    private async Task CompleteSaleAsync()

    {

        await RunSafeAsync(async () =>

        {

            var cartItems = Cart.Select(c => c.ToModel()).ToList();

            var sale = await _sales.CompleteSaleAsync(cartItems, DiscountAmount, _config.TaxRate, PaymentMethod, AmountReceived, CustomerPhoneInput, CustomerEmailInput);

            try { _printer.PrintReceipt(sale, _config); }

            catch { /* printer optional */ }

            var receiptText = _printer.BuildReceiptText(sale, _config);

            if (!string.IsNullOrWhiteSpace(sale.CustomerPhone) || !string.IsNullOrWhiteSpace(sale.CustomerEmail))
            {
                var saleCopy = sale;
                var configCopy = _config;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var notifications = scope.ServiceProvider.GetRequiredService<ICustomerNotificationService>();
                        await notifications.SendCheckoutThankYouAsync(saleCopy, configCopy, receiptText);
                    }
                    catch
                    {
                        // Notifications are optional.
                    }
                });
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var cloudSync = scope.ServiceProvider.GetRequiredService<ICloudSyncService>();
                    await cloudSync.SyncAsync();
                }
                catch
                {
                    // Cloud sync retries from background worker.
                }
            });

            System.Windows.MessageBox.Show(receiptText, $"Receipt — {sale.ReceiptNo}");

            Cart.Clear();

            DiscountAmountInput = string.Empty;

            DiscountAmount = 0;

            AmountReceivedInput = string.Empty;

            AmountReceived = 0;

            await RefreshCatalogAsync();

            RecalculateTotals();

        });

    }



    public void ClearSearch()

    {

        SearchQuery = string.Empty;

        SelectedSuggestion = null;

        IsDropDownOpen = false;

        SearchSuggestions.Clear();

    }



    private void RecalculateTotals()

    {

        var totals = _sales.CalculateTotals(Cart.Select(c => c.ToModel()), DiscountAmount, _config.TaxRate);

        SubtotalText = _settings.FormatMoney(totals.Subtotal, _config);

        TaxText = _settings.FormatMoney(totals.TaxAmount, _config);

        TotalText = _settings.FormatMoney(totals.Total, _config);

        var change = PaymentMethod == PaymentMethod.Cash

            ? AmountReceived - totals.Total

            : 0;

        ChangeText = _settings.FormatMoney(change, _config);

    }

}

