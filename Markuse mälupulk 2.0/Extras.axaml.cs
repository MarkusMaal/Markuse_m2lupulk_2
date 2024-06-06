using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Markuse_mälupulk_2._0;
using System.Threading;

namespace Markuse_mälupulk_2_0
{
    public partial class Extras : Window
    {
        internal MainWindow? parent = null;
        public Extras()
        {
            InitializeComponent();
        }

        private void Extra_Tip(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (sender is not Button me)
            {
                return;
            }
            string? name = me.Content?.ToString();
            if (name == null)
            {
                return;
            }
            string description = "";
            switch (name)
            {
                case "Mälupulga pakkfail":
                    description = "Väga vana mälupulga haldamise programm, mille põhifunktsiooniks on " +
                                  "uudiste lugemine ja uute videote vaatamine.";
                    SetImage("Pakkfail");
                    SetPanel(Stretch.None, new SolidColorBrush(Color.FromRgb(12, 12, 12)));
                    break;
                case "Turvarežiim":
                    description = "Käivitab mälupulga haldamise programmi minimaalses režiimis ainult " +
                                  "uudiste lugemiseks. Programmi käivitamiseks turvarežiimis ilma tavarežiimita" +
                                  "kasutage käivitamisel argumenti --safemode";
                    SetImage("Turvarežiim");
                    SetPanel(Stretch.None, new SolidColorBrush(Colors.White));
                    break;
                case "Juhpaneeli pärandversioon":
                    description = "Vanem programm mälupulga haldamiseks, mis oli ühtlasi ka mälupulga " +
                                  "juhtpaneeli eelkäijaks. Mõned inimesed võivad eelisata selle programmi " +
                                  "liidest rohkem. Seetõttu lisasin ta siia lisafunktsioonide alla.";
                    SetImage("Universaalprogramm");
                    SetPanel(Stretch.None, new SolidColorBrush(GetEditionColor()));
                    break;
                case "Teema kohandamine":
                    description = "Juhul kui käivitasite selle programmi Markuse arvutis, saate teemat käsitsi kohandada " +
                                  "Markuse arvuti juhtpaneelis. See valik on mõeldud olukordadeks, kus seda programmi ei " +
                                  "kasutata Markuse arvutis.";
                    SetImage("Muster");
                    SetPanel(Stretch.UniformToFill, new SolidColorBrush(Colors.Transparent));
                    break;
            }
            TipTitle.Text = name;
            TipText.Text = description;
        }

        private Color GetEditionColor()
        {
            Color r = Colors.Black;
            if (((SolidColorBrush?)parent?.EditionBox.Fill) == null)
            {
                return r;
            }
            if (((SolidColorBrush)parent.EditionBox.Fill).Color != Colors.LimeGreen)
            {
                return ((SolidColorBrush)parent.EditionBox.Fill).Color;
            } else
            {
                return Colors.Green;
            }
        }

        private void SetImage(string tag)
        {
            if (Application.Current?.Resources[tag] is not Image img)
            {
                return;
            }
            TipImage.Source = img.Source;
        }

        private void SetPanel(Stretch s, Brush brush)
        {
            TipImage.Stretch = s;
            TipImagePanel.Background = brush;
        }

        private void Extra_Reset(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            TipTitle.Text = "Lisainfo funktsiooni kohta";
             TipText.Text = "Lisanduv info teatud funktsiooni kohta ilmub siia," +
                            "kui viite kursori kindla nupu peale.";
            SetImage("Info");
            SetPanel(Stretch.None, new SolidColorBrush(Colors.Transparent));
        }

        private async void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not Button me)
            {
                return;
            }
            string? name = me.Content?.ToString();
            if (name == null)
            {
                return;
            }
            switch (name)
            {
                case "Teema kohandamine":
                    ManageTheme mt = new()
                    {
                        Background = this.Background,
                        Foreground = this.Foreground,
                        parent = parent,
                    };
                    if (parent != null)
                    {
                        mt.GradientsCheck.IsChecked = parent.GradientBg.Background?.Opacity > 0;
                    }
                    this.Opacity = 0;
                    await mt.ShowDialog(this).WaitAsync(CancellationToken.None);
                    this.Close();
                    break;
            }
        }
    }
}
