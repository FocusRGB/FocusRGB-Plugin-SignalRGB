using FocusRgb.PluginSdk;

namespace FocusRgb.Plugin.SignalRgb
{
    internal static class Program
    {
        public static Task Main() => PluginServer.RunAsync(
            new SignalRgbPlugin(new WindowsSignalRgbPlatform()),
            Console.In,
            Console.Out,
            Console.Error);
    }
}
