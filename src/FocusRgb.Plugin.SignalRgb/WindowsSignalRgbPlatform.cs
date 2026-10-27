using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FocusRgb.Plugin.SignalRgb
{
    internal sealed partial class WindowsSignalRgbPlatform : ISignalRgbPlatform
    {
        private const string BuiltInPrefix = "builtin:";
        private const string CatalogPrefix = "catalog:";
        private readonly string _localApplicationData;

        public WindowsSignalRgbPlatform()
            : this(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
        {
        }

        internal WindowsSignalRgbPlatform(string localApplicationData) =>
            _localApplicationData = localApplicationData;

        public bool IsProviderRunning()
        {
            var processes = Process.GetProcessesByName("SignalRgb");
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (var process in processes) process.Dispose();
            }
        }

        public string? FindLauncher()
        {
            var path = Path.Combine(_localApplicationData, "VortxEngine", "SignalRgbLauncher.exe");
            return File.Exists(path) ? path : null;
        }

        public IReadOnlyList<SignalRgbEffect> ListEffects()
        {
            var effects = new List<SignalRgbEffect>();
            AddBuiltInEffects(effects);
            AddCachedEffects(effects);
            return effects
                .DistinctBy(effect => effect.ResourceId, StringComparer.Ordinal)
                .OrderBy(effect => effect.DisplayName, StringComparer.Ordinal)
                .ThenBy(effect => effect.ResourceId, StringComparer.Ordinal)
                .ToArray();
        }

        public void SubmitEffect(string launcherPath, string applicationUrl)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = launcherPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add($"--url={applicationUrl}");
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The SignalRGB launcher did not start.");
        }

        private void AddBuiltInEffects(List<SignalRgbEffect> effects)
        {
            var application = FindLatestApplicationDirectory();
            if (application is null) return;
            var root = Path.Combine(application, "Signal-x64", "Effects");
            if (!Directory.Exists(root)) return;
            foreach (var file in Directory.EnumerateFiles(root, "*.html", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                effects.Add(new SignalRgbEffect(
                    BuiltInPrefix + Uri.EscapeDataString(relative),
                    ReadHtmlTitle(file) ?? Path.GetFileNameWithoutExtension(file),
                    true));
            }
        }

        private void AddCachedEffects(List<SignalRgbEffect> effects)
        {
            var cache = Path.Combine(_localApplicationData, "WhirlwindFX", "SignalRgb", "cache");
            var effectsRoot = Path.Combine(cache, "effects");
            if (!Directory.Exists(effectsRoot)) return;
            var names = ReadCatalogNames(Path.Combine(cache, "catalog_en.json"));
            foreach (var directory in Directory.EnumerateDirectories(effectsRoot))
            {
                var id = Path.GetFileName(directory);
                var effectFile = Path.Combine(directory, "effect.html");
                var name = names.GetValueOrDefault(id)
                    ?? (File.Exists(effectFile) ? ReadHtmlTitle(effectFile) : null)
                    ?? id;
                effects.Add(new SignalRgbEffect(
                    CatalogPrefix + Uri.EscapeDataString(id),
                    name,
                    File.Exists(effectFile),
                    "The catalog item has not been downloaded."));
            }
        }

        private string? FindLatestApplicationDirectory()
        {
            var root = Path.Combine(_localApplicationData, "VortxEngine");
            if (!Directory.Exists(root)) return null;
            return Directory.EnumerateDirectories(root, "app-*", SearchOption.TopDirectoryOnly)
                .Select(path => new { Path = path, Parsed = Version.TryParse(Path.GetFileName(path)[4..], out var version), Version = version })
                .Where(candidate => candidate.Parsed)
                .OrderByDescending(candidate => candidate.Version)
                .Select(candidate => candidate.Path)
                .FirstOrDefault();
        }

        private static string? ReadHtmlTitle(string path)
        {
            try
            {
                var match = TitlePattern().Match(File.ReadAllText(path));
                return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static Dictionary<string, string> ReadCatalogNames(string path)
        {
            if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var names = new Dictionary<string, string>(StringComparer.Ordinal);
                CollectCatalogNames(document.RootElement, names);
                return names;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        private static void CollectCatalogNames(JsonElement element, Dictionary<string, string> names)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var id = ReadString(element, "_id") ?? ReadString(element, "id");
                var name = ReadString(element, "name") ?? ReadString(element, "title");
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name)) names.TryAdd(id, name);
                foreach (var property in element.EnumerateObject()) CollectCatalogNames(property.Value, names);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray()) CollectCatalogNames(item, names);
            }
        }

        private static string? ReadString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        [GeneratedRegex("<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex TitlePattern();
    }
}
