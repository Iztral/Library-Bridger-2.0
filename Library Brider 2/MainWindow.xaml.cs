using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Library_Brider_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PickServiceWindow : Window
    {
        public PickServiceWindow()
        {
            InitializeComponent();
        }

        private void SpotifyButton_Click(object sender, RoutedEventArgs e)
        {
            Spotify.Windows.SpotifyWindow spotifyWindow = new Spotify.Windows.SpotifyWindow();
            spotifyWindow.Show();
            this.Close();
        }
    }
}
