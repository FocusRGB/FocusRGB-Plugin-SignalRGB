using System.Text.Json;
using FocusRgb.Contracts;

namespace FocusRgb.Plugin.SignalRgb.Tests
{
    public sealed class PluginManifestTests
    {
        [Fact]
        public void ContractVersion_MatchesSdkCurrentVersion()
        {
            using JsonDocument manifest = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "focusrgb-plugin.json")));

            Assert.Equal(
                ContractVersion.Current,
                manifest.RootElement.GetProperty("contractVersion").GetString());
        }
    }
}
