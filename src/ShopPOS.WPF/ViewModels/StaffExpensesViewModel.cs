using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Models;
using ShopPOS.Domain.Entities;
using ShopPOS.WPF.Helpers;
using ShopPOS.WPF.Services.Reports;
using ShopPOS.WPF.Windows;

namespace ShopPOS.WPF.ViewModels;

public partial class StaffExpensesViewModel : ViewModelBase, IDisposable
{
    private readonly IWorkerService _workers;
    private readonly IAttendanceService _attendance;
    private readonly IExpenseAndCashService _expenses;
    private readonly IFingerprintScannerService _fingerprint;
    private readonly ISettingsService _settings;
    private readonly IWorkerProfileService _workerProfile;
    private readonly ILedgerPdfReportService _pdfReports;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _toastCts;

    [ObservableProperty] private Worker? _selectedWorker;

    [ObservableProperty] private string _newWorkerName = string.Empty;
    [ObservableProperty] private string _newWorkerPhone = string.Empty;
    [ObservableProperty] private string _newWorkerRole = "Sales Staff";
    [ObservableProperty] private decimal? _newWorkerSalary;
    [ObservableProperty] private string _fingerprintStatus = "No thumbprint registered";
    [ObservableProperty] private string? _pendingFingerprintTemplate;

    [ObservableProperty] private DateTime? _expenseDate = DateTime.Today;
    [ObservableProperty] private decimal? _dailyCashIntake;
    [ObservableProperty] private decimal? _totalExpense;
    [ObservableProperty] private string _expenseDescription = string.Empty;

    [ObservableProperty] private string _toastMessage = string.Empty;
    [ObservableProperty] private bool _isToastVisible;
    [ObservableProperty] private string _scannerStatus = "Scanner idle";

    public ObservableCollection<Worker> Workers { get; } = new();
    public ObservableCollection<Attendance> TodayAttendance { get; } = new();
    public ObservableCollection<ExpenseAndCash> ExpenseEntries { get; } = new();
    public ObservableCollection<Worker> CheckedInWorkers { get; } = new();

    public StaffExpensesViewModel(
        IWorkerService workers,
        IAttendanceService attendance,
        IExpenseAndCashService expenses,
        IFingerprintScannerService fingerprint,
        ISettingsService settings,
        IWorkerProfileService workerProfile,
        ILedgerPdfReportService pdfReports,
        IServiceScopeFactory scopeFactory)
    {
        _workers = workers;
        _attendance = attendance;
        _expenses = expenses;
        _fingerprint = fingerprint;
        _settings = settings;
        _workerProfile = workerProfile;
        _pdfReports = pdfReports;
        _scopeFactory = scopeFactory;
        _fingerprint.FingerprintScanned += OnFingerprintScanned;
        _ = LoadAsync();
    }

    public void StartScanner()
    {
        _fingerprint.StartListening();
        ScannerStatus = _fingerprint.IsDeviceConnected
            ? "USB scanner listening for attendance..."
            : "Simulation mode — place thumb when prompted during enrollment; attendance uses registered templates.";
    }

    public void StopScanner()
    {
        _fingerprint.StopListening();
        ScannerStatus = "Scanner stopped";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var workers = await _workers.GetWorkersAsync();
            Workers.Clear();
            foreach (var w in workers) Workers.Add(w);

            var records = await _attendance.GetTodayRecordsAsync();
            TodayAttendance.Clear();
            CheckedInWorkers.Clear();
            foreach (var r in records)
            {
                TodayAttendance.Add(r);
                if (r.TimeToLeave is null && r.Worker is not null)
                    CheckedInWorkers.Add(r.Worker);
            }

            var entries = await _expenses.GetEntriesAsync();
            ExpenseEntries.Clear();
            foreach (var e in entries.Take(30)) ExpenseEntries.Add(e);
        });
    }

    [RelayCommand]
    private void RegisterThumbprint()
    {
        if (WindowNavigationHelper.ActivateIfOpen<FingerprintEnrollDialog>())
            return;

        var dialog = CreateScopedWindow<FingerprintEnrollDialog>();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.CapturedTemplate))
        {
            PendingFingerprintTemplate = dialog.CapturedTemplate;
            FingerprintStatus = "Thumbprint ready — save worker to commit";
        }
    }

    [RelayCommand]
    private async Task AddWorkerAsync()
    {
        await RunSafeAsync(async () =>
        {
            await _workers.AddWorkerAsync(new Worker
            {
                Name = NewWorkerName.Trim(),
                Phone = NewWorkerPhone.Trim(),
                Role = string.IsNullOrWhiteSpace(NewWorkerRole) ? "Staff" : NewWorkerRole.Trim(),
                MonthlySalary = NewWorkerSalary ?? 0,
                FingerprintTemplate = PendingFingerprintTemplate
            });

            NewWorkerName = string.Empty;
            NewWorkerPhone = string.Empty;
            NewWorkerRole = "Sales Staff";
            NewWorkerSalary = null;
            PendingFingerprintTemplate = null;
            FingerprintStatus = "No thumbprint registered";
            await LoadAsync();
        }, "Worker added successfully.");
    }

    [RelayCommand]
    private async Task MarkAttendanceAsync(Worker? worker)
    {
        if (worker is null) return;
        await RunSafeAsync(async () =>
        {
            var result = await _attendance.MarkByWorkerIdAsync(worker.Id);
            if (result.Success)
                await ShowToastAsync(result.Message);
            else
                StatusMessage = result.Message;
            await LoadAsync();
        });
    }

    [RelayCommand]
    private void OpenWorkerProfile(Worker? worker)
    {
        if (worker is null) return;

        if (WindowNavigationHelper.ActivateIfOpen<WorkerProfileWindow>(existing => existing.Initialize(worker.Id)))
            return;

        var win = CreateScopedWindow<WorkerProfileWindow>();
        win.Initialize(worker.Id);
        win.Owner = System.Windows.Application.Current.MainWindow;
        win.ShowDialog();
    }

    [RelayCommand]
    private async Task PrintEmployeeReportPdfAsync(Worker? worker)
    {
        var target = worker ?? SelectedWorker;
        if (target is null)
        {
            System.Windows.MessageBox.Show(
                "Please select a worker from the Active Staff list before printing the employee report.",
                "Worker Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var year = DateTime.Today.Year;
        var month = DateTime.Today.Month;
        var safeName = target.Name.Replace(' ', '_');
        var path = PdfReportExportHelper.PromptSavePath($"EmployeeReport_{safeName}_{year}-{month:D2}.pdf");
        if (path is null)
            return;

        await RunSafeAsync(async () =>
        {
            var report = await WorkerReportDataBuilder.BuildAsync(_workerProfile, _settings, target.Id, year, month)
                ?? throw new InvalidOperationException("Worker not found.");

            _pdfReports.GenerateWorkerReport(report, path);

            if (PdfReportExportHelper.ConfirmOpenAfterSave())
                PdfReportExportHelper.OpenPdf(path);
        });
    }

    [RelayCommand]
    private void OpenOwnerExpenses()
    {
        if (WindowNavigationHelper.ActivateIfOpen<OwnerExpensesWindow>())
            return;

        var win = CreateScopedWindow<OwnerExpensesWindow>();
        win.Owner = System.Windows.Application.Current.MainWindow;
        win.ShowDialog();
    }

    [RelayCommand]
    private async Task CheckOutWorkerAsync(Worker? worker)
    {
        if (worker is null) return;
        await RunSafeAsync(async () =>
        {
            var result = await _attendance.MarkByWorkerIdAsync(worker.Id);
            if (result.Success)
                await ShowToastAsync(result.Message);
            else
                StatusMessage = result.Message;
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task DeleteWorkerAsync(Worker? worker)
    {
        if (worker is null) return;
        if (System.Windows.MessageBox.Show(
                $"Deactivate {worker.Name}? They will be hidden from active staff lists.",
                "Confirm",
                System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
            return;

        await RunSafeAsync(async () =>
        {
            await _workers.SoftDeleteWorkerAsync(worker.Id);
            await LoadAsync();
        }, "Worker deactivated.");
    }

    [RelayCommand]
    private async Task SaveExpenseAsync()
    {
        if (!ExpenseDate.HasValue)
            return;

        await RunSafeAsync(async () =>
        {
            await _expenses.SaveEntryAsync(
                ExpenseDate.Value,
                DailyCashIntake ?? 0,
                TotalExpense ?? 0,
                ExpenseDescription);

            ExpenseDescription = string.Empty;
            DailyCashIntake = null;
            TotalExpense = null;
            await LoadAsync();
        }, "Daily cash & expense saved.");
    }

    public string FormatMoney(decimal amount) => _settings.FormatMoney(amount);

    private async void OnFingerprintScanned(object? sender, FingerprintScanEventArgs e)
    {
        var template = e.TemplateBase64;
        AttendanceMarkResult result;

        if (e.MatchedWorkerId is int workerId)
            result = await _attendance.MarkByWorkerIdAsync(workerId);
        else
            result = await _attendance.MarkByFingerprintAsync(template);

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (result.Success)
                await ShowToastAsync(result.Message);
            else
                StatusMessage = result.Message;

            await LoadAsync();
        });
    }

    private async Task ShowToastAsync(string message)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        ToastMessage = message;
        IsToastVisible = true;

        try
        {
            await Task.Delay(3500, token);
            IsToastVisible = false;
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _fingerprint.FingerprintScanned -= OnFingerprintScanned;
        StopScanner();
        _toastCts?.Cancel();
        _toastCts?.Dispose();
    }

    private TWindow CreateScopedWindow<TWindow>() where TWindow : Window
    {
        var scope = _scopeFactory.CreateScope();
        var window = scope.ServiceProvider.GetRequiredService<TWindow>();
        window.Closed += (_, _) => scope.Dispose();
        return window;
    }
}
