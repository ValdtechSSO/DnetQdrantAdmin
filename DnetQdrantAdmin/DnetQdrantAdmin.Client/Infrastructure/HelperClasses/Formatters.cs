namespace Dnet.QdrantAdmin.Client.Infrastructure.HelperClasses;

public static class Formatters
{
    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";

        double value = bytes;

        foreach (var unit in new[] { "KB", "MB", "GB", "TB" })
        {
            value /= 1024;

            if (value < 1024 || unit == "TB")
            {
                return $"{value:0.##} {unit}";
            }
        }

        return $"{bytes} B";
    }
}
