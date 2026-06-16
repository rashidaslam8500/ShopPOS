using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;

namespace ShopPOS.WPF.ViewModels;

public partial class AuditLogItemViewModel : ObservableObject
{
    public long Id { get; init; }
    public DateTime Timestamp { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;

    [ObservableProperty] private bool _isSelected;

    public static AuditLogItemViewModel FromEntity(AuditLog log) => new()
    {
        Id = log.Id,
        Timestamp = log.Timestamp,
        Username = log.Username,
        Action = log.Action.ToString(),
        EntityType = log.EntityType,
        Details = log.Details
    };
}

public partial class AuditLogsViewModel : ViewModelBase
{
    private readonly IAuditService _audit;

    [ObservableProperty] private bool _selectAll;

    public ObservableCollection<AuditLogItemViewModel> Logs { get; } = new();

    public AuditLogsViewModel(IAuditService audit)
    {
        _audit = audit;
        _ = LoadAsync();
    }

    partial void OnSelectAllChanged(bool value)
    {
        foreach (var item in Logs)
            item.IsSelected = value;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var logs = await _audit.GetRecentAsync(300);
            Logs.Clear();
            foreach (var log in logs)
                Logs.Add(AuditLogItemViewModel.FromEntity(log));
            SelectAll = false;
        });
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var ids = Logs.Where(l => l.IsSelected).Select(l => l.Id).ToList();
        if (ids.Count == 0)
        {
            System.Windows.MessageBox.Show("Select at least one audit log entry.", "Audit Trail");
            return;
        }

        var dialog = new Windows.OwnerPasswordDialog(
            "Audit Log Cleanup Authorization",
            "Enter the owner account password to permanently delete the selected audit log entries.",
            "Confirm Delete");

        if (dialog.ShowDialog() != true)
            return;

        await RunSafeAsync(async () =>
        {
            await _audit.DeleteLogsAsync(ids, dialog.Password);
            await LoadAsync();
        }, $"{ids.Count} audit log entr{(ids.Count == 1 ? "y" : "ies")} deleted.");
    }

    [RelayCommand]
    private async Task DeleteSingleAsync(AuditLogItemViewModel? item)
    {
        if (item is null)
            return;

        var dialog = new Windows.OwnerPasswordDialog(
            "Audit Log Cleanup Authorization",
            "Enter the owner account password to permanently delete this audit log entry.",
            "Confirm Delete");

        if (dialog.ShowDialog() != true)
            return;

        await RunSafeAsync(async () =>
        {
            await _audit.DeleteLogsAsync([item.Id], dialog.Password);
            await LoadAsync();
        }, "Audit log entry deleted.");
    }
}
