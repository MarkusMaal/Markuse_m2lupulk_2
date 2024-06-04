using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Markuse_mälupulk_2_0
{
    public partial class Parool : Window
    {
        public string currentpin = "";
        public bool result = false;
        public Parool()
        {
            InitializeComponent();
            LabelUpdate();
        }

        private void OK_Click(object? sender, RoutedEventArgs e)
        {
            result = true;
            this.Close();
        }

        private void Number_Click(object? sender, RoutedEventArgs e)
        {
            Button? me = sender as Button;
            if ((me != null) && (me.Content != null))
            {
                string? num = me.Content.ToString();
                currentpin += num;
                LabelUpdate();
            }
        }

        private void Backspace_Click(object? sender, RoutedEventArgs e)
        {
            if (currentpin.Length > 0)
            {
                currentpin = currentpin[..^1];
            }
            LabelUpdate();
        }

        void LabelUpdate()
        {
            TopBox.Text = "";
            for (int i = 0; i < currentpin.Length; i++)
            {
                TopBox.Text += "*";
            }
        }
    }
}
