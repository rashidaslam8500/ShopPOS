using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace ShopPOS.WPF.Services.Reports;

public static class PdfReportExportHelper
{
    public static string? PromptSavePath(string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save PDF Report",
            Filter = "PDF Document (*.pdf)|*.pdf",
            DefaultExt = "pdf",
            FileName = SanitizeFileName(defaultFileName),
            AddExtension = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public static void OpenPdf(string filePath)
    {
        Process.Start(new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        });
    }

    public static bool ConfirmOpenAfterSave()
    {
        return System.Windows.MessageBox.Show(
            "PDF saved successfully.\n\nWould you like to open the file now?",
            "Export Complete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.Yes;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
