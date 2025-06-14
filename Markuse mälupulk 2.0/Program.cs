using Avalonia;
using DocumentFormat.OpenXml.Vml;
using System;
using System.Diagnostics;

namespace Markuse_mälupulk_2._0
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            } catch (Exception ex) when (!Debugger.IsAttached)
            {
                Restart(ex);
            }
        }

        public static void Restart(Exception? ex = null)
        {
            string? exePath = Environment.ProcessPath;
            if (exePath is null)
            {
                Environment.Exit(255);
                return;
            }
            if (ex is not null)
            {
                Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, Arguments = "/e \"" + ex.Message.Replace("\"", "\\\"") + "\" + \"" + (ex.StackTrace ?? "").Replace("\"", "\\\"") + "\"" }); Environment.Exit(0);
            } else
            {
                Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true }); Environment.Exit(0);
            }
        }

        public static void RestartSafe()
        {
            string? exePath = Environment.ProcessPath;
            if (exePath is null)
            {
                Environment.Exit(255);
                return;
            }
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, Arguments = "--safemode" }); Environment.Exit(0);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
