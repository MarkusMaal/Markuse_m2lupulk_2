using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markuse_mälupulk_2._0;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Markuse_mälupulk_2_0
{
    public partial class SelectDrive : Window
    {
        public bool exit = true;
        public MainWindow? parent;

        bool initialized = false;
        bool testing = true;

        public SelectDrive()
        {
            InitializeComponent();

            var dataSource = new ObservableCollection<string[]>();
            foreach(Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains("Markuse mälupulk"))
                {
                    testing = false;
                    break;
                }
            }
            if (!testing)
            {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    string flash_root = di.RootDirectory.FullName;
                    if (File.Exists(flash_root + "/E_INFO/edition.txt"))
                    {
                        dataSource.Add([flash_root, GetFriendlySize(di.TotalSize), GetUkssVersion(flash_root)]);
                    }
                }
                DriveList.Columns.Add(new DataGridTextColumn { Header = "Asukoht", Binding = new Binding($"[{0}]") });
                DriveList.Columns.Add(new DataGridTextColumn { Header = "Maht", Binding = new Binding($"[{1}]") });
                DriveList.Columns.Add(new DataGridTextColumn { Header = "UKSS versioon", Binding = new Binding($"[{2}]") });
                DriveList.ItemsSource = dataSource;
            }
        }

        private static string GetUkssVersion(string root)
        {
            string ver = "Pole saadaval";
            if (Directory.Exists(root + "/markuse asjad/Kasutajad") && ((new DirectoryInfo(root + "/markuse asjad/Kasutajad").Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint))
            {
                ver = "v2.0";
            }
            else if (Directory.Exists(root + "/markuse asjad/Kiirrakendused") && Directory.Exists(root + "/markuse asjad/Mine") && Directory.Exists(root + "/markuse asjad/markuse asjad") && Directory.Exists(root + "/Markuse_videod"))
            {
                ver = "v1.1a";
            }
            else if (Directory.Exists(root + "/markuse asjad/Kiirrakendused") && Directory.Exists(root + "/Markuse_videod") && Directory.Exists(root + "/E_INFO"))
            {
                ver = "v1.1b";
            }
            else if (Directory.Exists(root + "/markuse asjad/markuse asjad/markus1") && Directory.Exists(root + "/markuse asjad/Uusimad videod") && Directory.Exists(root + "/markuse asjad/markuse asjad") && Directory.Exists(root + "/markuse asjad/Abi ja info/Abi ja toe keskus") && File.Exists(root + "/markuse asjad/Abi ja info/Uudised.lnk"))
            {
                ver = "v1.0";
            }
            return ver;
        }

        public string GetFriendlySize(long capacity, bool ignoreGib = false)
        {
            double decimalSize;
            double binarySize;
            string unit;
            if (capacity < 1000)
            {
                return string.Format("{0} B", capacity);
            }
            else if (capacity < 1000000)
            {
                unit = "k";
                binarySize = capacity / 1024.0;
                decimalSize = capacity / 1000.0;
            }
            else if (capacity < 1000000000)
            {
                unit = "M";
                binarySize = capacity / 1048576;
                decimalSize = capacity / 1000000.0;
            }
            else if (capacity < 1000000000000)
            {
                unit = "G";
                binarySize = capacity / 1073741824.0;
                decimalSize = capacity / 1000000000.0;
            }
            else if (capacity < 1000000000000000)
            {
                unit = "T";
                binarySize = capacity / 1099511627776.0;
                decimalSize = capacity / 1000000000000.0;
            }
            else if (capacity < 1000000000000000000)
            {
                unit = "P";
                binarySize = capacity / 1125899906842624.0;
                decimalSize = capacity / 10000001000000000.0;
            }
            else
            {
                unit = "E";
                binarySize = capacity / 1152921504606846976.0;
                decimalSize = capacity / 1000000000000000000.0;
            }
            if (!ignoreGib)
            {
                return string.Format("{0} {1}B ({2} {1}iB)", Math.Round(decimalSize, 2), unit, Math.Round(binarySize, 2));
            } else
            {
                return string.Format("{0} {1}B", Math.Round(decimalSize, 2), unit);
            }
        }

        private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Position = new PixelPoint(parent.Position.X + ((int)parent.Width / 2) - ((int)this.Width / 2), parent.Position.Y + ((int)parent.Height / 2) - ((int)this.Height / 2));
            initialized = true;
        }

        private void Window_PositionChanged(object? sender, Avalonia.Controls.PixelPointEventArgs e)
        {
            if ( initialized)
            {
                parent.Position = new PixelPoint(this.Position.X - (int)parent.Width / 2 + (int)this.Width / 2, this.Position.Y - (int)parent.Height / 2 + (int)this.Height / 2);
            }
        }

        private void Window_SizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
        {
            if (initialized)
            {
                parent.Position = new PixelPoint(this.Position.X - (int)parent.Width / 2 + (int)this.Width / 2, this.Position.Y - (int)parent.Height / 2 + (int)this.Height / 2);
            }
        }

        private void Exit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DriveList.SelectedItems.Count > 0)
            {
                exit = false;
                this.Close();
            }
        }

        private void DataGrid_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            ConfirmButton.IsEnabled = DriveList.SelectedItems.Count > 0;
        }
    }
}
