using FocusRgb.Contracts;
using FocusRgb.PluginSdk;

namespace FocusRgb.Plugin.SignalRgb
{
    internal sealed class SignalRgbPlugin(ISignalRgbPlatform platform) : IFocusRgbPlugin
    {
        private static readonly PluginReason LayoutUnsupported = new()
        {
            Code = "capability-unsupported",
            Message = "No supported SignalRGB layout interface is available. Omit the layout to retain provider placements."
        };

        public Task<InitializeResult> InitializeAsync(
            InitializeParameters parameters,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            return Task.FromResult(new InitializeResult
            {
                PluginId = "signalrgb",
                PluginVersion = typeof(SignalRgbPlugin).Assembly.GetName().Version!.ToString(3),
                ContractVersion = ContractVersion.Current,
                Capabilities =
                [
                    Capability("provider.effect.catalog", "supported"),
                    Capability("provider.effect.select", "unverified", "selection-not-observable",
                        "SignalRGB accepts application URLs, but the selected effect cannot be read back reliably."),
                    Capability("provider.selection.observe", "unverified", "selection-not-observable",
                        "The current SignalRGB effect cannot be read back reliably."),
                    Capability("provider.layout.catalog", "unsupported", LayoutUnsupported.Code, LayoutUnsupported.Message),
                    Capability("provider.layout.select", "unsupported", LayoutUnsupported.Code, LayoutUnsupported.Message),
                    Capability("provider.layout.export", "unsupported", "capability-unsupported",
                        "SignalRGB does not expose layout geometry through a verified interface."),
                    Capability("provider.devices.report", "unsupported", "capability-unsupported",
                        "SignalRGB does not expose a verified managed-device catalog to this plugin.")
                ]
            });
        }

        public Task<PluginStatusResult> GetStatusAsync(CancellationToken cancellationToken) =>
            Task.FromResult(platform.IsProviderRunning()
                ? new PluginStatusResult
                {
                    Availability = "available",
                    Reason = Reason("selection-not-observable",
                        "SignalRGB is running, but its current effect cannot be read back reliably.")
                }
                : new PluginStatusResult
                {
                    Availability = "unavailable",
                    Reason = Reason("provider-not-running",
                        "SignalRGB is not running. It will not be started automatically.")
                });

        public Task<PluginCatalogResult> ListCatalogAsync(
            CatalogListParameters parameters,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            if (!string.Equals(parameters.Kind, "effect", StringComparison.Ordinal))
            {
                return Task.FromResult(new PluginCatalogResult
                {
                    Support = "unsupported",
                    Reason = LayoutUnsupported
                });
            }

            var items = platform.ListEffects()
                .Select(effect => new PluginCatalogItem
                {
                    ResourceId = effect.ResourceId,
                    DisplayName = effect.DisplayName,
                    Available = effect.Available,
                    Reason = effect.Available
                        ? null
                        : Reason("resource-not-found", effect.UnavailableReason ?? "The effect is unavailable.")
                })
                .ToArray();
            return Task.FromResult(new PluginCatalogResult
            {
                Support = "supported",
                RefreshedAt = DateTimeOffset.UtcNow,
                Items = items
            });
        }

        public Task<BindingApplyResult> ApplyBindingAsync(
            BindingApplyParameters parameters,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            if (!string.Equals(parameters.RenderingAuthority, "provider", StringComparison.Ordinal))
            {
                return Applied("unsupported", "capability-unsupported",
                    "The SignalRGB plugin supports provider rendering authority only.");
            }
            if (!string.Equals(parameters.DeviceScope.Kind, "provider-managed", StringComparison.Ordinal)
                || parameters.DeviceScope.DeviceIds is { Count: > 0 })
            {
                return Applied("unsupported", "capability-unsupported",
                    "The SignalRGB plugin supports provider-managed device scope only.");
            }
            if (parameters.Layout is not null)
            {
                return Task.FromResult(new BindingApplyResult
                {
                    Outcome = "unsupported",
                    Reason = LayoutUnsupported
                });
            }
            if (parameters.Effect is null || string.IsNullOrWhiteSpace(parameters.Effect.ResourceId))
            {
                return Applied("rejected", "resource-not-found", "A resolved SignalRGB effect is required.");
            }
            if (!platform.IsProviderRunning())
            {
                return Applied("unavailable", "provider-not-running",
                    "SignalRGB is not running. It will not be started automatically.");
            }

            var effect = platform.ListEffects().SingleOrDefault(item =>
                string.Equals(item.ResourceId, parameters.Effect.ResourceId, StringComparison.Ordinal));
            if (effect is null
                || !effect.Available
                || !string.Equals(effect.DisplayName, parameters.Effect.DisplayName, StringComparison.Ordinal))
            {
                return Applied("unavailable", "resource-not-found",
                    "The requested SignalRGB effect is not present in the current catalog.");
            }

            var launcher = platform.FindLauncher();
            if (launcher is null)
            {
                return Applied("unavailable", "provider-not-running", "The SignalRGB launcher was not found.");
            }

            var encodedName = Uri.EscapeDataString(effect.DisplayName);
            var applicationUrl = $"signalrgb://effect/apply/{encodedName}?-silentlaunch-";
            try
            {
                platform.SubmitEffect(launcher, applicationUrl);
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return Applied("unavailable", "provider-not-running", exception.Message);
            }

            return Applied("unverified", "selection-not-observable",
                "The effect request was submitted, but visible output could not be read back.");
        }

        public Task<BindingReleaseResult> ReleaseBindingAsync(
            BindingReleaseParameters parameters,
            CancellationToken cancellationToken) =>
            Task.FromResult(new BindingReleaseResult { Outcome = "nothing-to-release" });

        public Task ShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static PluginCapability Capability(
            string id,
            string support,
            string? code = null,
            string? message = null) => new()
            {
                Id = id,
                Support = support,
                Reason = code is null || message is null ? null : Reason(code, message)
            };

        private static PluginReason Reason(string code, string message) => new() { Code = code, Message = message };

        private static Task<BindingApplyResult> Applied(string outcome, string code, string message) =>
            Task.FromResult(new BindingApplyResult
            {
                Outcome = outcome,
                Reason = Reason(code, message)
            });
    }
}
