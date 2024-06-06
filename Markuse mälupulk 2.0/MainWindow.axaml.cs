using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Linq;
using Avalonia.Interactivity;
using Markuse_mälupulk_2_0;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using MsBox.Avalonia;
using System.Threading.Tasks;
using System.Buffers.Text;
using RtfDomParser;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using DocumentFormat.OpenXml.Presentation;

namespace Markuse_mälupulk_2._0
{
    public partial class MainWindow : Window
    {
        string mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";   // mas root directory
        internal Color[] scheme = [Color.FromRgb(255, 255, 255), Color.FromRgb(0,0,0)];                          // default color scheme
        string flash_root = "";                                                                         // flash drive root directory
        readonly bool testing = true;                                                                            // avoid loading content when we're in axaml view
        internal string VerifileStatus = "BYPASS";
        string current_pin = "";
        readonly List<double> sts = [];
        readonly List<string> list = [];
        Thread[]? threads;
        readonly DispatcherTimer CheckIfConnected = new();
        bool canContinue = true;
        bool isChild = false;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowModel();
            CheckIfConnected.Interval = new TimeSpan(0, 0, 5);
            CheckIfConnected.Tick += CheckIfConnected_Tick;
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains("Markuse mälupulk")) 
                {
                    testing = false;
                    break;
                }
            }
        }





        private async void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            LinearGradientBrush? lgb = (LinearGradientBrush?)GradientBg.Background;
            if (lgb != null)
            {
                lgb.GradientStops[1].Color = (this.Background as SolidColorBrush).Color;
            }
            VerifileStatus = Verifile2();
            if (!isChild)
            {
                if (File.Exists(mas_root + "/edition.txt"))
                {
                    switch (VerifileStatus)
                    {
                        case "VERIFIED":
                            break;
                        case "FOREIGN":
                            await MessageBoxShow("Käivitasite programmi seadmes, mis ei vasta Markuse asjad süsteemi standarditele. Pange tähele, et teatud funktsionaalsus ei ole seetõttu saadaval.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                            return;
                        case "FAILED":
                            await MessageBoxShow("Verifile püsivuskontrolli läbimine nurjus. Programm ei tööta seadmetes, mis on valesti juurutatud.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                            this.Close();
                            return;
                        case "TAMPERED":
                            await MessageBoxShow("See arvuti pole õigesti juurutatud. Seda võis põhjustada hiljutine riistvaramuudatus. Palun kasutage juurutamiseks Markuse asjade juurutamistööriista. Programm ei tööta seadmetes, mis on valesti juurutatud.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                            this.Close();
                            return;
                        case "LEGACY":
                            await MessageBoxShow("See arvuti on juurutatud vana juurutamistööriistaga. Palun juurutage arvuti uuesti uue juurutamistarkvaraga. Programm ei tööta seadmetes, mis on valesti juurutatud.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                            this.Close();
                            return;
                    }
                    if (Verifile())
                    {
                        LoadTheme();
                    }
                }
                else
                {
                    await MessageBoxShow("Käivitasite programmi seadmes, mis ei vasta Markuse asjad süsteemi standarditele. Pange tähele, et teatud funktsionaalsus ei ole seetõttu saadaval.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                }
            }
            else
            {
                if ((VerifileStatus != "FOREIGN") || (VerifileStatus != "VERIFIED"))
                {
                    this.Close();
                    return;
                }
            }
            QAppPreview.Source = (Application.Current.Resources["Info"] as Image).Source;
            CreatePieChart(new Dictionary<string, int>() // create dummy pie chart for testing purposes
            {
                { "1", 20 },
                { "2", 32 },
                { "3", 24 },
                { "4", 11 },
                { "5", 64 },
                { "6", 5 },
                { "Vaba ruum", 100 },
            });
            if (!testing)
            {
                SelectDrive sd = new()
                {
                    parent = this,
                    Background = this.Background,
                    Foreground = this.Foreground
                };
                await sd.ShowDialog(this).WaitAsync(cancellationToken: CancellationToken.None);
                if (sd.exit)
                {
                    this.Close();
                    return;
                }
                flash_root = ((string[])sd.DriveList.SelectedItem)[0];
                CollectInfo();
            }
        }

        private async void CheckIfConnected_Tick(object? sender, EventArgs e)
        {
            if (ConnectionStateLabel.Content == null) { return; }
            if (!Directory.Exists(flash_root))
            {
                if (ConnectionStateLabel.Content.ToString() == "pole ühendatud")
                {
                    CheckIfConnected.IsEnabled = false;
                    MsBox.Avalonia.Enums.ButtonResult result = await MessageBoxShow("Seade pole enam ühendatud. Kas soovite muu seadme valida?", "", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning);
                    if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
                    {
                        SwitchDevice.IsChecked = true;
                        ReloadData(sender, null);
                    }
                    else
                    {
                        _ = await MessageBoxShow("Sisestage seade ja vajutage OK, et jätkata", "Mälupulka pole ühendatud", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                        CheckIfConnected.IsEnabled = true;
                    }
                }
                ConnectionStateLabel.Content = "pole ühendatud";
            }
            else
            {
                ConnectionStateLabel.Content = "ühendatud";
                this.Title = "Markuse mälupulk (" + flash_root + ")";
            }
            DevTab.IsVisible = LockManagement.IsVisible;
        }

        private int SafeIntConversion(double value)
        {
            return (int)(value / 1000.0);
        }

        private void CollectInfo()
        {
            CollectProgress.IsVisible = true;
            CollectProgress.IsIndeterminate = false;
            CollectProgress.Value = 0;
            ConnectionStateLabel.Content = "info kogumine...";
            CheckIfConnected.IsEnabled = false;
            DispatcherTimer waitForExit = new()
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            waitForExit.Tick += (object? sender, EventArgs e) =>
            {
                if (list.Count >= 6)
                {
                    Dictionary<string, int> stats= [];
                    int sum = 0;
                    string[] requiredFields = ["Markuse asjad", "Operatsioonsüsteemid", "Pakkfailid", "Kiirrakendused", "PlayStation mängud"];
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i > sts.Count - 1)
                        {
                            break;
                        }
                        switch (list[i])
                        {
                            case "markuse asjad":
                                if (!stats.ContainsKey("Markuse asjad") && (i < sts.Count))
                                {
                                    stats.Add("Markuse asjad", SafeIntConversion(sts[i]));
                                }
                                break;
                            case "multiboot":
                                if (stats.ContainsKey("Operatsioonsüsteemid"))
                                {
                                    stats["Operatsioonsüsteemid"] += SafeIntConversion(sts[i]);
                                }
                                else
                                {
                                    stats["Operatsioonsüsteemid"] = SafeIntConversion(sts[i]);
                                }
                                break;
                            case "sources":
                                if (stats.ContainsKey("Operatsioonsüsteemid"))
                                {
                                    stats["Operatsioonsüsteemid"] += SafeIntConversion(sts[i]);
                                }
                                else
                                {
                                    stats["Operatsioonsüsteemid"] = SafeIntConversion(sts[i]);
                                }
                                break;
                            case "Pakkfailid":
                                if (!stats.ContainsKey("Pakkfailid"))
                                {
                                    stats.Add("Pakkfailid", SafeIntConversion(sts[i]));
                                }
                                break;
                            case "Kiirrakendused":
                                if (!stats.ContainsKey("Kiirrakendused"))
                                {
                                    stats.Add("Kiirrakendused", SafeIntConversion(sts[i]));
                                }
                                break;
                            case "PlayStation mängud":
                                if (!stats.ContainsKey("PlayStation mängud") && (i < sts.Count))
                                {
                                    stats.Add("PlayStation mängud", SafeIntConversion(sts[i]));
                                }
                                break;
                        }
                        sum += SafeIntConversion(sts[i]);
                    }
                    foreach (string requiredField in requiredFields)
                    {
                        if (!stats.ContainsKey(requiredField))
                        {
                            stats.Add(requiredField, 0);
                        }
                        stats = RenameKey(stats, requiredField, requiredField + " (" + new SelectDrive().GetFriendlySize((long)stats[requiredField] * 1000, true) + ")");
                    }
                    DriveInfo drv = new(flash_root);
                    long usedSpace = drv.TotalSize - drv.TotalFreeSpace - ((long)sum * 1000L);
                    stats.Add("Muud failid", SafeIntConversion(usedSpace));
                    stats.Add("Vaba ruum", SafeIntConversion(drv.TotalFreeSpace));
                    stats = RenameKey(stats, "Muud failid", "Muud failid (" + new SelectDrive().GetFriendlySize((long)stats["Muud failid"] * 1000, true) + ")");
                    stats = RenameKey(stats, "Vaba ruum", "Vaba ruum (" + new SelectDrive().GetFriendlySize((long)stats["Vaba ruum"] * 1000, true) + ")");
                    CreatePieChart(stats);
                    canContinue = true;
                    waitForExit.IsEnabled = false;
                }
            };
            DispatcherTimer dpt = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 5)
            };
            dpt.Tick += (object? sender, EventArgs e) =>
            {
                if (CollectProgress.Value == CollectProgress.Maximum)
                {
                    CheckIfConnected.Start();
                    ConnectionStateLabel.Content = "ühendatud";
                    dpt.IsEnabled = false;
                    CollectProgress.IsVisible = false;
                    DataCollectTip.IsVisible = false;
                    NavigateDirectory(flash_root);
                }
                else
                {
                    if (canContinue)
                    {
                        CollectProgress.Value += 1;
                    } else
                    {
                        return;
                    }
                    switch (CollectProgress.Value)
                    {
                        case 1:
                            VideoBox.Items.Clear();
                            VideoBoxDev.Items.Clear();
                            foreach (FileInfo fi in new DirectoryInfo(flash_root + "/Markuse_videod").GetFiles())
                            {
                                if (fi.Name.Substring(1, 1) == ".")
                                {
                                    string videoName = string.Join('.', fi.Name.Split('.').Skip(1))[1..];
                                    VideoBox.Items.Add(videoName);
                                    VideoBoxDev.Items.Add(videoName);
                                }
                            }
                            break;
                        case 5:
                            LoadDoc("/E_INFO/uudis1.rtf");
                            break;
                        case 20:
                            UsersBox.Items.Clear();
                            foreach (DirectoryInfo d in new DirectoryInfo(flash_root + "/markuse asjad/markuse asjad").GetDirectories())
                            {
                                if ((d.Name == "Mine") || (d.Name == "_Template") || d.Name.StartsWith(' '))
                                {
                                    continue;
                                }
                                UsersBox.Items.Add(d.Name);
                            }
                            break;
                        case 35:
                            QAppBox.Items.Clear();
                            foreach (DirectoryInfo d in new DirectoryInfo(flash_root + "/markuse asjad/Kiirrakendused").GetDirectories())
                            {
                                if ((d.Name == " Mine") || (d.Name == "_Template") || d.Name.StartsWith(' '))
                                {
                                    continue;
                                }
                                QAppBox.Items.Add(d.Name);
                            }
                            break;
                        case 43:
                            if (File.Exists(mas_root + "/settings2.sf"))
                            {
                                string[] vs = File.ReadAllText(mas_root + "/settings2.sf", Encoding.ASCII).Split('=');
                                if (vs[1].ToString() == "true")
                                {
                                    AutostartCheck.IsChecked = true;
                                } else
                                {
                                    AutostartCheck.IsChecked = false;
                                }
                            }
                            break;
                        case 50:
                            string edition = File.ReadAllText(flash_root + "/E_INFO/edition.txt");
                            switch (edition.ToLower())
                            {
                                case "basic":
                                    EditionBox.Fill = new SolidColorBrush(Colors.LimeGreen);
                                    break;
                                case "premium":
                                    EditionBox.Fill = new SolidColorBrush(Colors.DarkRed);
                                    break;
                                case "ultimate":
                                    EditionBox.Fill = new SolidColorBrush(Colors.BlueViolet);
                                    break;
                            }
                            EditionLabel.Text = "Väljaanne: " + edition;
                            break;
                        case 55:
                            DriveInfo di = new DriveInfo(flash_root);
                            FilesystemLabel.Content = "Failisüsteem: " + di.DriveFormat;
                            CapacityLabel.Content = "Maht: " + new SelectDrive().GetFriendlySize(di.TotalSize);
                            DriveMountLabel.Content = "Draiv: " + di.RootDirectory;
                            break;
                        case 60:
                            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                            var attr = Attribute.GetCustomAttribute(assembly, typeof(BuildDateTimeAttribute)) as BuildDateTimeAttribute;
                            CpanelDateLabel.Content = "Kuupäev: " + attr?.Built.Date.ToString().Split(' ')[0];
                            CpanelVersionLabel.Content = "Versioon: " + assembly.GetName()?.Version?.ToString();
                            break;
                        case 65:
                            Thread[] th = [
                                new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/markuse asjad/markuse asjad"))),
                                new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/multiboot"))),
                                new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/sources"))),
                                new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/Pakkfailid"))),
                                new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/markuse asjad/Kiirrakendused"))),
                                new Thread(() => GetGameSize())
                            ];
                            threads = th;
                            foreach (Thread t in threads) {
                                t.Start();
                            }
                            waitForExit.Start();
                            canContinue = false;
                            break;
                    }
                }
            };
            dpt.Start();
        }

        Dictionary<string, int> RenameKey(Dictionary<string, int> dict, string key, string newName)
        {
            int value = dict[key];
            dict.Remove(key);
            dict[newName] = value;
            return dict;
        }

        public void NavigateDirectory(string path)
        {
            HelpLink.Content = "";
            BrowserTopText.Text = "Failisirvija";
            BrowserTopText.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            MainWindowModel mwm = new();
            mwm.Navigate(path);
            if (mwm.toptext != "")
            {
                BrowserTopText.Text = mwm.toptext;
                BrowserTopText.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            }
            if (mwm.info != "")
            {
                HelpLink.Content = mwm.infotopic + "?";
            }
            this.DataContext = mwm;
        }

        public void LoadDoc(string filename)
        {
            if (File.Exists(flash_root + filename.Replace(".rtf", ".docx")))
            {
                NewsBox.CloseDocument();
                NewsBox.LoadWordDoc(flash_root + filename.Replace(".rtf", ".docx"));
                NewsBox.FlowDoc.PagePadding = new Thickness(0);
            }
        }

        public void GetGameSize()
        {
            long gamedirsize = (long)0;
            gamedirsize += DirSize(new DirectoryInfo(flash_root + "/POPS"));
            gamedirsize += DirSize(new DirectoryInfo(flash_root + "/CD"));
            gamedirsize += DirSize(new DirectoryInfo(flash_root + "/DVD"));
            sts.Add(gamedirsize);
            list.Add("PlayStation mängud");
            Thread.CurrentThread.Interrupt();
        }

        public void GetDirSize(DirectoryInfo d)
        {
            sts.Add(DirSize(d));
            list.Add(d.Name);
            Thread.CurrentThread.Interrupt();
        }


        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            try
            {
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
            }
            catch { }
            // Add subdirectory sizes.
            try
            {
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    if (!((di.Attributes & FileAttributes.ReparsePoint) != 0))
                    {
                        size += DirSize(di);
                    }
                }
            }
            catch { }
            return size;
        }

        private void CreatePieChart(Dictionary<string, int> values)
        {
            SpaceUsage.Plot.Clear();
            ScottPlot.Color[] colors = [ScottPlot.Colors.Lime, ScottPlot.Colors.BlueViolet, ScottPlot.Colors.Yellow, ScottPlot.Colors.Red, ScottPlot.Colors.Cyan, ScottPlot.Colors.Blue, ScottPlot.Colors.Transparent];
            List<ScottPlot.PieSlice> slices = new();

            foreach (KeyValuePair<string, int> pair in values)
            {
                slices.Add(new ScottPlot.PieSlice() { Value = pair.Value, FillColor = colors.First(), Label = pair.Key });
                colors = colors.Skip(1).ToArray(); // remove each color from the colors array, so that we don't have to deal with indexing
            }

            // create doughnut/pie chart
            ScottPlot.Plottables.Pie p = SpaceUsage.Plot.Add.Pie(slices);
            p.DonutFraction = .5; // percentage taken up by the middle segment of the doughnut
            p.LineWidth = 0;
            p.LineColor = ScottPlot.Colors.Transparent; // remove outlines from doughnut segments (because they ugly)
            SpaceUsage.Plot.HideGrid(); // hides plot grid (completely pointless for pie charts)

            // make legend not look ugly
            SpaceUsage.Plot.Legend.BackgroundColor = ScottPlot.Colors.Transparent;
            SpaceUsage.Plot.Legend.ShadowFillStyle.Color = ScottPlot.Colors.Transparent;
            SpaceUsage.Plot.Legend.OutlineColor = ScottPlot.Colors.Transparent;

            // disable axies, because it's a piechart lol
            SpaceUsage.Plot.Layout.Frameless();
        }

        private void TestRtb()
        {
            NewsBox.LoadRtfDoc(@"G:\E_INFO\arhiiv\uudis1.rtf");
        }

        private static string Asgasggas(string sc){string s = sc;string a = s[..(s.Length / 2)];string b = s[(s.Length / 2)..];
        string c = "";for (int i = 0; i < b.Length; i++){c += (char)(b[i] - 1);if (i < a.Length){c += (char)(a[i] - 1);}}return c;}
        private void LoadTheme()
        {
            string? asoighiughw = VerifileStatus;
            byte[] data = [0xEF, 0xBB, 0xBF, 0x0B, 0x75, 0x6A, 0x68, 0x74, 0x73, 0x6E, 0x6D, 0x21, 0x21, 0x47, 0x4B, 0x58, 0x48, 0x23,
                           0x0E, 0x74, 0x73, 0x6F, 0x21, 0x21, 0x21, 0x64, 0x62, 0x63, 0x66, 0x54, 0x63, 0x75, 0x6A, 0x68, 0x31, 0x21,
                           0x6A, 0x75, 0x29, 0x64, 0x62, 0x63, 0x66, 0x4D, 0x6F, 0x75, 0x21, 0x21, 0x2A, 0x3C, 0x0B, 0x75, 0x6A, 0x68,
                           0x63, 0x3E, 0x74, 0x73, 0x6E, 0x6D, 0x2F, 0x76, 0x74, 0x73, 0x6F, 0x29, 0x6A, 0x75, 0x29, 0x64, 0x62, 0x63,
                           0x66, 0x4D, 0x6F, 0x75, 0x21, 0x21, 0x2A, 0x3C, 0x0B, 0x75, 0x6A, 0x68, 0x64, 0x3E, 0x23, 0x3C, 0x0B, 0x70,
                           0x21, 0x6A, 0x75, 0x6A, 0x3E, 0x31, 0x21, 0x21, 0x21, 0x2F, 0x66, 0x68, 0x69, 0x21, 0x2C, 0x2A, 0x0B, 0x0E,
                           0x64, 0x2C, 0x21, 0x64, 0x62, 0x2A, 0x63, 0x6A, 0x21, 0x21, 0x2A, 0x0E, 0x6A, 0x21, 0x6A, 0x3D, 0x62, 0x4D,
                           0x6F, 0x75, 0x2A, 0x0B, 0x0E, 0x64, 0x2C, 0x21, 0x64, 0x62, 0x2A, 0x62, 0x6A, 0x21, 0x21, 0x2A, 0x0E, 0x7E,
                           0x0B, 0x73, 0x75, 0x73, 0x21, 0x3C, 0x0E, 0x74, 0x73, 0x6F, 0x21, 0x64, 0x62, 0x63, 0x66, 0x3E, 0x23, 0x4B,
                           0x46, 0x54, 0x47, 0x3C, 0x0B, 0x75, 0x6A, 0x68, 0x62, 0x3E, 0x74, 0x73, 0x6E, 0x6D, 0x2F, 0x76, 0x74, 0x73,
                           0x6F, 0x29, 0x2D, 0x29, 0x6F, 0x2A, 0x74, 0x73, 0x6E, 0x6D, 0x2F, 0x66, 0x68, 0x69, 0x30, 0x33, 0x2A, 0x0E,
                           0x74, 0x73, 0x6F, 0x21, 0x21, 0x21, 0x64, 0x62, 0x63, 0x66, 0x54, 0x63, 0x75, 0x6A, 0x68, 0x29, 0x6F, 0x2A,
                           0x74, 0x73, 0x6E, 0x6D, 0x2F, 0x66, 0x68, 0x69, 0x30, 0x33, 0x2A, 0x0E, 0x74, 0x73, 0x6F, 0x21, 0x21, 0x21,
                           0x23, 0x0E, 0x67, 0x73, 0x29, 0x6F, 0x21, 0x21, 0x21, 0x3C, 0x6A, 0x3D, 0x63, 0x4D, 0x6F, 0x75, 0x3C, 0x6A,
                           0x2C, 0x0E, 0x7C, 0x0B, 0x21, 0x3E, 0x29, 0x69, 0x73, 0x29, 0x5C, 0x5E, 0x2E, 0x32, 0x3C, 0x0B, 0x67, 0x29,
                           0x21, 0x21, 0x2F, 0x66, 0x68, 0x69, 0x0E, 0x7C, 0x0B, 0x21, 0x3E, 0x29, 0x69, 0x73, 0x29, 0x5C, 0x5E, 0x2E,
                           0x32, 0x3C, 0x0B, 0x0E, 0x7E, 0x66, 0x76, 0x6F, 0x64];
            string aighoauisg = Encoding.UTF8.GetString(data); string? wihgouiwww = CSharpScript.
                EvaluateAsync(string.Concat(Asgasggas(aighoauisg).AsSpan(4), ";")).Result.ToString();

            if (File.Exists(mas_root + "/bg_common.png"))
            {
                if (asoighiughw != wihgouiwww) { return; }
                ImageBrush ib = new()
                {
                    Source = new Bitmap(mas_root + "/bg_common.png"),
                    Stretch = Stretch.UniformToFill
                };
                TopPanel.Background = ib;
            }
            if (asoighiughw != wihgouiwww) { return; }
            if (File.Exists(mas_root + "/scheme.cfg"))
            {
                string[] schemeData = File.ReadAllText(mas_root + "/scheme.cfg").Split(';');
                byte[] bg = schemeData[0].Split(":")[..3].Select(byte.Parse).ToArray();
                byte[] fg = schemeData[1].Split(":")[..3].Select(byte.Parse).ToArray();
                scheme = [Color.FromRgb(bg[0], bg[1], bg[2]), Color.FromRgb(fg[0], fg[1], fg[2])];
            }
            this.Background = new SolidColorBrush(scheme[0]);
            this.Foreground = new SolidColorBrush(scheme[1]);
            LinearGradientBrush? lgb = (LinearGradientBrush?)GradientBg.Background;
            if (lgb != null)
            {
                lgb.GradientStops[1].Color = scheme[0];
            }
        }

        private void Reload_Click(object? sender, RoutedEventArgs e)
        {
            string? docStr = DocNav.Content?.ToString()?.Split("/")[0].Split(" ")[1];
            if (docStr == null)
            {
                return;
            }
            int docId = int.Parse(docStr);

            LoadDoc($"/E_INFO/uudis{docId}.rtf");
        }

        private void Fullscreen_Toggle(object? sender, RoutedEventArgs e)
        {
            CheckBox? me = (CheckBox?)sender;
            if (me != null)
            {
                this.WindowState = (me.IsChecked ?? false) ? WindowState.FullScreen : WindowState.Normal;
                this.FullScreenExit.IsVisible = me.IsChecked ?? false;
            }
        }

        private void Image_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Image? me = (Image?)sender;
                if (me != null)
                {
                    Random r = new();
                    me.RenderTransform = new ScaleTransform(r.Next(1, 5), r.Next(1,5));
                }
            }
        }

        private void QAppBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            Bitmap? loadedimage = null;
            if (QAppBox.SelectedItems?.Count <= 0)
            {
                QAppName.Text = "Kiirrakendused";
                QAppDescription.Text = "Kiirrakendused on nagu tavalised töölauarakendused, " +
                               "mida leiate enda arvutist, aga neil on eriline " +
                               "funktsioon: need töötavad kohe igas arvutis otse mälupulgalt.";

                QAppPreview.Source = (Application.Current.Resources["Info"] as Image).Source;
                return;
            }
            QAppName.Text = "See tekst ei peaks mitte kunagi nähtav olema";
            QAppDescription.Text = "(rakenduse puudub kirjeldus)";

            try
            {
                QAppPreview.Source = (Application.Current.Resources["Info"] as Image).Source;
                QAppName.Text = this.QAppBox.SelectedItems[0].ToString();
                QAppDescription.Text = File.ReadAllText(flash_root + "/markuse asjad/Kiirrakendused/" + this.QAppBox.SelectedItems[0].ToString() + "/" + this.QAppBox.SelectedItems[0].ToString() + "Info.txt", Encoding.Unicode);
                loadedimage = new Bitmap(flash_root + "/markuse asjad/Kiirrakendused/" + this.QAppBox.SelectedItems[0].ToString() + "/" + this.QAppBox.SelectedItems[0].ToString() + "ScreenShot.bmp");

                QAppPreview.Source = loadedimage;
                QAppPreview.Stretch = Stretch.None;
            }
            catch
            {
            }
        }

        private void ExitApp()
        {
            this.Close();
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }

        private void OpenApp(object sender, RoutedEventArgs e)
        {
            OpenApp();
        }

        private void DeleteApp(object sender, RoutedEventArgs e)
        {
            DeleteApp();
        }


        void OpenApp()
        {
            string? appName = QAppBox.SelectedItems?[0]?.ToString();
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = flash_root + "/markuse asjad/Kiirrakendused/" + appName?.ToString() + "/" + appName?.ToString() + "Portable.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.WorkingDirectory = flash_root + "/markuse asjad/Kiirrakendused/" + appName?.ToString();
                p.Start();
            }
            catch (Exception ex)
            {
                _= MessageBoxShow(ex.Message, "Tundub, et rakenduse käivitumine ebaõnnestus. Põhjus on järgmine:", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        void DeleteApp()
        {
            string? appName = QAppBox.SelectedItems?[0]?.ToString();
            try
            {
                Directory.Delete(flash_root + "/markuse asjad/Kiirrakendused/" + appName?.ToString(), true);
                QAppBox.Items.Clear();
                if (Directory.Exists(flash_root + "/markuse asjad/Kiirrakendused"))
                {
                    foreach (string fi in Directory.GetDirectories(flash_root + "/markuse asjad/Kiirrakendused"))
                    {
                        QAppBox.Items.Add(fi.Replace(flash_root + "/markuse asjad/Kiirrakendused/", "").ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBoxShow(ex.Message, "Tundub, et rakenduse kustutamine ebaõnnestus. Põhjus on järgmine:", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void DataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (FileBrowser.SelectedItems.Count > 0)
            {
                string? selFolder = ((Folder?)FileBrowser.SelectedItems[0])?.Name;
                MainWindowModel? ctx = ((MainWindowModel?)DataContext);
                if ((ctx == null) || (selFolder == null))
                {
                    return;
                }
                string currentDir = ctx.url;
                NavigateDirectory(currentDir + "/" + selFolder);
            }
        }

        private void NavigateDirectory(object? sender, RoutedEventArgs e)
        {
            if (sender is Button me)
            {
                switch (me.Content)
                {
                    case "Markuse asjad":
                        NavigateDirectory(flash_root + "/markuse asjad");
                        break;
                    case "Kiirrakendused":
                        NavigateDirectory(flash_root + "/markuse asjad/Kiirrakendused");
                        break;
                    case "Esiletõstetud videod":
                        NavigateDirectory(flash_root + "/Markuse_videod");
                        break;
                    case "Abi ja info":
                        NavigateDirectory(flash_root + "/markuse asjad/Abi ja info");
                        break;
                    case "Skriptid":
                        NavigateDirectory(flash_root + "/Pakkfailid");
                        break;
                    case "Ventoy opsüsteemid":
                        NavigateDirectory(flash_root + "/multiboot");
                        break;
                    case "Ventoy konfiguratsioon":
                        NavigateDirectory(flash_root + "/ventoy");
                        break;
                    case "Ketta juurkaust":
                        NavigateDirectory(flash_root);
                        break;
                    case "Mine":
                        NavigateDirectory(flash_root + "/markuse asjad/markuse asjad/Mine");
                        break;
                }
            }
        }

        private void UserDirsTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if ((UserDirs.SelectedItems?.Count > 0) && (UsersBox.SelectedItems?.Count > 0))
            {
                string? username = UsersBox.SelectedItems[0]?.ToString();
                string? subfolder = ((ListBoxItem?)UserDirs.SelectedItems[0])?.Content?.ToString();
                if (username == null || subfolder == null)
                {
                    return;
                }
                NavigateDirectory(flash_root + "/markuse asjad/markuse asjad/" + username + "/" + subfolder);
            }
        }

        private void UsersBoxTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            UserDirs.IsEnabled = UsersBox.SelectedItems?.Count > 0;
        }

        private void HelpLink_Click(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            MainWindowModel? mwm = (MainWindowModel?)this.DataContext;
            if (mwm != null)
            {
                _ = MessageBoxShow(mwm.info, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Question);
            }
        }

        private void NextDoc_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            string? docStr = DocNav.Content?.ToString()?.Split("/")[0].Split(" ")[1];
            if (docStr == null)
            {
                return;
            }
            int docId = int.Parse(docStr);
            if (docId == 5)
            {
                docId = 0;
            }
            LoadDoc($"/E_INFO/uudis{++docId}.rtf");
            DocNav.Content = string.Format("Artikkel {0}/5", docId);
        }

        private void PreDoc_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            string? docStr = DocNav.Content?.ToString()?.Split("/")[0].Split(" ")[1];
            if (docStr == null)
            {
                return;
            }
            int docId = int.Parse(docStr);
            if (docId == 1)
            {
                docId = 6;
            }
            LoadDoc($"/E_INFO/uudis{--docId}.rtf");
            DocNav.Content = string.Format("Artikkel {0}/5", docId);
        }

        private void RichTextBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            NewsSidePanel.IsVisible = !NewsSidePanel.IsVisible;
            if (!NewsSidePanel.IsVisible)
            {
                NewsGrid.ColumnDefinitions[2].MaxWidth = 0;
            } else
            {
                NewsGrid.ColumnDefinitions[2].MaxWidth = 9999;
            }
        }

        private void PlayVideo(object? sender, RoutedEventArgs e)
        {
            if (VideoBox.SelectedItems?.Count > 0)
            {
                Process p = new();
                p.StartInfo.FileName = $"{flash_root}/Markuse_videod/{VideoBox.SelectedIndex+1}. {VideoBox.SelectedItems[0]}";
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
        }

        private void AllVidsClick(object? sender, RoutedEventArgs e)
        {
            Process p = new();
            p.StartInfo.FileName = $"{flash_root}/Markuse_videod";
            p.StartInfo.UseShellExecute = true;
            p.Start();
        }

        private void VideoBox_Select(object? sender, SelectionChangedEventArgs e)
        {
            VideoPlayButton.IsEnabled = VideoBox.SelectedItems?.Count > 0;
        }

        private async void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            TabControl? me = sender as TabControl;
            if (me != null)
            {
                if (me.SelectedIndex == 5)
                {
                    DevPanel.IsVisible = false;
                    Parool p = new();
                    p.Background = this.Background;
                    p.Foreground = this.Foreground;
                    if (File.Exists(flash_root + "/NTFS/spin.sys"))
                    {
                        if (current_pin != File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString())
                        {
                            string ostr = File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString();
                            p.Title = "Arendamise valikud nõuavad autentimist";
                            p.currentpin = "";
                            await p.ShowDialog(this).WaitAsync(CancellationToken.None);
                            byte[] correct = MD5.HashData(Encoding.ASCII.GetBytes(p.currentpin));
                            string cstr = "";
                            foreach (byte b in correct)
                            {
                                cstr += b.ToString("X2");
                            }
                            string s = File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString();
                            current_pin = cstr;
                            if (cstr != ostr)
                            {
                                me.SelectedIndex = 0;
                                await MessageBoxShow("Vale PIN kood", "Arendamise valikud nõuavad autentimist", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).WaitAsync(CancellationToken.None);
                                return;
                            }
                            else
                            {
                                DevPanel.IsVisible = true;
                                LockManagement.IsVisible = true;
                            }
                        } else
                        {
                            DevPanel.IsVisible = true;
                        }
                    }
                    else if (File.Exists(flash_root + "/NTFS/config.sys"))
                    {
                        if (current_pin != File.ReadAllLines(flash_root + "/NTFS/config.sys", Encoding.ASCII)[0].ToString())
                        {
                            string ostr = File.ReadAllLines(flash_root + "/NTFS/config.sys", Encoding.ASCII)[0].ToString();
                            p.Title = "Arendamise valikud nõuavad autentimist";
                            p.currentpin = "";
                            await p.ShowDialog(this).WaitAsync(CancellationToken.None);
                            current_pin = p.currentpin;
                            if (p.currentpin != ostr)
                            {
                                me.SelectedIndex = 0;
                                await MessageBoxShow("Vale PIN kood", "Arendamise valikud nõuavad autentimist", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).WaitAsync(CancellationToken.None);
                                return;
                            } else
                            {
                                DevPanel.IsVisible = true;
                                LockManagement.IsVisible = true;
                            }
                        }
                        else
                        {
                            DevPanel.IsVisible = true;
                        }
                    }
                }
            }
        }

        private void LockFeatsClick(object? sender, RoutedEventArgs e)
        {
            current_pin = "";
            LockManagement.IsVisible = false;
        }

        private void ReloadData(object? sender, RoutedEventArgs? e) {
            if (SwitchDevice.IsChecked ?? false)
            {
                MainWindow mw = new();
                mw.isChild = true;
                mw.scheme = this.scheme;
                mw.Show();
                this.Close();
            } else
            {
                LoadTheme();
                QAppPreview.Source = (Application.Current.Resources["Info"] as Image).Source;
                CreatePieChart(new Dictionary<string, int>() // create dummy pie chart for testing purposes
                {
                    { "1", 20 },
                    { "2", 32 },
                    { "3", 24 },
                    { "4", 11 },
                    { "5", 64 },
                    { "6", 5 },
                    { "Vaba ruum", 100 },
                });
                sts.Clear();
                list.Clear();
                canContinue = true;
                threads = [];
                CollectInfo();
            }
        }

        private async void AddApp(object? sender, RoutedEventArgs e)
        {
            AddApp aa = new()
            {
                flash_root = flash_root
            };
            await aa.ShowDialog(this).WaitAsync(CancellationToken.None);
            MsBox.Avalonia.Enums.ButtonResult msgbox = await MessageBoxShow("Värskendan andmeid?", "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question).WaitAsync(CancellationToken.None);
            if (msgbox == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                ReloadData(sender, e);
            }
        }

        private void Autostart_Click(object? sender, RoutedEventArgs e)
        {
            if (CollectProgress.IsVisible) { return; }
            File.WriteAllText(mas_root + "/settings2.sf", "AutoRun=" + (AutostartCheck.IsChecked ?? false).ToString().ToLower(), Encoding.ASCII);
        }

        private void EditionLabel_Click(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            if (e.Pointer.IsPrimary && (EditionLabel.Text != null))
            {
                List<string> features = [];
                if (File.Exists(flash_root + "/E_INFO/edition.txt")) { features.Add("Integratsioon Markuse arvutiga"); };
                if (File.Exists(flash_root + "/E_INFO/uudis1.rtf")) { features.Add("Uudised ja lisainfo mälupulga kohta"); };
                if (File.Exists(flash_root + "/E_INFO/videod.txt") && Directory.Exists(flash_root + "/Markuse_videod")) { features.Add("Uusimad videod"); };
                if (File.Exists(flash_root + "/NTFS/spin.sys")) { features.Add("Turvalise PIN koodi tugi"); };
                if (File.Exists(flash_root + "/NTFS/config.sys")) { features.Add("Ebaturvalise PIN koodi tugi"); };
                if (Directory.Exists(flash_root + "/Batch") || Directory.Exists(flash_root + "/Pakkfailid")) { features.Add("Pakkfailid"); };
                if (Directory.Exists(flash_root + "/markuse asjad/markuse asjad")) { features.Add("Markuse kaustad"); };
                if (Directory.Exists(flash_root + "/markuse asjad/Kiirrakendused")) { features.Add("Kiirrakendused"); };
                if (Directory.Exists(flash_root + "/multiboot")) { features.Add("Operatsioonsüsteemide käivitamine mälupulgalt"); };
                if (File.Exists(flash_root + "/CD/games.bin") || File.Exists(flash_root + "/DVD/games.bin")) { features.Add("PS2 mängude laadimine mälupulgalt"); };
                if (File.Exists(flash_root + "/POPS/POPSTARTER.ELF") && File.Exists(flash_root + "/POPS/POPS_IOX.PAK")) { features.Add("PS1 mängude laadimine mälupulgalt"); };
                if (File.Exists(flash_root + "/autorun.inf") && File.Exists(flash_root + "/mas_flash.ico")) { features.Add("Kohandatud ikoon ja pikk draivi nimi"); };
                if (File.Exists(flash_root + "/autorun.inf") && !File.Exists(flash_root + "/mas_flash.ico")) { features.Add("Pikk draivi nimi"); };
                if (File.Exists(flash_root + "/markuse asjad/markuse asjad/kasutajad.txt") && File.Exists(flash_root + "/E_INFO/videod.txt")) { features.Add("Ühilduvus varasemate mälupulga programmidega"); };
                if (File.Exists(flash_root + "/NTFS/config.sys") && (new DriveInfo(flash_root).DriveType == DriveType.Removable)) { features.Add("Mälupulga lahtilukustuse tugi"); };
                if ((EditionLabel.Text.Contains("Ultimate") || EditionLabel.Text.Contains("Premium")) && (File.Exists(flash_root + "/E_INFO/convert.bat"))) { features.Add("Väljaande muutmine"); }
                string featurestr = "";
                foreach (string feature in features)
                {
                    featurestr += " - " + feature + "\n";
                }
                _ = MessageBoxShow(EditionLabel.Text + "\nSee väljanne sisaldab järgmisi funktsioone:\n" + featurestr, "Täpsem info väljaande kohta", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Setting);
            }
        }

        private void Extras_Click(object? sender, RoutedEventArgs e)
        {
            Extras ex = new()
            {
                Background = this.Background,
                Foreground = this.Foreground,
                parent = this
            };
            
            ex.Show();
        }

        private async void RenameFlash_Click(object? sender, RoutedEventArgs e)
        {
            InputBox ib = new();
            ib.Text.Content = "Sisestage uus mälupulga nimi";
            ib.Title = "Mälupulga nime muutmine";
            ib.Background = new SolidColorBrush(scheme[0]);
            ib.Foreground = new SolidColorBrush(scheme[1]);
            EncodingProvider ppp = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(ppp);
            await ib.ShowDialog(this).WaitAsync(CancellationToken.None);
            if (ib.result)
            {
                try
                {
                    string content = "[Autorun]\r\nlabel=" + ib.InputTxt.Text + "\r\nicon=autorun.exe,0\r\n";
                    File.WriteAllText(flash_root + "/autorun.inf", content, Encoding.GetEncoding("windows-1252"));
                    _ = MessageBoxShow("Mälupulga nimi muudeti edukalt. Väljutage mälupulk ja sisestage see uuesti, et muudatusi näha", "Toiming õnnestus", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                }
                catch (Exception ex)
                {
                    _ = MessageBoxShow(ex.Message, "Midagi läks valesti", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                }
            } else
            {
                _ = MessageBoxShow("Kasutaja katkestas toimingu", "Mälupulga nime ei muudetud", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Setting);
            }
        }

        private async void ConvertEdition_Click(object? sender, RoutedEventArgs e)
        {
            if (Directory.Exists(flash_root + "/markuse asjad/Kasutajad") && ((new DirectoryInfo(flash_root + "/markuse asjad/Kasutajad").Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint))
            {
                _ = MessageBoxShow("Tegemist on UKSS v2.0 süsteemi mälupulgaga. Teisendamisfunktsioon pole veel implementeeritud.", "Teisenda väljaandeks", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                return;
            }
            //teisenda väljaandeks
            if (File.Exists(flash_root + "/NTFS/config.sys"))
            {
                Parool p = new();
                p.Background = this.Background;
                p.Foreground = this.Foreground;
                if (p.currentpin != File.ReadAllLines(flash_root + "/NTFS/config.sys", Encoding.ASCII)[0].ToString())
                {
                    p.Title = "Kinnitamiseks sisestage PIN kood";
                    p.currentpin = "";
                    await p.ShowDialog(this).WaitAsync(CancellationToken.None);
                    string s = File.ReadAllLines(flash_root + "/NTFS/config.sys", Encoding.ASCII)[0].ToString();
                    if (p.currentpin != s)
                    {
                        _ = MessageBoxShow("Vale PIN kood", "Toiming katkestati", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                        return;
                    }
                }
            }
            MsBox.Avalonia.Enums.ButtonResult result = await MessageBoxShow("See funktsioon võimaldab Markuse mälupulga väljaande teisendada. Kui te teisendate Premium -> Ultimate, siis lisanduvad järgnevad funktsioonid/kui teisendate Ultimate -> Premium kaovad järgmised funktsioonid:\n\nKaustad\nKiirrakendused\n\nKas olete kindel, et soovite jätkata?", "Väljaande teisendamine", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning);
            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                if (File.Exists(flash_root + "/E_INFO/convert.bat"))
                {
                    Process p = new();
                    p.StartInfo.FileName = flash_root + "/E_INFO/convert.bat";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.WorkingDirectory = flash_root + "/E_INFO";
                    p.Start();
                }
                else
                {
                    _ = MessageBoxShow("Teisendusprogrammi ei eksisteeri. Kui teie seade kasutab Basic väljaannet, ei ole seda võimalik teisendada.", "Ei saa teisendada", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                }
            }
            else
            {
                _ = MessageBoxShow("Toiming katkestati", "Muudatusi ei tehtud", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Setting);
            }
        }


        private async void LegacyPin_Click(object? sender, RoutedEventArgs e)
        {
            //muuda pin koodi
            try
            {
                if (!Directory.Exists(flash_root + "/NTFS")) { Directory.CreateDirectory(flash_root + "/NTFS"); }
            }
            catch
            {
                _ = MessageBoxShow("Juurdepääs asukohale \"" + flash_root + "/NTFS\" puudub. Kas seade on ühendatud?", "Ei saa PIN koodi muuta", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                return;
            }
            Parool p = new()
            {
                Background = this.Background,
                Foreground = this.Foreground
            };
            MsBox.Avalonia.Enums.ButtonResult result = await MessageBoxShow("Kas soovite sisse lülitada ebaturvalise PIN-koodi? See on vajalik, kui soovite mälupulka hallata vanemate programmidega. Pange tähele, et ebaturvaline PIN kood ei ole krüptitud ning lihtsasti kättesaadav ükskõik millisele kasutajale.", "Ebaturvalise PIN-koodi seadistamine", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning);
            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                if (File.Exists(flash_root + "/NTFS/spin.sys"))
                {
                    if (current_pin != File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString())
                    {
                        p.Title = "Kinnitamiseks sisestage vana PIN kood";
                        p.currentpin = "";
                        await p.ShowDialog(this).WaitAsync(CancellationToken.None);
                        string s = File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString();

                        byte[] correct = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(p.currentpin));
                        string cstr = "";
                        foreach (byte b in correct)
                        {
                            cstr += b.ToString("X2");
                        }
                        if (cstr != s)
                        {
                            _ = MessageBoxShow("Vale PIN kood", "Toiming katkestati", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                            return;
                        }
                        else
                        {
                            LockManagement.IsVisible = true;
                        }
                    }
                }
                p = new()
                {
                    Background = this.Background,
                    Foreground = this.Foreground,
                    Title = "Sisestage uus PIN kood",
                    currentpin = ""
                };
                await p.ShowDialog(this).WaitAsync(CancellationToken.None);
                try
                {
                    File.WriteAllText(flash_root + "/NTFS/config.sys", p.currentpin + "\n", Encoding.ASCII);
                    _ = MessageBoxShow("Uus PIN kood salvestati edukalt", "Toiming õnnestus", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                }
                catch (Exception ex)
                {
                    _ = MessageBoxShow(ex.Message, "PIN koodi salvestamine nurjus", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                }
            }
            else
            {
                File.WriteAllText(flash_root + "/NTFS/config.sys", "Ebaturvaline PIN kood keelatud\nInsecure authentication code disabled\n", Encoding.ASCII);
                _ = MessageBoxShow("Ebaturvaline PIN kood lülitati edukalt välja", "Toiming õnnestus", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
            }
        }

        private async void ChangePin_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(flash_root + "/NTFS")) { Directory.CreateDirectory(flash_root + "/NTFS"); }
            }
            catch
            {
                _ = MessageBoxShow("Juurdepääs asukohale \"" + flash_root + "/NTFS\" puudub. Kas seade on ühendatud?", "Ei saa PIN koodi muuta", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                return;
            }
            Parool p = new()
            {
                Background = this.Background,
                Foreground = this.Foreground
            };
            if (File.Exists(flash_root + "/NTFS/spin.sys"))
            {
                if (current_pin != File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString())
                {
                    p.Title = "Kinnitamiseks sisestage vana PIN kood";
                    p.currentpin = "";
                    await p.ShowDialog(this).WaitAsync(CancellationToken.None);
                    string s = File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString();

                    byte[] correct = MD5.HashData(Encoding.ASCII.GetBytes(p.currentpin));
                    string cstr = "";
                    foreach (byte b in correct)
                    {
                        cstr += b.ToString("X2");
                    }
                    if (cstr != s)
                    {
                        _ = MessageBoxShow("Vale PIN kood", "Toiming katkestati", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                        return;
                    }
                    else
                    {
                        LockManagement.IsVisible = true;
                    }
                }
            }
            p = new()
            {
                Title = "Sisestage uus PIN kood",
                currentpin = "",
                Background = this.Background,
                Foreground = this.Foreground
            };
            await p.ShowDialog(this).WaitAsync(CancellationToken.None);
            try
            {
                byte[] correct = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(p.currentpin));
                string cstr = "";
                foreach (byte b in correct)
                {
                    cstr += b.ToString("X2");
                }
                File.WriteAllText(flash_root + "/NTFS/spin.sys", cstr + "\n", Encoding.ASCII);
                _ = MessageBoxShow("Uus PIN kood salvestati edukalt", "Toiming õnnestus", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
            }
            catch (Exception ex)
            {
                _ = MessageBoxShow(ex.Message, "PIN koodi salvestamine nurjus", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private async void BM_Click(object? sender, RoutedEventArgs e)
        {
            if (File.Exists(flash_root + "/NTFS/spin.sys"))
            {
                Parool p = new();
                if (current_pin != File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString())
                {
                    p.Title = "Kinnitamiseks sisestage vana PIN kood";
                    p.currentpin = "";
                    p.Background = this.Background;
                    p.Background = this.Background;
                    await p.ShowDialog(this).WaitAsync(CancellationToken.None);
                    string s = File.ReadAllLines(flash_root + "/NTFS/spin.sys", Encoding.ASCII)[0].ToString();

                    byte[] correct = MD5.HashData(Encoding.ASCII.GetBytes(p.currentpin));
                    string cstr = "";
                    foreach (byte b in correct)
                    {
                        cstr += b.ToString("X2");
                    }
                    if (cstr != s)
                    {
                        _ = MessageBoxShow("Vale PIN kood", "Toiming katkestati", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                        return;
                    }
                    else
                    {
                        LockManagement.IsVisible = true;
                    }
                }
            }
            else
            {
                if (File.Exists(flash_root + "/NTFS/config.sys"))
                {
                    _ = MessageBoxShow("Varukoopiate haldamiseks tuleb välja lülitada ebaturvaline PIN-kood. Haldamise vahekaardis, valige \"Ebaturvaline PIN kood\", et seda teha..", "Varukoopiate haldamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    return;
                }
            }
            if (Directory.Exists(flash_root + "/"))
            {
                this.Hide();
                //varundamine/taaste
                BackupManager br = new()
                {
                    Background = this.Background,
                    Foreground = this.Foreground
                };
                if (WindowState == WindowState.FullScreen)
                {
                    br.WindowState = WindowState.FullScreen;
                }
                br.dest_drive = flash_root + "/";
                br.parent = this;
                br.Show();
            }
            else
            {
                _ = MessageBoxShow("Viga: Seadet pole ühendatud", "", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }


        // verifile stuff
        private string Verifile2()
        {
            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-jar " + mas_root + "/verifile2.jar",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            };
            p.Start();
            while (!p.StandardOutput.EndOfStream)
            {
                string line = p.StandardOutput.ReadLine() ?? "";
                return line.Split('\n')[0];
            }
            return "FAILED";
        }


        private bool Verifile()
        {
            return Verifile2() == "VERIFIED";
        }


        // Reimplementation of WinForms MessageBox.Show
        private Task<MsBox.Avalonia.Enums.ButtonResult> MessageBoxShow(string message, string caption = "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }
    }
}