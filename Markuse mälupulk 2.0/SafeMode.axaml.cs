using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Markuse_mälupulk_2._0;
using MsBox.Avalonia;

//
// Markuse mälupulk
// Turvarežiim
//
// Funktsionaalsuse lisamine ainult äärmuslikel tingimustel, nii
// palju kui vaja, nii vähe kui võimalik
//

namespace Markuse_mälupulk_2_0
{
    public partial class SafeMode : Window
    {
        readonly List<DriveInfo> drives;
        DriveInfo? currentDevice;
        int selectedIdx;
        int selectedArticle;
        string drive;
        private readonly bool testing;


        public SafeMode()
        {
            // define default values
            drives = [];
            selectedIdx = 0;
            drive = "";
            selectedArticle = 1;

            // check if we're actually running the application, if not, just display the form with no data
            // (the latter is true if we're in the designer)
            testing = true;
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains("Markuse mälupulk")) 
                {
                    testing = false;
                    break;
                }
            }

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if (testing) {
                return;
            }
            foreach (DriveInfo di in DriveInfo.GetDrives()) {
                if (Directory.Exists(di.RootDirectory + "/E_INFO")) {
                    drives.Add(di);
                }
            }
            selectedIdx = 0;
            currentDevice = drives[selectedIdx];
            drive = currentDevice.RootDirectory.Name;
            if (currentDevice.VolumeLabel.ToString() != "") {
                DeviceName.Content = currentDevice.VolumeLabel.ToString();
            } else {

                if (!File.Exists(currentDevice.RootDirectory + "/autorun.inf"))
                {
                    DeviceName.Content = "Sildita irdseade";
                }
                else
                {
                    string[] autoruninf = File.ReadAllText(currentDevice.RootDirectory + "/autorun.inf", Encoding.GetEncoding("windows-1252")).Replace("\r\n", "\n").Split('\n');
                    foreach (string str in autoruninf)
                    {
                        if (str.Contains("label=", StringComparison.CurrentCultureIgnoreCase))
                        {
                            string label = str.Split('=')[1];
                            DeviceName.Content = label;
                        }
                    }
                }
                NextDeviceButton.IsVisible = drives.Count > 0;
                DeviceName.Content += " (" + drive + ")";
                ReloadData();
            }
        }
        void ReloadData()
        {
            ReloadArticle();
            VideoBox.Items.Clear();
            if (currentDevice == null) {
                return;
            }
            if (Directory.Exists(currentDevice.RootDirectory + "/Markuse_videod"))
            {
                foreach (FileInfo fi in new DirectoryInfo(currentDevice.RootDirectory + "/Markuse_videod").GetFiles())
                {
                    string file = fi.Name;
                    if (fi.Name.Contains(". "))
                    {
                        VideoBox.Items.Add(string.Concat(file.Split('.')[1].AsSpan(1), ".", file.Split('.')[2]));
                    }
                }
                PlayButton.IsEnabled = false;
                VideoBox.IsEnabled = true;
            }
            else
            {
                VideoBox.Items.Add("Pole saadaval");
            }
        }

        public void LoadDoc(string filename)
        {
            if (currentDevice == null) {
                return;
            }
            if (File.Exists(currentDevice.RootDirectory + "/E_INFO/" + filename.Replace(".rtf", ".docx")))
            {
                NewsBox.CloseDocument();
                NewsBox.LoadWordDoc(currentDevice.RootDirectory + "/E_INFO/" + filename.Replace(".rtf", ".docx"));
                NewsBox.FlowDoc.PagePadding = new Thickness(0);
            }
        }

        void ReloadArticle()
        {
            try
            {
                LoadDoc($"uudis{selectedArticle}.rtf");
            }
            catch
            {
                //richTextBox1.Text = "Viga artikli laadimisel";
            }
            ArticleLabel.Content = "Artikkel " + selectedArticle.ToString() + "/5";
        }

        private void NextDev_Click(object sender, RoutedEventArgs e) {

            NewsBox.IsEnabled = false;
            selectedIdx++;
            if (selectedIdx >= drives.Count)
            {
                selectedIdx = 0;
            }
            currentDevice = drives[selectedIdx];
            drive = currentDevice.RootDirectory.Name;
            try
            {
                if (currentDevice.VolumeLabel.ToString() != "")
                {
                    DeviceName.Content = currentDevice.VolumeLabel.ToString();
                }
                else
                {
                    if (!File.Exists(currentDevice.RootDirectory + "autorun.inf"))
                    {
                        DeviceName.Content = "Sildita irdseade";
                    }
                    else
                    {
                        string[] autoruninf = File.ReadAllText(currentDevice.RootDirectory + "autorun.inf", Encoding.GetEncoding("windows-1252")).Replace("\r\n", "\n").Split('\n');
                        foreach (string str in autoruninf)
                        {
                            if (str.ToLower().Contains("label="))
                            {
                                string label = str.Split('=')[1];
                                DeviceName.Content = label;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBoxShow(ex.Message, "Ei saa seadet muuta", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                DeviceName.Content = "Nimetu irdseade";
            }
            DeviceName.Content  += " (" + drive + ")";
            ReloadData();
        }
        
        private void PlayButton_Click(object sender, RoutedEventArgs e) {
            try
            {
                Process p = new()
                {
                    StartInfo = new()
                    {
                        FileName = currentDevice.RootDirectory + "/Markuse_videod/" + (VideoBox.SelectedIndex + 1).ToString() + ". " + VideoBox.SelectedItem.ToString(),
                        UseShellExecute = true
                    }
                };
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBoxShow(ex.Message, "Viga", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            PlayButton.IsEnabled = VideoBox.SelectedItems?.Count > 0;
        }

        private void PreviousArticle_Click(object sender, RoutedEventArgs e) {
            selectedArticle--;
            if (selectedArticle < 1) {
                selectedArticle = 5;
            }
            ReloadArticle();
        }

        private void NextArticle_Click(object sender, RoutedEventArgs e) {
            selectedArticle++;
            if (selectedArticle > 5) {
                selectedArticle = 1;
            }
            ReloadArticle();
        }

        private void RefreshArticle_Click(object sender, RoutedEventArgs e) {
            ReloadArticle();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e) {
            VideoBox.IsEnabled = false;
            PlayButton.IsEnabled = false;
            drives.Clear();
            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                if (Directory.Exists(di.RootDirectory + "/E_INFO"))
                {
                    drives.Add(di);
                }
            }
            selectedIdx = 0;
            currentDevice = drives[selectedIdx];
            drive = currentDevice.RootDirectory.Name;
            DeviceName.Content = "Nimetu irdseade";
            if (currentDevice.VolumeLabel.ToString() != "")
            {
                DeviceName.Content = currentDevice.VolumeLabel.ToString();
            }
            NextDeviceButton.IsVisible = drives.Count > 1;
            DeviceName.Content += " (" + drive + ")";
            ReloadData();
        }

        private void NormalMode_Click(object sender, RoutedEventArgs e) {
            MainWindow mw = new();
            mw.Show();
            this.Close();
        }

        private void NewsBox_DoubleTapped(object sender, TappedEventArgs e) {
            if (SidePanel.Width > 0) {
                SidePanel.Width = 0;
            } else {
                SidePanel.Width = Width / 2;
            }
        }

        // Reimplementation of WinForms MessageBox.Show
        private Task<MsBox.Avalonia.Enums.ButtonResult> MessageBoxShow(string message, string caption = "Markuse mälupulk", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None, WindowStartupLocation spawn = WindowStartupLocation.CenterOwner)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon, spawn);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }
    }
}
