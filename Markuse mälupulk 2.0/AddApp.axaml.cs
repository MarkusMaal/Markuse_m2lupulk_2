using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Markuse_mälupulk_2._0;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Joins;
using System.Text;

namespace Markuse_mälupulk_2_0
{
    public partial class AddApp : Window
    {
        public bool finished = false;
        public string? app = null;

        internal string flash_root = "";
        public AddApp()
        {
            InitializeComponent();
        }

        private void NextPage(object? sender, RoutedEventArgs e)
        {
            if (Page1.IsVisible)
            {
                if (OnFlashCheck.IsChecked ?? false)
                {
                    AllocateFolders();
                } else
                {
                    CopyFolderFiles();
                }
                Page1.IsVisible = false;
                Page2.IsVisible = true;
            } else if (Page2.IsVisible)
            {
                SetAttribs();
                Page2.IsVisible = false;
                Page3.IsVisible = true;
            }
        }

        private void SetAttribs()
        {
            this.app = this.listBox1.SelectedItem?.ToString();
            this.AppNameLabel.Content = "Rakenduse nimi: " + this.app;
            if (File.Exists(flash_root + "/markuse asjad/Kiirrakendused/" + this.app + "/" + this.app + "Portable.exe"))
                this.AppStartupExecutable.Content = "Käivitatav programm: " + this.app + "Portable.exe";
            else
                this.AppNameLabel.Content = "Käivitatav programm pole määratud";
            try
            {
                this.AppDescription.Text = File.ReadAllText(flash_root + "/markuse asjad/Kiirrakendused/" + this.app + "/" + this.app + "Info.txt", Encoding.Unicode);
            }
            catch
            {
            }
            try
            {
                this.AppPreview.Source = new Bitmap(flash_root + "/markuse asjad/Kiirrakendused/" + this.app + "/" + this.app + "ScreenShot.bmp");
            }
            catch
            {
            }
        }

        private async void CopyFolderFiles()
        {
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }
            // Start async operation to open the dialog.
            var dir = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Vali kataloog",
            });

            if ((dir is not null) && (dir.Count > 0))
            {
                DirectoryCopy(dir[0].Path.AbsolutePath, flash_root + "/markuse asjad/Kiirrakendused/" + dir[0].Path.AbsolutePath.Split('/')[^1], true);
                AllocateFolders();
            }
        }


        void AllocateFolders()
        {
            listBox1.Items.Clear();
            foreach (DirectoryInfo di in new DirectoryInfo(flash_root + "/markuse asjad/Kiirrakendused").GetDirectories())
            {
                listBox1.Items.Add(di.Name);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                try
                {
                    file.CopyTo(tempPath, false);
                }
                catch { }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private async void SetStartupExec_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }
            // Start async operation to open the dialog.
            FilePickerFileType ExeType = new("Executables")
            { 
                Patterns = [ "*.exe", "*.appimage"]
            };
    
            var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vali käivitatav programm",
                FileTypeFilter = [ExeType, FilePickerFileTypes.All]
            });

            if (file is not null)
            {
                try
                {
                    File.Copy(file[0].Path.AbsolutePath, flash_root + "/markuse asjad/Kiirrakendused/" + app + "/" + app + "Portable.exe", true);
                    AppNameLabel.Content = "Käivitatav programm: " + app + "Portable.exe";
                }
                catch
                {

                }
            }
       }

        private async void UpdateAppPreview_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }
            // Start async operation to open the dialog.
            FilePickerFileType BmpType = new("Bitirasteri formaadis kujutised")
            {
                Patterns = ["*.bmp"]
            };

            var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vali kuvatõmmis",
                FileTypeFilter = [BmpType]
            });

            if (file is not null)
            {
                AppPreview.Source = null;
                
                File.Copy(Uri.UnescapeDataString(file[0].Path.AbsolutePath), flash_root + "/markuse asjad/Kiirrakendused/" + this.app + "/" + this.app + "ScreenShot.bmp", true);
                AppPreview.Source = new Bitmap(flash_root + "/markuse asjad/Kiirrakendused/" + this.app + "/" + this.app + "ScreenShot.bmp");
            }
        }

        private void Finish_Setup(object? sender, RoutedEventArgs args)
        {
            File.WriteAllText(flash_root + "/markuse asjad/Kiirrakendused/" + app + "/" + app + "Info.txt", AppDescription.Text, Encoding.Unicode);
            finished = true;
            this.Close();
        }
    }
}
