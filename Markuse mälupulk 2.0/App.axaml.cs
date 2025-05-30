using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Markuse_mälupulk_2_0;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MasCommon;

namespace Markuse_mälupulk_2._0
{
    public partial class App : Application
    {
        public static string root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        public static bool foreign = false;
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
                if ((args?.Length > 0) && args.Contains("--safemode")) {
                    desktop.MainWindow = new SafeMode();
                } else {
                    desktop.MainWindow = new MainWindow();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

    }
}