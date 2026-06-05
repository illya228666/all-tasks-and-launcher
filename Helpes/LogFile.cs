using System.Globalization;

namespace Helpers
{
    public static class LogFile
    {
        public static void AppendTimestampedLine(string path, string payload, CultureInfo culture)
        {
            EnsureDirectory(path);

            string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", culture);

            using StreamWriter writer = new(path, true);
            writer.WriteLine($"{timestamp} - {payload}");
        }

        public static List<string> ReadLines(string path)
        {
            if (!File.Exists(path))
            {
                return new List<string>();
            }

            return File.ReadAllLines(path).ToList();
        }

        public static List<string> ReadLinesByDate(string path, string datePrefix)
        {
            return ReadLines(path)
                .Where(line => line.Length >= 10 && line.Substring(0, 10) == datePrefix)
                .ToList();
        }

        private static void EnsureDirectory(string path)
        {
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
