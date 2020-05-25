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
using System.Windows.Shapes;

namespace Library_Brider_2.Spotify.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    /// 
    public class ConfigurationSettings
    {
        public int ScanDepth
        {
            get { return Properties.Settings.Default.ScanDepth; }
        }
        public int FileOrder
        {
            get { return Properties.Settings.Default.FileOrder; }
        }
        public int NotFoundBehaviour
        {
            get { return Properties.Settings.Default.NotFoundBehaviour; }
        }
        public bool PlaylistPrivacy
        {
            get { return Properties.Settings.Default.PlaylistPrivacy; }
        }
        public bool LikeTracks
        {
            get { return Properties.Settings.Default.LikeTracks; }
        }
    }

    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.ScanDepth = ScanDepthBox.SelectedIndex;
            Properties.Settings.Default.FileOrder = FileOrderBox.SelectedIndex;
            Properties.Settings.Default.NotFoundBehaviour = NotFoundBehaviourBox.SelectedIndex;
            Properties.Settings.Default.PlaylistPrivacy = (bool)PlaylistPrivacyBox.IsChecked;
            Properties.Settings.Default.LikeTracks = (bool)LikeTracksBox.IsChecked;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
