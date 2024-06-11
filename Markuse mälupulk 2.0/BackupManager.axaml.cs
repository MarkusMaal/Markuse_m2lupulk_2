using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Markuse_mälupulk_2._0;
using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading;

namespace Markuse_mälupulk_2_0;

public partial class BackupManager : Window
{
    private readonly Thread? backup;
    private readonly Thread? restore;
    internal string? dest_drive;
    internal MainWindow? parent;
    private string? folder;
    private string currentfile;
    private bool finished = true;
    private int fileCount = 0;
    private int progress = 0;
    private readonly DispatcherTimer checkStatusTimer;
    bool progressSetup = false;
    internal bool isSimulation;

    public BackupManager()
    {
        backup = new Thread(() =>
        {
            currentfile = "Loendamine...";
            CountDirs(dest_drive ?? "");
            progressSetup = true;
            DirectoryCopy(dest_drive ?? "", folder ?? "", true);
            finished = true;
        });
        restore = new Thread(() =>
        {
            currentfile = "Loendamine...";
            CountDirs(folder ?? "");
            progressSetup = true;
            DirectoryCopy(folder ?? "", dest_drive ?? "", true);
            finished = true;
        });
        checkStatusTimer = new();
        currentfile = "";
        InitializeComponent();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (finished)
        {
            parent?.Show();
        }
        else
        {
            e.Cancel = true;
        }
    }

    private void CountDirs(string dirName)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new(dirName);

        if (dir.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return;
        }
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Directory does not exist or could not be found: "
                + dirName);
        }

        DirectoryInfo[] dirs;

        try
        {
            dirs = dir.GetDirectories();
        } catch
        {
            return;
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        fileCount += files.Length;
        foreach (DirectoryInfo subdir in dirs)
        {
            if (subdir.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }
            try
            {
                fileCount += subdir.GetFiles().Length;
            } catch { }
            CountDirs(subdir.FullName);
        }
    }
    

    private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs;
        try
        {
            dirs = dir.GetDirectories();
        } catch
        {
            return;
        }

        // If the destination directory doesn't exist, create it.       
        if (!isSimulation)
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            try
            {
                
                currentfile = file.Name;
                if (!isSimulation)
                {
                    file.CopyTo(tempPath, false);
                    _ = new FileInfo(tempPath)
                    {
                        Attributes = file.Attributes,
                        LastAccessTime = file.LastAccessTime,
                        LastWriteTime = file.LastWriteTime,
                        CreationTime = file.CreationTime,
                        IsReadOnly = file.IsReadOnly
                    };
                } else
                {
                    Thread.Sleep(100);
                }
                progress++;
            }
            catch { }
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                if (subdir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e) // Taaste!
    {
        finished = false;
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }
        // Start async operation to open the dialog.
        var dir = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Vali kataloog, kust varukoopia taastada",
        });

        if ((dir is not null) && (dir.Count > 0))
        {
            finished = false;
            folder = Uri.UnescapeDataString(dir[0].Path.AbsolutePath);
            Buttons.IsVisible = false;
            StatusPanel.IsVisible = true;
            StatusTopText.Content = "Taastamine. Palun oota...";
            checkStatusTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            CopyProgress.IsIndeterminate = true;
            checkStatusTimer.Tick += (object? sender, EventArgs e) =>
            {
                if (!finished)
                {
                    if (progressSetup)
                    {
                        CopyProgress.IsIndeterminate = false;
                        CopyProgress.Maximum = fileCount;
                        CurrentFile.Content = $"Praegune fail: {currentfile}";
                    }
                    else
                    {
                        CurrentFile.Content = $"Loendamine... ({fileCount} faili leitud)";
                    }
                    CopyProgress.Value = progress;
                }
                else
                {
                    StatusPanel.IsVisible = false;
                    Buttons.IsVisible = true;
                    checkStatusTimer.IsEnabled = false;
                }
            };
            restore?.Start();
            checkStatusTimer.Start();
        }
    }

    private async void Backup_Click(object? sender, RoutedEventArgs e) // Varundamine!
    {
        finished = false;
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }
        // Start async operation to open the dialog.
        var dir = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Vali kataloog, kuhu varukoopia salvestada",
        });

        if ((dir is not null) && (dir.Count > 0))
        {
            finished = false;
            folder = Uri.UnescapeDataString(dir[0].Path.AbsolutePath);
            Buttons.IsVisible = false;
            StatusPanel.IsVisible = true;
            StatusTopText.Content = "Varundamine. Palun oota...";
            checkStatusTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            CopyProgress.IsIndeterminate = true;
            checkStatusTimer.Tick += (object? sender, EventArgs e) =>
            {
                if (!finished)
                {
                    if (progressSetup)
                    {
                        CopyProgress.IsIndeterminate = false;
                        CopyProgress.Maximum = fileCount;
                        CurrentFile.Content = $"Praegune fail: {currentfile}";
                    } else
                    {
                        CurrentFile.Content = $"Loendamine... ({fileCount} faili leitud)";
                    }
                    CopyProgress.Value = progress;
                } else
                {
                    StatusPanel.IsVisible = false;
                    Buttons.IsVisible = true;
                    checkStatusTimer.IsEnabled = false;
                }
            };
            backup?.Start();
            checkStatusTimer.Start();
        }
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        isSimulation = parent?.SimulationCheck.IsChecked ?? false;
        if (isSimulation)
        {
            this.Title += " // SIMULATSIOON //";
        }
    }
}