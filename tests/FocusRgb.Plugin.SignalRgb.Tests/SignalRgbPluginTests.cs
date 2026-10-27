using System.Text.Json;
using FocusRgb.Contracts;

namespace FocusRgb.Plugin.SignalRgb.Tests
{
    public sealed class SignalRgbPluginTests
    {
        [Fact]
        public async Task Initialize_ReportsManifestVersionAndVerifiedCapabilityLimits()
        {
            var result = await Plugin().InitializeAsync(Initialize(), TestContext.Current.CancellationToken);
            using var manifest = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "focusrgb-plugin.json")));

            Assert.Equal("signalrgb", result.PluginId);
            Assert.Equal(manifest.RootElement.GetProperty("version").GetString(), result.PluginVersion);
            Assert.Equal(ContractVersion.Current, result.ContractVersion);
            Assert.Equal("unverified", result.Capabilities.Single(item => item.Id == "provider.effect.select").Support);
            Assert.Equal("unsupported", result.Capabilities.Single(item => item.Id == "provider.layout.select").Support);
        }

        [Fact]
        public async Task Status_ReturnsUnavailable_WithoutStartingProvider()
        {
            var platform = new FakePlatform { Running = false };

            var result = await Plugin(platform).GetStatusAsync(TestContext.Current.CancellationToken);

            Assert.Equal("unavailable", result.Availability);
            Assert.Equal("provider-not-running", result.Reason?.Code);
            Assert.Empty(platform.Submissions);
        }

        [Fact]
        public async Task Apply_EncodesEffectName_AndReturnsUnverified()
        {
            var platform = new FakePlatform
            {
                Effects = [new SignalRgbEffect("catalog:one", "Aurora & Snow/Fire", true)]
            };

            var result = await Plugin(platform).ApplyBindingAsync(
                Apply(new PluginResourceReference
                {
                    ResourceId = "catalog:one",
                    DisplayName = "Aurora & Snow/Fire"
                }),
                TestContext.Current.CancellationToken);

            Assert.Equal("unverified", result.Outcome);
            var submission = Assert.Single(platform.Submissions);
            Assert.Equal("signalrgb://effect/apply/Aurora%20%26%20Snow%2FFire?-silentlaunch-", submission.Url);
        }

        [Fact]
        public async Task Apply_DoesNotSubmit_WhenProviderIsStopped()
        {
            var platform = new FakePlatform
            {
                Running = false,
                Effects = [new SignalRgbEffect("catalog:one", "Aurora", true)]
            };

            var result = await Plugin(platform).ApplyBindingAsync(
                Apply(new PluginResourceReference { ResourceId = "catalog:one", DisplayName = "Aurora" }),
                TestContext.Current.CancellationToken);

            Assert.Equal("unavailable", result.Outcome);
            Assert.Equal("provider-not-running", result.Reason?.Code);
            Assert.Empty(platform.Submissions);
        }

        [Fact]
        public async Task Apply_RejectsMismatchedCatalogIdentity()
        {
            var platform = new FakePlatform
            {
                Effects = [new SignalRgbEffect("catalog:one", "Aurora", true)]
            };

            var result = await Plugin(platform).ApplyBindingAsync(
                Apply(new PluginResourceReference { ResourceId = "catalog:one", DisplayName = "aurora" }),
                TestContext.Current.CancellationToken);

            Assert.Equal("unavailable", result.Outcome);
            Assert.Equal("resource-not-found", result.Reason?.Code);
            Assert.Empty(platform.Submissions);
        }

        [Fact]
        public async Task Apply_ReturnsUnsupported_WhenLayoutIsRequested()
        {
            var parameters = Apply(new PluginResourceReference { ResourceId = "catalog:one", DisplayName = "Aurora" });
            parameters = new BindingApplyParameters
            {
                Profile = parameters.Profile,
                BindingId = parameters.BindingId,
                RenderingAuthority = parameters.RenderingAuthority,
                DeviceScope = parameters.DeviceScope,
                Effect = parameters.Effect,
                Layout = new PluginResourceReference { ResourceId = "desk", DisplayName = "Desk" }
            };

            var result = await Plugin().ApplyBindingAsync(parameters, TestContext.Current.CancellationToken);

            Assert.Equal("unsupported", result.Outcome);
            Assert.Equal("capability-unsupported", result.Reason?.Code);
        }

        [Fact]
        public async Task Apply_ReportsLauncherFailure()
        {
            var platform = new FakePlatform
            {
                Effects = [new SignalRgbEffect("catalog:one", "Aurora", true)],
                ThrowOnSubmit = true
            };

            var result = await Plugin(platform).ApplyBindingAsync(
                Apply(new PluginResourceReference { ResourceId = "catalog:one", DisplayName = "Aurora" }),
                TestContext.Current.CancellationToken);

            Assert.Equal("unavailable", result.Outcome);
            Assert.Equal("provider-not-running", result.Reason?.Code);
        }

        [Fact]
        public async Task Catalog_ReturnsEffects_AndRejectsLayouts()
        {
            var platform = new FakePlatform
            {
                Effects =
                [
                    new SignalRgbEffect("builtin:one", "One", true),
                    new SignalRgbEffect("catalog:two", "Two", false, "Not downloaded.")
                ]
            };
            var plugin = Plugin(platform);

            var effects = await plugin.ListCatalogAsync(
                new CatalogListParameters { Kind = "effect" }, TestContext.Current.CancellationToken);
            var layouts = await plugin.ListCatalogAsync(
                new CatalogListParameters { Kind = "layout" }, TestContext.Current.CancellationToken);

            Assert.Equal("supported", effects.Support);
            Assert.Equal(2, effects.Items.Count);
            Assert.False(effects.Items.Single(item => item.ResourceId == "catalog:two").Available);
            Assert.Equal("unsupported", layouts.Support);
        }

        private static SignalRgbPlugin Plugin(FakePlatform? platform = null) => new(platform ?? new FakePlatform());

        private static InitializeParameters Initialize() => new()
        {
            ContractVersion = ContractVersion.Current,
            Host = new PluginHostInfo { Name = "tests", Version = "1.0.0" }
        };

        private static BindingApplyParameters Apply(PluginResourceReference effect) => new()
        {
            Profile = "default",
            BindingId = "main",
            RenderingAuthority = "provider",
            DeviceScope = new PluginDeviceScope { Kind = "provider-managed" },
            Effect = effect
        };

        private sealed class FakePlatform : ISignalRgbPlatform
        {
            public bool Running { get; init; } = true;
            public IReadOnlyList<SignalRgbEffect> Effects { get; init; } = [];
            public bool ThrowOnSubmit { get; init; }
            public List<(string Launcher, string Url)> Submissions { get; } = [];

            public bool IsProviderRunning() => Running;
            public string? FindLauncher() => "SignalRgbLauncher.exe";
            public IReadOnlyList<SignalRgbEffect> ListEffects() => Effects;

            public void SubmitEffect(string launcherPath, string applicationUrl)
            {
                if (ThrowOnSubmit) throw new InvalidOperationException("Launcher failed.");
                Submissions.Add((launcherPath, applicationUrl));
            }
        }
    }
}
