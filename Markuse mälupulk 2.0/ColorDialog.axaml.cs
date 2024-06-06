using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Markuse_mälupulk_2_0;

public partial class ColorDialog : Window
{
    public bool result = false;
    public ColorDialog()
    {
        InitializeComponent();
    }

    private void Confirm(object? sender, RoutedEventArgs e)
    {
        result = true;
        this.Close();
    }
}