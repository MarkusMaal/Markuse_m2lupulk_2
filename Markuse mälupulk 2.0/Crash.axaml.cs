using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Markuse_mälupulk_2._0;

public partial class Crash : Window
{
    public Crash()
    {
        InitializeComponent();
    }

    private void ButtonClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private void ButtonReset_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Program.Restart();
    }

    private void ButtonResetSafe_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Program.RestartSafe();
    }
}