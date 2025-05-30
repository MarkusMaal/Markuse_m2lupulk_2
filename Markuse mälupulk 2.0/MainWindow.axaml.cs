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
using System.Threading;
using Avalonia.Threading;
using System.Diagnostics;
using System.Text;
using MsBox.Avalonia;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Avalonia.Platform.Storage;
using AvRichTextBox;
using System.Globalization;
using MasCommon;
using ScottPlot;
using Color = Avalonia.Media.Color;
using Colors = Avalonia.Media.Colors;
using Image = Avalonia.Controls.Image;

namespace Markuse_mälupulk_2._0
{
    public partial class MainWindow : Window
    {
        readonly internal string mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + (OperatingSystem.IsWindows() ? "\\" : "/") + ".mas";   // mas root directory
        internal Color[] scheme = [Color.FromRgb(255, 255, 255), Color.FromRgb(0,0,0)];                          // default color scheme
        internal string flash_root = "";                                                                         // flash drive root directory
        readonly bool testing = true;                                                                            // avoid loading content when we're in axaml view
        readonly bool devtest = false;
        internal string VerifileStatus = "BYPASS";
        string current_pin = "";
        readonly List<double> sts = [];
        readonly List<string> list = [];
        Thread[]? threads;

        bool canContinue = true;
        bool isChild = false;
        readonly bool simulation = false;
        private RichTextBox rtb;

        // timers
        readonly DispatcherTimer CheckIfConnected = new();
        readonly DispatcherTimer CheckFqa = new();
        readonly DispatcherTimer CopyProgress = new();

        // file browser stuff
        bool copy = false;
        bool fqa = false;
        string clipboard = "";
        string current_file = "";
        string[] filenames;
        string outfile = "";
        int progress = 0;
        
        private Verifile vf = new();


        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowModel();
            rtb = new RichTextBox();
            TimerSetup();
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
            {
                testing = false;
            }
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains("Markuse mälupulk")) 
                {
                    testing = false;
                    break;
                }
            }
        }


        private void TimerSetup()
        {
            CheckIfConnected.Interval = new TimeSpan(0, 0, 5);
            CheckFqa.Interval = new TimeSpan(0, 0, 0, 0, 100);
            CopyProgress.Interval = new TimeSpan(0,0,0,0,100);
            CheckIfConnected.Tick += CheckIfConnected_Tick;
            CheckFqa.Tick += CheckFqa_Tick;
            CopyProgress.Tick += CopyProgress_Tick;
        }


        private async void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            LinearGradientBrush? lgb = (LinearGradientBrush?)GradientBg.Background;
            if (lgb != null)
            {
                lgb.GradientStops[1].Color = (this.Background as SolidColorBrush).Color;
            }
            VerifileStatus = vf.MakeAttestation();
            if (!isChild && !testing)
            {
                if (File.Exists(mas_root + "/edition.txt"))
                {
                    switch (VerifileStatus)
                    {
                        case "VERIFIED":
                            break;
                        case "FAILED":
                        case "FOREIGN":
                            await MessageBoxShow("Käivitasite programmi seadmes, mis ei vasta Markuse asjad süsteemi standarditele. Pange tähele, et teatud funktsionaalsus ei ole seetõttu saadaval.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                            break;
                        case "TAMPERED":
                            await MessageBoxShow("See arvuti pole õigesti juurutatud. Seda võis põhjustada hiljutine riistvaramuudatus. Palun kasutage juurutamiseks Markuse asjade juurutamistööriista. Programm ei tööta seadmetes, mis on valesti juurutatud.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                            this.Close();
                            return;
                        case "LEGACY":
                            await MessageBoxShow("See arvuti on juurutatud vana juurutamistööriistaga. Palun juurutage arvuti uuesti uue juurutamistarkvaraga. Programm ei tööta seadmetes, mis on valesti juurutatud.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                            this.Close();
                            return;
                    }
                    LoadTheme();
                }
                else
                {
                    await MessageBoxShow("Käivitasite programmi seadmes, mis ei vasta Markuse asjad süsteemi standarditele. Pange tähele, et teatud funktsionaalsus ei ole seetõttu saadaval.\nKood: VF_" + VerifileStatus, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                }
            }
            else if (!testing)
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
                    Foreground = this.Foreground,
                };
                await sd.ShowDialog(this).WaitAsync(cancellationToken: CancellationToken.None);
                if (sd.exit)
                {
                    this.Close();
                    return;
                }
                if (sd.DriveList.SelectedItems.Count == 0)
                {
                    WaitUntilConnected();
                    return;
                }
                flash_root = ((string[])sd.DriveList.SelectedItem)[0];
                CollectInfo();
            }
        }

        private void CheckFqa_Tick(object? sender, EventArgs e)
        {
            if (fqa)
            {
                CollectProgress.IsIndeterminate = false;
                CollectProgress.IsVisible = true;
                CheckFqa.IsEnabled = false;
                CheckIfConnected.IsEnabled = true;
                FoldersTab.IsEnabled = true;
                if (ConnectionStateLabel?.Content?.ToString()?.Equals("kasutaja loomine") ?? false)
                {
                    _ = MessageBoxShow("Kasutaja loodi edukalt", "Uue kasutaja lisamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                    UsersBox.Items.Add(DataCollectTip.Content);
                    //WriteLegacyUsers();
                    AddUserButton.IsEnabled = true;
                } else
                {
                    RefreshFileBrowser(sender, null);
                }
                if (ConnectionStateLabel == null)
                {
                    return;
                }
                ConnectionStateLabel.Content = "valmis";
                DataCollectTip.Content = "Andmete kogumise ajal saate juba laaditud funktsioone kasutada";
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
                if (clipboard == "")
                {
                    ConnectionStateLabel.Content = "ühendatud";
                } else
                {
                    ConnectionStateLabel.Content = $"fail lõikelaual: {clipboard}";
                }
                this.Title = "Markuse mälupulk (" + flash_root + ")";
            }
            DevTab.IsVisible = LockManagement.IsVisible || devtest;
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
            int waitTime = 0;
            waitForExit.Tick += async (object? sender, EventArgs e) =>
            {
                if ((list.Count >= 6) || (waitTime > 30)) // waitTime is the timeout, if stat collection for some reason stalls and doesn't complete, just move on with data collection
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
                    try
                    {
                        DriveInfo drv = new(flash_root);
                        long usedSpace = drv.TotalSize - drv.TotalFreeSpace - ((long)sum * 1000L);
                        stats.Add("Muud failid", SafeIntConversion(usedSpace));
                        stats.Add("Vaba ruum", SafeIntConversion(drv.TotalFreeSpace));
                        stats = RenameKey(stats, "Muud failid", "Muud failid (" + new SelectDrive().GetFriendlySize((long)stats["Muud failid"] * 1000, true) + ")");
                        stats = RenameKey(stats, "Vaba ruum", "Vaba ruum (" + new SelectDrive().GetFriendlySize((long)stats["Vaba ruum"] * 1000, true) + ")");
                        CreatePieChart(stats);
                        canContinue = true;
                        waitForExit.IsEnabled = false;
                        if (waitTime > 30)
                        {
                            await MessageBoxShow("Statistika andmete kogumisel ilmnes tundmatu rike. Kogutud info võib olla ebatäpne.", "Hoiatus", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                        }
                    } catch (Exception ex)
                    {
                        waitForExit.IsEnabled = false;
                        await MessageBoxShow($"Ilmnes viga: {ex.Message}", "Viga", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                        waitForExit.IsEnabled = true;
                        return;
                    }
                }
                else
                { 
                    waitTime++;
                }
            };
            DispatcherTimer dpt = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 5)
            };
            dpt.Tick += async (object? sender, EventArgs e) =>
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
                    try
                    {
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
                                    }
                                    else
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
                                foreach (Thread t in threads)
                                {
                                    t.Start();
                                }
                                waitForExit.Start();
                                canContinue = false;
                                break;
                        }
                    } catch (Exception ex)
                    {
                        canContinue = false;
                        switch (await MessageBoxShow($"Info kogumine nurjus. Vea üksikasjad: {ex.Message}\n\n{ex.StackTrace}\n\nKas soovite muu mälupulga valida?", "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.YesNoAbort, MsBox.Avalonia.Enums.Icon.Error))
                        {
                            case MsBox.Avalonia.Enums.ButtonResult.Abort:
                                Environment.Exit(1);
                                return;
                            case MsBox.Avalonia.Enums.ButtonResult.Yes:
                                dpt.IsEnabled = false;
                                SelectDrive sd = new()
                                {
                                    parent = this,
                                    Background = this.Background,
                                    Foreground = this.Foreground,
                                };
                                await sd.ShowDialog(this).WaitAsync(cancellationToken: CancellationToken.None);
                                if (sd.exit)
                                {
                                    this.Close();
                                    return;
                                }
                                flash_root = ((string[])sd.DriveList.SelectedItem)[0];
                                canContinue = true;
                                CollectInfo();
                                break;
                            case MsBox.Avalonia.Enums.ButtonResult.No:
                                CollectProgress.Value--;
                                canContinue = true;
                                break;
                        }
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
            /*if (File.Exists(flash_root + filename.Replace(".rtf", ".docx")))
            {
                NewsBox.CloseDocument();
                NewsBox.LoadWordDoc(flash_root + filename.Replace(".rtf", ".docx"));
                NewsBox.FlowDocument.PagePadding = new Thickness(0);
            }
            else */ if (File.Exists(flash_root + filename))
            {
                NewsBox.CloseDocument();
                NewsBox.LoadRtfDoc(flash_root + filename);
                NewsBox.FlowDocument.PagePadding = new Thickness(0);
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
            try
            {
                sts.Add(DirSize(d));
                list.Add(d.Name);
                Thread.CurrentThread.Interrupt();
            } catch
            {
                GetDirSize(d);
            }
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
                    if (!di.Attributes.HasFlag(FileAttributes.ReparsePoint))
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
                slices.Add(new ScottPlot.PieSlice() { Value = pair.Value, FillColor = colors.First(), Label = pair.Key, LegendText = pair.Key, LabelStyle = new LabelStyle() { Alignment = Alignment.LowerRight}});
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

        internal void LoadTheme()
        {
            if (VerifileStatus != "VERIFIED")
            {
                return;
            }

            if (File.Exists(mas_root + "/bg_common.png"))
            {
                if (VerifileStatus != "VERIFIED") { return; }
                ImageBrush ib = new()
                {
                    Source = new Bitmap(mas_root + "/bg_common.png"),
                    Stretch = Stretch.UniformToFill
                };
                TopPanel.Background = ib;
            }
            if (VerifileStatus != "VERIFIED") { return; }
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

        private void DataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs? e)
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

        private void Open_Click(object? sender, RoutedEventArgs e)
        {
            DataGrid_DoubleTapped(sender, null);
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

        private void WaitUntilConnected()
        {
            SelectDrive sd;
            DispatcherTimer searchDrives = new()
            {
                Interval = new TimeSpan(0, 0, 0, 1)
            };
            searchDrives.Tick += async (object? sender, EventArgs e) =>
            {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (File.Exists(di.Name + "/E_INFO/edition.txt"))
                    {
                        searchDrives.IsEnabled = false;
                        sd = new()
                        {
                            parent = this,
                            Background = this.Background,
                            Foreground = this.Foreground,
                        };
                        await sd.ShowDialog(this).WaitAsync(cancellationToken: CancellationToken.None);
                        if (sd.exit)
                        {
                            this.Close();
                            return;
                        }
                        if (sd.DriveList.SelectedItems.Count == 0)
                        {
                            searchDrives.IsEnabled = true;
                        }
                        else
                        {
                            flash_root = ((string[])sd.DriveList.SelectedItem)[0];
                            CollectInfo();
                        }
                    }
                }
            };
            searchDrives.Start();
        }

        private async void ReloadData(object? sender, RoutedEventArgs? e) {
            LoadTheme();
            QAppPreview.Source = (Application.Current.Resources["Info"] as Image).Source;
            if (SwitchDevice.IsChecked ?? false)
            {
                SelectDrive sd = new();
                sd.Background = this.Background;
                sd.Foreground = this.Foreground;
                //sd.Position = new PixelPoint(Position.X + (int)Width / 2 - (int)sd.Width / 2, Position.Y + (int)Height / 2 - (int)sd.Height / 2);
                sd.parent = this;
                await sd.ShowDialog(this).WaitAsync(CancellationToken.None);
                if (sd.DriveList.SelectedItems.Count == 0)
                {

                }
                if (sd.DriveList.SelectedItems.Count > 0) { 
                    flash_root = ((string[])sd.DriveList.SelectedItem)[0];
                    if (!Directory.Exists(flash_root))
                    {
                        ReloadData(sender, e);
                    }
                } else
                {
                    ReloadData(sender, e);
                }
            }
            CreatePieChart(new Dictionary<string, int>() // create dummy pie chart for testing purposes
            {
                { "See", 20 },
                { "väike", 32 },
                { "mölder", 24 },
                { "jõuab", 11 },
                { "rongile", 64 },
                { "hüpata", 5 },
                { "Vaba ruum", 100 },
            });
            sts.Clear();
            list.Clear();
            canContinue = true;
            threads = [];
            CollectInfo();
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
                if (File.Exists(flash_root + "/E_INFO/convert.bat") && OperatingSystem.IsWindows())
                {
                    Process p = new();
                    p.StartInfo.FileName = flash_root + "/E_INFO/convert.bat";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.WorkingDirectory = flash_root + "/E_INFO";
                    p.Start();
                } else if (File.Exists(flash_root + "/E_INFO/convert.sh") && OperatingSystem.IsLinux()) {
                    Process p = new();
                    p.StartInfo.FileName = "konsole";
                    p.StartInfo.Arguments = "-e sh " + flash_root + "/E_INFO/convert.sh";
                    p.StartInfo.UseShellExecute = true;
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

        private async void DevAddVideo_Click(object? sender, RoutedEventArgs e)
        {

            bool done = false;
            while (!done)
            {
                var topLevel = GetTopLevel(this);
                if (topLevel == null) { return; }
                // Start async operation to open the dialog.
                FilePickerFileType VideoType = new("Videfailid")
                {
                    Patterns = ["*.bk2", "*.mnv", "*.ogv", "*.mjpg", "*.zmv", "*.mgv", "*.wmv", "*.flv", "*.3gp", "*.webm", "*.mov", "*.avi", "*.mp4", "*.mpg"]
                };

                var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Vali video mälupulgalt",
                    SuggestedStartLocation = StorageProvider.TryGetFolderFromPathAsync(flash_root).Result,
                    FileTypeFilter = [VideoType, FilePickerFileTypes.All]
                });

                if ((file is null) || (file.Count < 1))
                {
                    return;
                }
                string fullName = Uri.UnescapeDataString(file[0].Path.AbsolutePath);
                if (!fullName.StartsWith(flash_root.Replace("\\", "/")))
                {
                    MsBox.Avalonia.Enums.ButtonResult result = await MessageBoxShow("Videofail peab asuma mälupulgal", "Ei saa uut videot lisada", MsBox.Avalonia.Enums.ButtonEnum.OkAbort, MsBox.Avalonia.Enums.Icon.Error);
                    if (result == MsBox.Avalonia.Enums.ButtonResult.Ok)
                    {
                        done = false;
                    }
                    else
                    {
                        done = true;
                    }
                    continue;
                }
                try
                {
                    // Start async operation to open the dialog.
                    var dir = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        Title = $"Valige kaust kuhu teisaldada \"{VideoBoxDev.Items[2]}\"",
                        SuggestedStartLocation = StorageProvider.TryGetFolderFromPathAsync(flash_root).Result,
                    });

                    if ((dir is not null) && (dir.Count > 0))
                    {
                        File.Move(fullName, flash_root + "/Markuse_videod/1. " + file[0].Name);
                        File.Move($"{flash_root}/Markuse_videod/1. {VideoBoxDev.Items[0]}", $"{flash_root}/Markuse_videod/2. {VideoBoxDev.Items[0]}");
                        File.Move($"{flash_root}/Markuse_videod/2. {VideoBoxDev.Items[1]}", $"{flash_root}/Markuse_videod/3. {VideoBoxDev.Items[1]}");
                        File.Move($"{flash_root}/Markuse_videod/3. {VideoBoxDev.Items[2]}", $"{Uri.UnescapeDataString(dir[0].Path.AbsolutePath)}/{VideoBoxDev.Items[2]}");
                        string? second = VideoBox.Items[0]?.ToString();
                        string? third = VideoBox.Items[1]?.ToString();
                        VideoBox.Items.Clear();
                        VideoBox.Items.Add(file[0].Name);
                        VideoBox.Items.Add(second);
                        VideoBox.Items.Add(third);
                        _ = MessageBoxShow("Uus video lisati edukalt. Kui soovite, et uued videod toimiksid vanemates programmides, klõpsake \"Rakenda muudatused\" nuppu.", "Uue video lisamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                        done = true;
                    } else
                    {
                        _ = MessageBoxShow("Ei saanud videot lisada. Põhjus: Te ei määranud kausta, kuhu kolmas video teisaldada.\r\n\r\nMuudatusi ei tehtud.", "Uue video lisamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                        done = true;
                    }
                }
                catch (Exception ex)
                {
                    MsBox.Avalonia.Enums.ButtonResult erresult = await MessageBoxShow(ex.Message + "\r\n\r\nVajutage \"Katkesta\", et toiming katkestada\r\nVajutage \"OK\", et valida muu fail", "Ei saa uut videot lisada", MsBox.Avalonia.Enums.ButtonEnum.OkAbort, MsBox.Avalonia.Enums.Icon.Error);
                    if (erresult == MsBox.Avalonia.Enums.ButtonResult.Ok)
                    {
                        done = false;
                    }
                    else
                    {
                        done = true;
                    }
                    continue;
                }
                done = true;
                
            }
            return;
        }

        private async void DevEditVideo_Click(object? sender, RoutedEventArgs e)
        {
            bool done = false;
            while (!done)
            {
                var topLevel = GetTopLevel(this);
                if (topLevel == null) { return; }
                // Start async operation to open the dialog.
                FilePickerFileType VideoType = new("Videfailid")
                {
                    Patterns = ["*.bk2", "*.mnv", "*.ogv", "*.mjpg", "*.zmv", "*.mgv", "*.wmv", "*.flv", "*.3gp", "*.webm", "*.mov", "*.avi", "*.mp4", "*.mpg"]
                };

                var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Vali video mälupulgalt",
                    SuggestedStartLocation = StorageProvider.TryGetFolderFromPathAsync(flash_root).Result,
                    FileTypeFilter = [VideoType, FilePickerFileTypes.All]
                });

                if ((file is null) || (file.Count < 1))
                {
                    return;
                }
                string fullName = Uri.UnescapeDataString(file[0].Path.AbsolutePath);
                if (!fullName.StartsWith(flash_root.Replace("\\", "/"))) // replace with forward slash to fix an issue in Windows
                {
                    MsBox.Avalonia.Enums.ButtonResult result = await MessageBoxShow("Videofail peab asuma mälupulgal", "Ei saa uut videot lisada", MsBox.Avalonia.Enums.ButtonEnum.OkAbort, MsBox.Avalonia.Enums.Icon.Error);
                    if (result == MsBox.Avalonia.Enums.ButtonResult.Ok)
                    {
                        done = false;
                    }
                    else
                    {
                        done = true;
                    }
                    continue;
                }
                try
                {
                    // Start async operation to open the dialog.
                    var dir = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        Title = $"Valige kaust kuhu teisaldada video \"{VideoBoxDev.SelectedItem}\"",
                        SuggestedStartLocation = StorageProvider.TryGetFolderFromPathAsync(flash_root).Result,
                    });

                    if ((dir is not null) && (dir.Count > 0))
                    {
                        File.Move(fullName, flash_root + "/Markuse_videod/" + (VideoBoxDev.SelectedIndex + 1).ToString() + ". " + file[0].Name);
                        File.Move($"{flash_root}/Markuse_videod/{VideoBoxDev.SelectedIndex + 1}. {VideoBoxDev.SelectedItem}", $"{Uri.UnescapeDataString(dir[0].Path.AbsolutePath)}/{VideoBoxDev.SelectedItem}");
                        VideoBoxDev.Items[VideoBoxDev.SelectedIndex] = file[0].Name;
                        _ = MessageBoxShow("Video asendati edukalt. Kui soovite, et vanemad mälupulga tarkvara versioonid muudatusi näeksid, vajutage nupule \"Rakenda muudatused\".", "Video asendamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                        done = true;
                    }
                    else
                    {
                        _ = MessageBoxShow("Ei saanud videot asendada. Põhjus: Te ei määranud kausta, kuhu eelmine video teisaldada.\r\n\r\nMuudatusi ei tehtud.", "Video asendamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                        done = true;
                    }
                }
                catch (Exception ex)
                {
                    MsBox.Avalonia.Enums.ButtonResult erresult = await MessageBoxShow(ex.Message + "\r\n\r\nVajutage \"Katkesta\", et toiming katkestada\r\nVajutage \"OK\", et valida muu fail", "Ei saa uut videot asendada", MsBox.Avalonia.Enums.ButtonEnum.OkAbort, MsBox.Avalonia.Enums.Icon.Error);
                    if (erresult == MsBox.Avalonia.Enums.ButtonResult.Ok)
                    {
                        done = false;
                    }
                    else
                    {
                        done = true;
                    }
                    continue;
                }
                done = true;
            }
        }

        private void DevApplyVideo_Click(object? sender, RoutedEventArgs e)
        {
            //videofaili salvestamine
            File.WriteAllText(flash_root + "/E_INFO/videod.txt", $"{VideoBoxDev.Items[0]};{VideoBoxDev.Items[1]};{VideoBoxDev.Items[2]}");

            //annab kasutajale teada, et kõik õnnestus
            _ = MessageBoxShow("Andmed salvestati edukalt. Muudatused on nüüd nähtavad vanemates versioonides", "Arendamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
        }

        private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.F12)
            {
                DevTab.IsVisible = !DevTab.IsVisible;
            }
        }

        private void EraseClipboard(object? sender, RoutedEventArgs e)
        {
            clipboard = "";
        }

        private void RefreshFileBrowser(object? sender, RoutedEventArgs? e)
        {
            // navigate to the same directory, i.e. refresh
            NavigateDirectory(GetWorkingDirectory());
        }

        private void CutCopyFile(object? sender, RoutedEventArgs e)
        {
            if (FileBrowser.SelectedItems.Count > 0)
            {
                string? selFolder = ((Folder?)FileBrowser.SelectedItems[0])?.Name;
                string currentDir = GetWorkingDirectory();
                // get header text of sender (the object that invoked this method) and set the copy variable accordingly
                string? headerText = ((MenuItem?)sender)?.Header?.ToString();
                copy = headerText == "Kopeeri";
                clipboard = $"{currentDir}/{selFolder}";
            }
        }

        private string GetWorkingDirectory()
        {
            // get current url from MainWindowModel
            MainWindowModel? ctx = ((MainWindowModel?)DataContext);
            if ((ctx == null))
            {
                return "";
            }
            return ctx.url;
        }

        private async void NewFolderClick(object? sender, RoutedEventArgs e)
        {
            InputBox ib = new();
            ib.Text.Content = "Andke kaustale nimi";
            await ib.ShowDialog(this).WaitAsync(CancellationToken.None);
            if (ib.result)
            {
                Directory.CreateDirectory(GetWorkingDirectory() + "/" + ib.InputTxt.Text);
            }
            NavigateDirectory(GetWorkingDirectory());
        }


        void CopyFiles()
        {
            foreach (string file in filenames)
            {
                progress++;
                current_file = file;
                FileInfo fi = new FileInfo(current_file);
                try
                {
                    string required_dir = fi.DirectoryName.Replace(clipboard, outfile);
                    if (!simulation)
                    {
                        if (!Directory.Exists(required_dir))
                        {
                            Directory.CreateDirectory(required_dir);
                        }
                        if (copy)
                        {
                            File.Copy(file, required_dir + "/" + fi.Name, true);
                        }
                        else
                        {
                            File.Move(file, required_dir + "/" + fi.Name);
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (Exception e) {
                    if (!simulation)
                    {
                        throw;
                    }
                }
            }
        }



        void DelFile(string FileName)
        {
            if (!simulation)
            {
                File.Delete(FileName);
            }
            else
            {
                Thread.Sleep(5000);
            }
            fqa = true;
        }

        void DelTree(string FolderName)
        {
            if (!simulation)
            {
                try
                {
                    Directory.Delete(FolderName, true);
                }
                catch
                {

                }
            }
            else
            {
                Thread.Sleep(10000);
            }
            fqa = true;
        }


        private async void DeleteFileFolder(object? sender, RoutedEventArgs e)
        {
            if (await MessageBoxShow("See toiming kustutab valitud üksuse jäädavalt. Kas olete kindel, et soovite jätkata?", "Failisüsteemi üksuse kustutamine", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning)  == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                if (FileBrowser.SelectedItems.Count > 0)
                {
                    string? selectedItem = FileBrowser.SelectedItems[0]?.ToString();
                    FoldersTab.IsEnabled = false;
                    fqa = false;
                    CheckIfConnected.IsEnabled = false;
                    CollectProgress.IsIndeterminate = true;
                    CollectProgress.IsVisible = true;
                    CheckFqa.IsEnabled = true;
                    string s = GetWorkingDirectory() + "/" + selectedItem;
                    if (File.Exists(s))
                    {
                        DataCollectTip.Content = $"faili kustutamine... (\"{selectedItem}\")";
                        new Thread(() => DelFile(s)).Start();
                    } else if (Directory.Exists(s))
                    {
                        DataCollectTip.Content = $"kausta kustutamine... (\"{selectedItem}\")";
                        new Thread(() => DelTree(s)).Start();
                    }
                } else
                {
                    FoldersTab.IsEnabled = false;
                    fqa = false;
                    CheckIfConnected.IsEnabled = false;
                    CollectProgress.IsIndeterminate = true;
                    CollectProgress.IsVisible = true;
                    ConnectionStateLabel.Content = $"kausta kustutamine... (\"{GetWorkingDirectory()}\")";
                    CheckFqa.IsEnabled = true;
                    new Thread(() => DelTree(GetWorkingDirectory())).Start();
                }
                if (!simulation)
                {
                    DataCollectTip.Content = "Ärge eemaldage mälupulka arvutist!";
                } else
                {
                    DataCollectTip.Content = "// Simulatsioon //";
                }
                DataCollectTip.IsVisible = true;
            }
        }

        private void PasteFileFolder(object? sender, RoutedEventArgs e)
        {
            if (Directory.Exists(clipboard))
            {
                filenames = Directory.GetFiles(clipboard, "*.*", SearchOption.AllDirectories);
                // copied item is a folder
                if (FileBrowser.SelectedItems.Count == 0)
                {
                    outfile = GetWorkingDirectory() ;
                }
                else
                {
                    if (Directory.Exists(GetWorkingDirectory() + "/" + FileBrowser.SelectedItems[0].ToString()))
                    {
                        outfile = GetWorkingDirectory() + "/" + FileBrowser.SelectedItems[0].ToString();
                    }
                    else
                    {
                        outfile = GetWorkingDirectory();
                    }
                }
                Thread t = new Thread(new ThreadStart(CopyFiles));
                t.Start();
                CopyProgress.IsEnabled = true;
                CheckIfConnected.IsEnabled = false;
                CollectProgress.IsVisible = true;
                CollectProgress.Maximum = filenames.Length;
                CollectProgress.Value = 0;
                FoldersTab.IsEnabled = false;
                if (!simulation)
                {
                    DataCollectTip.Content = "Ärge eemaldage mälupulka arvutist!";
                }
                else
                {
                    DataCollectTip.Content = "// Simulatsioon //";
                }
                DataCollectTip.IsVisible = true;
            } else if (File.Exists(clipboard))
            {
                // copied item is a file
                if (copy)
                {
                    File.Copy(clipboard, GetWorkingDirectory() + "/" + new FileInfo(clipboard).Name);
                }
                else
                {
                    File.Move(clipboard, GetWorkingDirectory() + "/" + new FileInfo(clipboard).Name);
                }
                NavigateDirectory(GetWorkingDirectory());
            }
        }


        private void CopyProgress_Tick(object sender, EventArgs e)
        {
            ReloadDataButton.IsEnabled = (progress < CollectProgress.Maximum);
            if (progress < CollectProgress.Maximum)
            {
                CollectProgress.Value = progress;
                string task = "";
                if (copy)
                {
                    task = "kopeerimine";
                }
                else
                {
                    task = "teisaldamine";
                }
                ConnectionStateLabel.Content = $"Faili \"{new FileInfo(current_file).Name}\" {task}...";
            }
            else
            {
                CopyProgress.IsEnabled = false;
                CollectProgress.Value = 0;
                CollectProgress.IsVisible = false;
                FoldersTab.IsEnabled = true;
                clipboard = "";
                ConnectionStateLabel.Content = "valmis";
                filenames = null;
                CheckIfConnected.IsEnabled = true;
                if (!simulation)
                {
                    NavigateDirectory(GetWorkingDirectory());
                }
                DataCollectTip.Content = "Andmete kogumise ajal saate juba laaditud funktsioone kasutada";
                DataCollectTip.IsVisible = false;
            }
        }

        private void EditNews(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button) {
                RealEditNews(button.Content?.ToString()?[^1..] ?? "1"); // gets last charcter from button content and passes it to RealEditNews
            }
        }

        private async void RealEditNews(string idx)
        {
            RichCreator rc = new($"{flash_root}/E_INFO/uudis{idx}.rtf".Replace("\\", "/"));
            await rc.ShowDialog(this).WaitAsync(CancellationToken.None);
            rc.RichTextBox1.SaveRtfDoc($"{flash_root}/E_INFO/uudis{idx}.rtf");
            rc.Close();
        }


        private async void Create_Doc_Click(object? sender, RoutedEventArgs e)
        {
            RichCreator rc = new();
            await rc.ShowDialog(this).WaitAsync(CancellationToken.None);
            rtb = rc.RichTextBox1;
        }

        private async void Import_Doc_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }
            // Start async operation to open the dialog.
            FilePickerFileType DocType = new("Microsoft Word 2007+ dokument")
            {
                Patterns = ["*.docx", "*.dotx"]
            };

            var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Valige imporditav dokument",
                FileTypeFilter = [DocType]
            });

            if (file is not null && file.Count > 0)
            {
                try
                {
                    rtb.LoadWordDoc(Uri.UnescapeDataString(file[0].Path.AbsolutePath));
                    await MessageBoxShow("Fail laaditi edukalt mällu", "Uudisefaili laadimine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                } catch (Exception ex)
                {
                    await MessageBoxShow($"Faili laadimine ebaõnnestus\nViga: {ex.Message}", "Uudisefaili laadimine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                }
            } else
            {
                await MessageBoxShow("Faili ei laaditud, kuna kasutaja katkestas toimingu", "Uudisefaili laadimine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Setting);
            }
        }

        private async void Archive_Doc_Click(object? sender, RoutedEventArgs e)
        {
            if (await MessageBoxShow("See funktsioon võimaldab lisada viimase uudise arhiveeritud uudiste kausta. Kas soovite jätkata?", "Uudise arhiveerimine", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question) == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                bool no_archives = true;
                string[] types = ["rtf", "docx"];
                foreach (string type in types)
                {
                    if (File.Exists($"{flash_root}/E_INFO/uudis6.{type}"))
                    {
                        File.Move($"{flash_root}/E_INFO/uudis6.{type}", $"{flash_root}/E_INFO/arhiiv/uudis{DateTime.Now.DayOfYear}{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}.{type}");
                        no_archives = false;
                    }
                }
                if (no_archives)
                {
                    await MessageBoxShow("Ei leitud uudised, mida oleks vaja arhiveerida.", "Uudise arhiveerimine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                }
            }
        }

        private void News_Archive_Click(object? sender, RoutedEventArgs e)
        {
            new Process()
            {
                StartInfo = new()
                {
                    UseShellExecute = true,
                    FileName = $"{flash_root}/E_INFO/arhiiv"
                }
            }.Start();
        }

        private async void Apply_Doc_Changes(object? sender, RoutedEventArgs e)
        {
            if (File.Exists($"{flash_root}/E_INFO/uudis6.docx"))
            {
                if (await MessageBoxShow("Enne uudise lisamist tuleb üks uudis arhiveerida. Kas soovite seda kohe teha?", "Uudise lisamine", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning) == MsBox.Avalonia.Enums.ButtonResult.Yes)
                {
                    string s = flash_root + "/E_INFO/arhiiv/uudis" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".docx";
                    File.Move($"{flash_root}/E_INFO/uudis6.docx", s);
                } else
                {
                    await MessageBoxShow("Ei saanud uudist lisada.", "Uudise lisamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                    return;
                }
            }
            File.Move(flash_root + "/E_INFO/uudis5.docx", flash_root + "/E_INFO/uudis6.docx");
            File.Move(flash_root + "/E_INFO/uudis4.docx", flash_root + "/E_INFO/uudis5.docx");
            File.Move(flash_root + "/E_INFO/uudis3.docx", flash_root + "/E_INFO/uudis4.docx");
            File.Move(flash_root + "/E_INFO/uudis2.docx", flash_root + "/E_INFO/uudis3.docx");
            File.Move(flash_root + "/E_INFO/uudis1.docx", flash_root + "/E_INFO/uudis2.docx");

            //salvestab uue uudise
            //teisendab teksti baitideks, et vältida probleeme kasutuseloleva failiga
            string path = flash_root + "/E_INFO/uudis9.docx";
            //rtb?.SaveAsWord(path);
            rtb = null;
            File.Move(flash_root + "/E_INFO/uudis9.docx", flash_root+ "/E_INFO/uudis1.docx");

            //eemaldab ebavajaliku uudise mälust
            rtb = new RichTextBox();

            //annab kasutajale teada, et kõik õnnestus
            await MessageBoxShow("Andmed salvestati edukalt. Programm värskendab nüüd andmeid...", "Arendamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
            ReloadData(sender, e);
        }

        // Reimplementation of WinForms MessageBox.Show
        internal Task<MsBox.Avalonia.Enums.ButtonResult> MessageBoxShow(string message, string caption = "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None, WindowStartupLocation spawn = WindowStartupLocation.CenterOwner)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon, spawn);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }

    }
}