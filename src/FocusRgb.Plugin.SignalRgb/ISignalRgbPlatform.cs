namespace FocusRgb.Plugin.SignalRgb
{
    internal interface ISignalRgbPlatform
    {
        bool IsProviderRunning();

        string? FindLauncher();

        IReadOnlyList<SignalRgbEffect> ListEffects();

        void SubmitEffect(string launcherPath, string applicationUrl);
    }

    internal sealed record SignalRgbEffect(string ResourceId, string DisplayName, bool Available, string? UnavailableReason = null);
}
