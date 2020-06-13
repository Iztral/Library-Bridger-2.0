using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using SpotifyAPI.Web;

namespace Library_Brider_2.Spotify.Windows
{
    /// <summary>
    /// Interaction logic for ReplaceDialog.xaml
    /// </summary>
    public partial class ReplaceDialog : Window
    {
        public FullTrack returnTrack;

        private class ReplacementTrack
        {
            public string ImagePath { get; set; }
            public string Name { get; set; }
            public FullTrack SpotifyTrack { get; set; }
        }

        public ReplaceDialog(SearchResponse search_result)
        {
            List<ReplacementTrack> list = new List<ReplacementTrack>();
            InitializeComponent();
            foreach (FullTrack track in search_result.Tracks.Items)
            {
                ReplacementTrack repTrack = new ReplacementTrack
                {
                    ImagePath = track.Album.Images[0].Url,
                    Name = track.Artists[0].Name + " - " + track.Name,
                    SpotifyTrack = track
                };
                list.Add(repTrack);
            }
            switchBox.ItemsSource = list;
        }

        private void SwitchBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (switchBox.SelectedItem != null)
            {
                returnTrack = ((ReplacementTrack)switchBox.SelectedItem).SpotifyTrack;
            }
            this.DialogResult = true;
        }
    }
}
