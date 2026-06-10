namespace FengZhi.Foundation.Data;

/// <summary>
/// 数据加载异常。包含文件路径和行号以便快速定位 YAML 错误。
/// </summary>
public sealed class DataLoadException : Exception
{
    public string FilePath { get; }
    public int? LineNumber { get; }

    public DataLoadException(string filePath, int? lineNumber, string message, Exception? inner = null)
        : base(FormatMessage(filePath, lineNumber, message), inner)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    private static string FormatMessage(string filePath, int? lineNumber, string message)
    {
        var location = lineNumber.HasValue
            ? $"{filePath}:{lineNumber.Value}"
            : filePath;
        return $"[DataLoad] {location} — {message}";
    }
}
