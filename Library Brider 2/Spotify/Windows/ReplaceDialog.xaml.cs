using SpotifyAPI.Web.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Library_Brider_2.Spotify.Windows
{
    /// <summary>
    /// Interaction logic for ReplaceDialog.xaml
    /// </summary>
    public partial class ReplaceDialog : Window
    {
        public FullTrack returnTrack;
        public ReplaceDialog(SearchItem search_result)
        {
            List<ReplacementTrack> list = new List<ReplacementTrack>();
            InitializeComponent();
            foreach (FullTrack track in search_result.Tracks.Items)
            {
                ReplacementTrack repTrack = new ReplacementTrack
                {
                    ImagePath = track.Album.Images[0].Url,
                    Name = track.Artists[0].Name + " - " + track.Name,
                    Spot_track = track
                };
                list.Add(repTrack);
            }
            replaceTrackBox.ItemsSource = list;
        }

        private class ReplacementTrack
        {
            public string ImagePath { get; set; }
            public string Name { get; set; }
            public FullTrack Spot_track { get; set; }
        }

        private void SwitchBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (switchBox.SelectedItem != null)
            {
                returnTrack = ((ReplacementTrack)switchBox.SelectedItem).Spot_track;
            }
            this.DialogResult = true;
        }
    }
}
