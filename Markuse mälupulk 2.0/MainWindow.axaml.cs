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

namespace Markuse_mälupulk_2._0
{
    public partial class MainWindow : Window
    {
        string mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";   // mas root directory
        Color[] scheme = [Color.FromRgb(255, 255, 255), Color.FromRgb(0,0,0)];                          // default color scheme
        string flash_root = "";                                                                         // flash drive root directory
        bool testing = true;                                                                            // avoid loading content when we're in axaml view
        List<double> sts = new List<double>();
        List<string> list = new List<string>();
        bool canContinue = true;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowModel();
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
            if (!testing)
            {
                SelectDrive sd = new();
                sd.parent = this;
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
            DispatcherTimer waitForExit = new();
            waitForExit.Interval = new TimeSpan(0, 0, 1);
            waitForExit.Tick += (object? sender, EventArgs e) =>
            {
                if (list.Count >= 6)
                {
                    Dictionary<string, int> stats= [];
                    int sum = 0;
                    string[] requiredFields = ["Markuse asjad", "Operatsioonsüsteemid", "Pakkfailid", "Kiirrakendused", "PlayStation mängud"];
                    for (int i = 0; i < list.Count; i++)
                    {
                        switch (list[i])
                        {
                            case "markuse asjad":
                                if (!stats.ContainsKey("Markuse asjad"))
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
                                if (!stats.ContainsKey("PlayStation mängud"))
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
            DispatcherTimer dpt = new DispatcherTimer();
            dpt.Interval = new TimeSpan(0, 0, 0, 0, 20);
            dpt.Tick += (object? sender, EventArgs e) =>
            {
                if (CollectProgress.Value == CollectProgress.Maximum)
                {
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
                            foreach (FileInfo fi in new DirectoryInfo(flash_root + "/Markuse_videod").GetFiles())
                            {
                                if (fi.Name.Substring(1, 1) == ".")
                                {
                                    VideoBox.Items.Add(string.Join('.', fi.Name.Split('.').Skip(1))[1..]);
                                }
                            }
                            break;
                        case 5:
                            //NewsBox.LoadRtfDoc(flash_root + "/E_INFO/uudis1.rtf");
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
                            EditionLabel.Content = "Väljaanne: " + edition;
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
                            new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/markuse asjad/markuse asjad"))).Start();
                            new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/multiboot"))).Start();
                            new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/sources"))).Start();
                            new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/Pakkfailid"))).Start();
                            new Thread(() => GetDirSize(new DirectoryInfo(flash_root + "/markuse asjad/Kiirrakendused"))).Start();
                            new Thread(() => GetGameSize()).Start();
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

        public void GetGameSize()
        {
            long gamedirsize = (long)0;
            gamedirsize += DirSize(new DirectoryInfo(flash_root + "/POPS"));
            gamedirsize += DirSize(new DirectoryInfo(flash_root + "/CD"));
            gamedirsize += DirSize(new DirectoryInfo(flash_root + "/DVD"));
            sts.Add(gamedirsize);
            list.Add("PlayStation mängud");
        }

        public void GetDirSize(DirectoryInfo d)
        {
            sts.Add(DirSize(d));
            list.Add(d.Name);
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

        private void LoadTheme()
        {
            if (File.Exists(mas_root + "/bg_common.png"))
            {
                ImageBrush ib = new()
                {
                    Source = new Bitmap(mas_root + "/bg_common.png"),
                    Stretch = Stretch.UniformToFill
                };
                TopPanel.Background = ib;
            }
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
            TestRtb();
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
                //MessageBox.Show(ex.Message, "Tundub, et rakenduse käivitumine ebaõnnestus. Põhjus on järgmine:", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                //MessageBox.Show(ex.Message, "Tundub, et rakenduse kustutamine ebaõnnestus. Põhjus on järgmine:", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Button? me = sender as Button;
            if (me != null)
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


        // Reimplementation of WinForms MessageBox.Show
        private Task<MsBox.Avalonia.Enums.ButtonResult> MessageBoxShow(string message, string caption = "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }

        private void HelpLink_Click(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            MainWindowModel? mwm = (MainWindowModel?)this.DataContext;
            if (mwm != null)
            {
                _ = MessageBoxShow(mwm.info, "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Question);
            }
        }
    }
}