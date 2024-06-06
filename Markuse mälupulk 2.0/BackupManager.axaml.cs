using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Markuse_mälupulk_2._0;

namespace Markuse_mälupulk_2_0;

public partial class BackupManager : Window
{
    internal string dest_drive;
    internal MainWindow parent;

    public BackupManager() => InitializeComponent();

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        parent.Show();
    }
}