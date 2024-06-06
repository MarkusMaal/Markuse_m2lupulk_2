using Avalonia.Controls;
using Avalonia.Media;
using Markuse_mälupulk_2._0;
using Avalonia;
using ScottPlot;
using Avalonia.Interactivity;

namespace Markuse_mälupulk_2_0
{
    public partial class ManageTheme : Window
    {
        internal MainWindow? parent = null;
        public ManageTheme()
        {
            InitializeComponent();
        }

        private void GradientCheck_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (parent == null) { return; }
            if (parent.GradientBg.Background is not LinearGradientBrush lb) { return; }
            if (GradientsCheck.IsChecked ?? false)
            {
                lb.Opacity = 1;
                parent.GradientBg.Background = lb;
            } else
            {
                lb.Opacity = 0;
                parent.GradientBg.Background = lb;
            }
        }

        private async void ChangeBgCol(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (parent == null) { return; }
            SolidColorBrush? bgcol = ((SolidColorBrush?)parent?.Background);
            if (bgcol == null) { return; }
            ColorDialog cpd = new();
            cpd.Color.Color = bgcol.Color;
            await cpd.ShowDialog(this);
            if (cpd.result && (parent != null))
            {
                parent.Background = new SolidColorBrush(cpd.Color.Color);
                this.Background = new SolidColorBrush(cpd.Color.Color);
                LinearGradientBrush? lgb = (LinearGradientBrush?)parent.GradientBg.Background;
                if (lgb != null)
                {
                    lgb.GradientStops[1].Color = cpd.Color.Color;
                }
                parent.scheme[0] = cpd.Color.Color;
            }
        }

        private async void ChangeFgCol(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (parent == null) { return; }
            SolidColorBrush? fgcol = ((SolidColorBrush?)parent?.Foreground);
            if (fgcol == null) { return; }
            ColorDialog cpd = new();
            cpd.Color.Color = fgcol.Color;
            await cpd.ShowDialog(this);
            if (cpd.result && (parent != null))
            {
                parent.Foreground = new SolidColorBrush(cpd.Color.Color);
                this.Foreground = new SolidColorBrush(cpd.Color.Color);
                parent.scheme[1] = cpd.Color.Color;
            }
        }

        private void SyncScheme(object? sender, RoutedEventArgs e)
        {
            
        }
    }
}
