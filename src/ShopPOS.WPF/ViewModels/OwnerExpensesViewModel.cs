using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;

namespace ShopPOS.WPF.ViewModels;

public partial class OwnerExpensesViewModel : ViewModelBase
{
    private readonly IOwnerPersonalExpenseService _expenses;
    private readonly ISettingsService _settings;

    [ObservableProperty] private DateTime? _expenseDate = DateTime.Today;
    [ObservableProperty] private decimal? _expenseAmount;
    [ObservableProperty] private OwnerExpenseCategory _selectedCategory = OwnerExpenseCategory.Personal;
    [ObservableProperty] private string _expenseDescription = string.Empty;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private DateTime? _filterFrom;
    [ObservableProperty] private DateTime? _filterTo;

    public ObservableCollection<OwnerPersonalExpense> Items { get; } = new();
    public Array Categories => Enum.GetValues(typeof(OwnerExpenseCategory));

    public OwnerExpensesViewModel(IOwnerPersonalExpenseService expenses, ISettingsService settings)
    {
        _expenses = expenses;
        _settings = settings;
        _ = SearchAsync();
    }

    partial void OnSearchQueryChanged(string value) => _ = SearchAsync();
    partial void OnFilterFromChanged(DateTime? value) => _ = SearchAsync();
    partial void OnFilterToChanged(DateTime? value) => _ = SearchAsync();

    [RelayCommand]
    private async Task SearchAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = await _expenses.SearchAsync(
                string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                FilterFrom,
                FilterTo);
            Items.Clear();
            foreach (var e in list) Items.Add(e);
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ExpenseDate.HasValue || string.IsNullOrWhiteSpace(ExpenseDescription)) return;
        await RunSafeAsync(async () =>
        {
            await _expenses.AddAsync(ExpenseDate.Value, ExpenseAmount ?? 0, SelectedCategory, ExpenseDescription);
            ExpenseAmount = null;
            ExpenseDescription = string.Empty;
            await SearchAsync();
        }, "Owner expense saved.");
    }

    public string FormatMoney(decimal amount) => _settings.FormatMoney(amount);
}
