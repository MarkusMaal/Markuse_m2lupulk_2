using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markuse_mälupulk_2_0;
using MasCommon;
using MsBox.Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Avalonia.Controls;
using Tmds.DBus.Protocol;

namespace Markuse_mälupulk_2._0
{
    public partial class App : Application
    {
        public static string root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        public static bool foreign = false;
        private Window MainWindow { get; set; }
        public override void Initialize()
        {
            if (!Verifile.CheckVerifileTamper() && !foreign)
            {
                return;
            }
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var args = desktop.Args;
                bool showError = (args ?? []).Contains("/e");
                if (showError)
                {
                    if (Debugger.IsAttached)
                    {
                        throw new Exception("User attempted to display crash window with a debugger attached. If you want to test the crashing window, please start this program without a debugger attached.");
                    }
                    Crash c = new();
                    if ((args is null) || (args.Length < 4)) {
                        args = ["", "End-user manually initiated a crash", "", "", ""];
                    }
                    c.ErrorDescription.Text = (args ?? ["", ""])[1];
                    c.StackTrace.Text = string.Join(" ", (args ?? ["", ""]).Skip(3).ToArray());
                    MainWindow = c;
                    desktop.MainWindow = MainWindow;
                    CompleteInit();
                    return;
                }
                if ((args?.Length > 0) && args.Contains("--safemode"))
                {
                    MainWindow = new SafeMode();
                }
                else
                {
                    MainWindow = new MainWindow();
                }

                desktop.MainWindow = MainWindow;
            }

            CompleteInit();
        }

        private void CompleteInit()
        {
            base.OnFrameworkInitializationCompleted();
        }

        private void CloseNativeButtonClick(object? sender, EventArgs e)
        {
            MainWindow.Close();
        }

        private void RefreshNativeButtonClick(object? sender, EventArgs e)
        {
            if (MainWindow is not MainWindow main) return;
            if (!main.IsVisible) return;
            main.ReloadData(sender, null);
        }
    }
}