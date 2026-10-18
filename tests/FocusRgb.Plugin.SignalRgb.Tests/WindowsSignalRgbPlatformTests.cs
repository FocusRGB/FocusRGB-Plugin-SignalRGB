using System.Text.Json;

namespace FocusRgb.Plugin.SignalRgb.Tests
{
    public sealed class WindowsSignalRgbPlatformTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), $"focusrgb-signalrgb-{Guid.NewGuid():N}");

        [Fact]
        public void ListEffects_UsesLatestSemanticInstall_AndDownloadedCatalogNames()
        {
            WriteEffect("app-2.5.9", "Old.html", "Old");
            WriteEffect("app-2.5.80", "Dynamic/Screen Ambience.html", "Screen Ambience");
            var cache = Path.Combine(_root, "WhirlwindFX", "SignalRgb", "cache");
            Directory.CreateDirectory(Path.Combine(cache, "effects", "effect-id"));
            File.WriteAllText(Path.Combine(cache, "effects", "effect-id", "effect.html"), "<title>Wrong fallback</title>");
            File.WriteAllText(Path.Combine(cache, "catalog_en.json"), JsonSerializer.Serialize(new
            {
                effects = new[] { new { _id = "effect-id", name = "Aurora" } }
            }));

            var effects = new WindowsSignalRgbPlatform(_root).ListEffects();

            Assert.DoesNotContain(effects, item => item.DisplayName == "Old");
            Assert.Contains(effects, item => item.DisplayName == "Screen Ambience" && item.ResourceId.StartsWith("builtin:", StringComparison.Ordinal));
            Assert.Contains(effects, item => item.DisplayName == "Aurora" && item.ResourceId == "catalog:effect-id" && item.Available);
        }

        [Fact]
        public void ListEffects_MarksUndownloadedCacheEntryUnavailable()
        {
            var cache = Path.Combine(_root, "WhirlwindFX", "SignalRgb", "cache");
            Directory.CreateDirectory(Path.Combine(cache, "effects", "not-downloaded"));
            File.WriteAllText(Path.Combine(cache, "catalog_en.json"),
                "{\"items\":[{\"id\":\"not-downloaded\",\"title\":\"Paid Effect\"}]}");

            var effect = Assert.Single(new WindowsSignalRgbPlatform(_root).ListEffects());

            Assert.Equal("Paid Effect", effect.DisplayName);
            Assert.False(effect.Available);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        private void WriteEffect(string version, string relativePath, string title)
        {
            var path = Path.Combine(_root, "VortxEngine", version, "Signal-x64", "Effects",
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"<html><title>{title}</title></html>");
        }
    }
}
