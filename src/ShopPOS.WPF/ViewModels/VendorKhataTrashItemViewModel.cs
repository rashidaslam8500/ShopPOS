using CommunityToolkit.Mvvm.ComponentModel;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.ViewModels;

public partial class VendorKhataTrashItemViewModel : ObservableObject
{
    public VendorKhataLine Line { get; }

    [ObservableProperty] private bool _isSelected;

    public VendorKhataTrashItemViewModel(VendorKhataLine line) => Line = line;

    public int Id => Line.Id;
    public DateTime? DeletedAt => Line.DeletedAt;
    public DateTime Date => Line.Date;
    public string InvoiceNumber => Line.InvoiceNumber;
    public decimal TotalBill => Line.TotalBill;
    public decimal CashPaid => Line.CashPaid;
    public string? Description => Line.Description;
}
