namespace ShopPOS.WPF.Services;

using System.IO;

public interface IVendorBillStorageService
{
    bool FileExists(string? attachmentPath);
    bool IsImageFile(string path);
    bool IsPdfFile(string path);
    Task<string> ImportBillAsync(int vendorId, int entryId, string sourceFilePath);
    void TryDeleteFile(string? attachmentPath);
}

public class VendorBillStorageService : IVendorBillStorageService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png" };

    public string StorageRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "KitchenMart",
            "VendorBills");

    public bool FileExists(string? attachmentPath) =>
        !string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath);

    public bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ImageExtensions.Contains(ext);
    }

    public bool IsPdfFile(string path) =>
        string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ImportBillAsync(int vendorId, int entryId, string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("The selected file could not be found.", sourceFilePath);

        var ext = Path.GetExtension(sourceFilePath);
        if (!ImageExtensions.Contains(ext) && !IsPdfFile(sourceFilePath))
            throw new InvalidOperationException("Only JPG, JPEG, PNG, and PDF files are supported.");

        Directory.CreateDirectory(StorageRoot);
        var fileName = $"vendor{vendorId}_entry{entryId}_{DateTime.Now:yyyyMMdd_HHmmssfff}{ext}";
        var destination = Path.Combine(StorageRoot, fileName);
        await Task.Run(() => File.Copy(sourceFilePath, destination, overwrite: false));
        return destination;
    }

    public void TryDeleteFile(string? attachmentPath)
    {
        if (string.IsNullOrWhiteSpace(attachmentPath) || !File.Exists(attachmentPath))
            return;

        try { File.Delete(attachmentPath); }
        catch { /* best-effort cleanup */ }
    }
}
