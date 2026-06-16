namespace ShopPOS.Domain.Models;

public class AttendanceMarkResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int WorkerId { get; init; }
    public string WorkerName { get; init; } = string.Empty;
    public bool IsTimeIn { get; init; }
}

public class FingerprintCaptureResult
{
    public bool Success { get; init; }
    public string? TemplateBase64 { get; init; }
    public string Message { get; init; } = string.Empty;
}
