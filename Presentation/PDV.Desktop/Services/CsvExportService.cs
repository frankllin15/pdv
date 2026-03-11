using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace PDV.Desktop.Services;

public record CsvColumnDefinition<T>(string Header, Func<T, string> ValueSelector);

public interface ICsvExportService
{
    byte[] GenerateCsv<T>(IEnumerable<T> data, CsvColumnDefinition<T>[] columns);
    Task<string?> ExportToCsvAsync<T>(IEnumerable<T> data, CsvColumnDefinition<T>[] columns, string suggestedFileName);
}

public class CsvExportService : ICsvExportService
{
    private const char Separator = ';';

    public byte[] GenerateCsv<T>(IEnumerable<T> data, CsvColumnDefinition<T>[] columns)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine(string.Join(Separator, columns.Select(c => EscapeField(c.Header))));

        // Data rows
        foreach (var item in data)
        {
            sb.AppendLine(string.Join(Separator, columns.Select(c => EscapeField(c.ValueSelector(item)))));
        }

        // UTF-8 with BOM for Excel compatibility
        var bom = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + content.Length];
        bom.CopyTo(result, 0);
        content.CopyTo(result, bom.Length);
        return result;
    }

    public async Task<string?> ExportToCsvAsync<T>(
        IEnumerable<T> data,
        CsvColumnDefinition<T>[] columns,
        string suggestedFileName)
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar CSV",
            SuggestedFileName = suggestedFileName,
            FileTypeChoices =
            [
                new FilePickerFileType("CSV") { Patterns = ["*.csv"] }
            ],
            DefaultExtension = "csv"
        });

        if (file == null) return null;

        var bytes = GenerateCsv(data, columns);
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(bytes);

        return file.Name;
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    private static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(Separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
