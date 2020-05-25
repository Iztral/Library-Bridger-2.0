using Library_Brider_2.Generic_Classes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace Library_Brider_2.Spotify.Windows
{
    /// <summary>
    /// Interaction logic for SpotifyWindow.xaml
    /// </summary>
    public partial class SpotifyWindow : Window
    {
        private readonly static SpotifyWebAPI _spotify = new SpotifyWebAPI();
        readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        public SpotifyWindow()
        {
            InitializeComponent();
            SetupWorker();
        }


        #region background worker functions#
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //search for local songs//
            throw new NotImplementedException();
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //report progress//
            throw new NotImplementedException();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Enable playlist operations//
            throw new NotImplementedException();
        }

        private void SetupWorker()
        {
            backgroundWorker.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker.ProgressChanged += Worker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
        }

        #endregion

        #region authorization functions

        public void Authorize(string _clientId)
        {
            ImplicitGrantAuth auth = new ImplicitGrantAuth(_clientId,
                "http://localhost:4002", "http://localhost:4002",
                Scope.UserLibraryModify | Scope.PlaylistModifyPrivate | Scope.PlaylistModifyPublic);
            auth.AuthReceived += (sender, payload) =>
            {
                auth.Stop();
                _spotify.TokenType = payload.TokenType;
                _spotify.AccessToken = payload.AccessToken;
            };
            auth.Start();
            auth.OpenBrowser();
        }

        #endregion

        #region local search functions

        private void FillLocalListWithTracks()
        {
            string folderPath = GetFolderPath();
            if(folderPath != null)
            {
                List<LocalTrack> trackList = GetLocalTracksFromFolder(folderPath);
                SortTracks(trackList);

                if (trackList != null && trackList.Count > 0)
                {
                    FinishLocalSearch(trackList);
                }
            }
        }

        private void FinishLocalSearch(List<LocalTrack> trackList)
        {
            local_list.ItemsSource = trackList;
            progressBar.Maximum = trackList.Count;
            Search_Button.IsEnabled = true;
        }

        private string GetFolderPath()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    return fbd.SelectedPath;
                }
                else
                {
                    return null;
                }
            }
        }

        private List<LocalTrack> GetLocalTracksFromFolder(string folderPath)
        {
            DirectoryInfo scannedFolderInfo = new DirectoryInfo(folderPath);

            List<LocalTrack> trackList = new List<LocalTrack>();
            foreach (FileSystemInfo file in GetMusicFilesInFolder(scannedFolderInfo))
            {
                trackList.Add(new LocalTrack(file.FullName));
            }

            return trackList;
        }

        private FileSystemInfo[] GetMusicFilesInFolder(DirectoryInfo folder)
        {
            if(Properties.Settings.Default.ScanDepth == 1)
            {
                return folder.GetFileSystemInfos("*.mp3", SearchOption.AllDirectories);
            }
            else
            {
                return folder.GetFileSystemInfos("*.mp3", SearchOption.TopDirectoryOnly);
            }
           
        }

        private LocalTrack GetLocalTrackFromPath(string path)
        {
            return new LocalTrack(path);
        }

        private void SortTracks(List<LocalTrack> trackList)
        {
            switch (Properties.Settings.Default.FileOrder)
            {
                case 0:
                    if (Properties.Settings.Default.FileOrderReversed)
                        trackList = trackList.OrderByDescending(o => o.FileName).ToList();
                    else
                        trackList = trackList.OrderBy(o => o.FileName).ToList();
                    break;
                case 1:
                    if (Properties.Settings.Default.FileOrderReversed)
                        trackList = trackList.OrderByDescending(o => o.Author).ToList();
                    else
                        trackList = trackList.OrderBy(o => o.Author).ToList();
                    break;
                case 2:
                    if (Properties.Settings.Default.FileOrderReversed)
                        trackList = trackList.OrderByDescending(o => o.Title).ToList();
                    else
                        trackList = trackList.OrderBy(o => o.Title).ToList();
                    break;
                case 3:
                    if (Properties.Settings.Default.FileOrderReversed)
                        trackList = trackList.OrderByDescending(o => o.CreationDate).ToList();
                    else
                        trackList = trackList.OrderBy(o => o.CreationDate).ToList();
                    break;
                case 4:
                    if (Properties.Settings.Default.FileOrderReversed)
                        trackList = trackList.OrderByDescending(o => o.ModificationDate).ToList();
                    else
                        trackList = trackList.OrderBy(o => o.ModificationDate).ToList();
                    break;
            }
        }



        #endregion

        #region external search functions
        #endregion

        #region playlist operation functions

        #endregion

        #region edit found tracks functions
        #endregion

        #region backup application state

        #endregion

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        private void AuthButton_Click(object sender, RoutedEventArgs e)
        {
            Authorize(Properties.Settings.Default.ApplicationKey);
        }

        private void LocalSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_spotify.AccessToken != null)
            {
                FillLocalListWithTracks();
            }
            else
            {
                System.Windows.MessageBox.Show("First, you must authorize the application.");
            }
        }

        
    }
}
