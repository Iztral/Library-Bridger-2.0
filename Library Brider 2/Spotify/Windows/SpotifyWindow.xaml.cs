using Library_Brider_2.Generic_Classes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Serialization;
using MessageBox = System.Windows.MessageBox;

namespace Library_Brider_2.Spotify.Windows
{
    /// <summary>
    /// Interaction logic for SpotifyWindow.xaml
    /// </summary>
    public partial class SpotifyWindow : Window
    {
        private readonly static SpotifyWebAPI _spotify = new SpotifyWebAPI();
        readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        private List<FullTrack> GetListOfFoundTracks()
        {
            return (List<FullTrack>)found_list.ItemsSource;
        }

        public SpotifyWindow()
        {
            InitializeComponent();
            SetupWorker();
        }

        #region background worker functions
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            FillSpotifyListWithTracks(sender as BackgroundWorker);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            UpdateFoundList((List<FullTrack>)e.UserState);
        }

        private void UpdateFoundList(List<FullTrack> tracksFoundInSpotify)
        {
            found_list.ItemsSource = null;
            found_list.ItemsSource = tracksFoundInSpotify;
            var item = GetListOfFoundTracks()[GetListOfFoundTracks().Count - 1];
            found_list.ScrollIntoView(item);
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SearchButtonSearchFinished();

            if (found_list.Items != null)
            {
                AddPlaylist_Button.IsEnabled = true;
            }
        }

        private void SearchButtonSearchFinished()
        {
            Search_Button.Click -= CancelSearch_Click;
            Search_Button.Click += SearchButton_Click;
            Search_Button.Content = "Spotify Search";
        }

        private void SearchButtonSearchStarted() 
        {
            Search_Button.Click -= CancelSearch_Click;
            Search_Button.Click += SearchButton_Click;
            Search_Button.Content = "Spotify Search";
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
            if (folderPath != null)
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
            if (Properties.Settings.Default.ScanDepth == 1)
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

        private void FillSpotifyListWithTracks(BackgroundWorker backgroundWorker)
        {
            var tracksFromLocalList = (List<LocalTrack>)local_list.ItemsSource;
            DeleteContainerForNotFound();

            #region statistics
            Stopwatch elapsedTimeCounter = new Stopwatch();
            elapsedTimeCounter.Start();
            #endregion

            List<FullTrack> tracksFoundInSpotify = new List<FullTrack>();

            int progressTracker = 0;

            foreach (LocalTrack localTrack in tracksFromLocalList)
            {
                if (backgroundWorker.CancellationPending)
                {
                    break;
                }
                else
                {
                    SearchItem searchResults = GetTrackSearchResults(localTrack, 1);
                    if (IsSearchEmpty(searchResults))
                    {
                        FullTrack track = searchResults.Tracks.Items[0];
                        tracksFoundInSpotify.Add(track);
                        localTrack.SpotifyUri = track.Id;
                    }
                    else
                    {
                        ProcessNotFoundFile(localTrack);
                    }
                    backgroundWorker.ReportProgress(progressTracker++, tracksFoundInSpotify);
                }
            }

            #region statistics
            elapsedTimeCounter.Stop();
            WriteStatisticsToFile(tracksFromLocalList, tracksFoundInSpotify, (int)elapsedTimeCounter.Elapsed.TotalSeconds);
            #endregion
        }

        private void DeleteContainerForNotFound()
        {
            if (Directory.Exists("Files Not Found"))
            {
                Directory.Delete("Files Not Found", true);
            }
        }

        private SearchItem GetTrackSearchResults(LocalTrack local_, int limitResultAmout)
        {
            if (local_.SearchType == LocalSearchType.FULL_TAGS)
            {
                return SearchSpotifyForTrackByTags(local_, limitResultAmout);
            }
            if (local_.SearchType == LocalSearchType.FILENAME_ONLY)
            {
                return SearchSpotifyForTrackByName(local_, limitResultAmout);
            }
            if (local_.SearchType == LocalSearchType.AUDIO_SEARCH)
            {
                return SearchSpotifyForTrackByFingerprint(local_, limitResultAmout);
            }

            return new SearchItem();
        }

        private SearchItem SearchSpotifyForTrackByTags(LocalTrack local_, int limitResultAmout)
        {
            SearchItem result;
            int numberOfRetries = 0;
            bool hasError;
            do
            {
                result = _spotify.SearchItems(local_.FullTagTitle(), SearchType.Track, limitResultAmout);
                hasError = CheckSearchResultForError(result);
                numberOfRetries++;
            }
            while (hasError || (hasError && (numberOfRetries < 3)));

            return result;
        }

        private SearchItem SearchSpotifyForTrackByName(LocalTrack local_, int limitResultAmout)
        {
            SearchItem result;
            int numberOfRetries = 0;
            bool hasError;
            do
            {
                result = _spotify.SearchItems(local_.FileName, SearchType.Track, limitResultAmout);
                hasError = CheckSearchResultForError(result);
                numberOfRetries++;
            }
            while (hasError || (hasError && (numberOfRetries < 3)));

            if (IsSearchEmpty(result) && !hasError)
                DecreaseSearchCriteria(local_);

            return result;
        }

        private SearchItem SearchSpotifyForTrackByFingerprint(LocalTrack local_, int limitResultAmout)
        {
            SearchItem result;
            int numberOfRetries = 0;
            bool hasError;
            do
            {
                result = _spotify.SearchItems(local_.FileName, SearchType.Track, limitResultAmout);
                hasError = CheckSearchResultForError(result);
                numberOfRetries++;
            }
            while (hasError || (hasError && (numberOfRetries < 3)));

            if (IsSearchEmpty(result) && !hasError)
                DecreaseSearchCriteria(local_);

            return result;
        }

        private bool CheckSearchResultForError(SearchItem resultToCheck)
        {
            if (resultToCheck.HasError())
            {
                Thread.Sleep(1100);
                return true;
            }
            else
                return false;
        }

        private bool IsSearchEmpty(SearchItem resultToCheck)
        {
            return resultToCheck.Tracks.Items.Any();
        }

        private void DecreaseSearchCriteria(LocalTrack local_)
        {
            if (local_.SearchType == LocalSearchType.FULL_TAGS)
            {
                local_.SearchType = LocalSearchType.FILENAME_ONLY;
            }
            else if (local_.SearchType == LocalSearchType.FILENAME_ONLY)
            {
                local_.SearchType = LocalSearchType.AUDIO_SEARCH;
            }
        }

        private void ProcessNotFoundFile(LocalTrack local_)
        {
            System.IO.Directory.CreateDirectory("Files Not Found");
            local_.SpotifyUri = "not found";
            if (Properties.Settings.Default.NotFoundBehaviour == 0)
            {
                SaveNotFoundTrackFileNameToContainer(local_);
            }
            else if (Properties.Settings.Default.NotFoundBehaviour == 1)
            {
                CopyNotFoundTrackToContainer(local_);
            }
        }

        private void SaveNotFoundTrackFileNameToContainer(LocalTrack trackToSave)
        {
            File.AppendAllText("Files Not Found\\" + "not_found_tracks.txt",
                    trackToSave.FileName + Environment.NewLine);
        }

        private void CopyNotFoundTrackToContainer(LocalTrack trackToSave)
        {
            var destFile = AppDomain.CurrentDomain.BaseDirectory +
                    "Files Not Found\\" + Path.GetFileName(trackToSave.LocalPath);
            File.Copy(trackToSave.LocalPath, destFile);
        }

        private void WriteStatisticsToFile(List<LocalTrack> tracksFromLocalList, List<FullTrack> tracksFoundInSpotify, int timeElapsed)
        {
            File.WriteAllText("statistics.txt", "Total number of songs: " + tracksFromLocalList.Count + Environment.NewLine
                + "Number of found songs: " + tracksFoundInSpotify.Count + Environment.NewLine
                + "Time elapsed (in seconds): " + timeElapsed);
        }

        #endregion

        #region playlist operation functions

        #endregion

        #region edit found tracks functions
        #endregion

        #region backup application state

        #endregion

        #region main buttons logic

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

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchButtonSearchStarted();
            backgroundWorker.RunWorkerAsync();
        }

        private void CancelSearch_Click(object sender, RoutedEventArgs e)
        {
            backgroundWorker.CancelAsync();
        }

        #endregion

        #region playlist creation functions

        private void AddPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (!backgroundWorker.IsBusy && IsPlaylistNameNotEmpty())
            {
                ErrorResponse response = CreateFilledPlaylistInSpotify();

                if (response.Error == null)
                {
                    MessageBox.Show("Playlist " + playlistName.Text + " was created.");
                }
                else
                {
                    MessageBox.Show("Something went wrong. Error:" + Environment.NewLine + response.Error.Message);
                }
            }
        }

        private bool IsPlaylistNameNotEmpty()
        {
            return !String.IsNullOrWhiteSpace(playlistName.Text);
        }

        private ErrorResponse CreateFilledPlaylistInSpotify()
        {
            return AddTracks(CreateEmptyPlaylistInSpotify().Id, GetListOfFoundTracks());
        }

        private FullPlaylist CreateEmptyPlaylistInSpotify()
        {
            return _spotify.CreatePlaylist(_spotify.GetPrivateProfile().Id, playlistName.Text, Properties.Settings.Default.PlaylistPrivacy);
        }

        private ErrorResponse AddTracks(string playlistId, List<FullTrack> tracksAddedToPlaylist)
        {
            ErrorResponse response = FillPlaylistWithTracks(playlistId, tracksAddedToPlaylist);

            if (Properties.Settings.Default.LikeTracks)
            {
                response = AddTracksToLibrary(tracksAddedToPlaylist);
            }
            return response;
        }

        private ErrorResponse FillPlaylistWithTracks(string playlistId, List<FullTrack> tracksAddedToPlaylist)
        {
            ErrorResponse response = new ErrorResponse();

            List<string> trackUris = new List<string>();
            tracksAddedToPlaylist.ForEach(i => trackUris.Add(i.Uri));

            SplitList<string>(trackUris, 99).ForEach(i => response = _spotify.AddPlaylistTracks(playlistId, i));
            return response;
        }

        private static List<List<T>> SplitList<T>(List<T> locations, int nSize)
        {
            var list = new List<List<T>>();

            for (int i = 0; i < locations.Count; i += nSize)
            {
                list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
            }

            return list;
        }

        private ErrorResponse AddTracksToLibrary(List<FullTrack> tracksAddedToPlaylist)
        {
            ErrorResponse response = new ErrorResponse();

            List<string> songIds = new List<string>();
            tracksAddedToPlaylist.ForEach(i => songIds.Add(i.Id));

            SplitList<string>(songIds, 99).ForEach(i => response = _spotify.SaveTracks(i));

            return response;
        }

        #endregion

        #region found tracks edit functions

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!backgroundWorker.IsBusy && found_list.SelectedItem != null)
            {
                (GetListOfFoundTracks()).Remove((FullTrack)found_list.SelectedItem);
                var old = found_list.ItemsSource;
                found_list.ItemsSource = null;
                found_list.ItemsSource = old;
            }
        }

        private void Replace_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!backgroundWorker.IsBusy && found_list.SelectedItem != null)
            {
                string selectedSpotify = ((FullTrack)found_list.SelectedItem).Id;
                int selectedIndex = found_list.SelectedIndex;

                var listlocal_ = (List<LocalTrack>)local_list.ItemsSource;
                LocalTrack local_ = listlocal_.Find(x => x.SpotifyUri == selectedSpotify);
                if (local_ != null)
                {
                    var list = GetTrackSearchResults(local_, 5);
                    ReplaceDialog dialog = new ReplaceDialog(list);
                    if (dialog.ShowDialog() == true && dialog.returnTrack != null)
                    {
                        var old = GetListOfFoundTracks();
                        found_list.ItemsSource = null;
                        old[selectedIndex] = dialog.returnTrack;
                        found_list.ItemsSource = old;
                    }
                }
            }
        }

        private void Find_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!backgroundWorker.IsBusy && found_list.SelectedItem != null)
            {
                string selectedSpotify = ((FullTrack)found_list.SelectedItem).Id;
                var listlocal_ = (List<LocalTrack>)local_list.ItemsSource;
                var local_ = listlocal_.Find(x => x.SpotifyUri == selectedSpotify);
                local_list.SelectedItem = local_;
                local_list.ScrollIntoView(local_);
                
            }
        }

        #endregion

        #region backup
        private void SaveBackup_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!(local_list.Items.IsEmpty || found_list.Items.IsEmpty))
            {
                WriteToXML((List<LocalTrack>)local_list.ItemsSource, (List<FullTrack>)found_list.ItemsSource);
            }
            else
            {
                MessageBox.Show("One of lists is empty.");
            }
        }

        private void LoadBackup_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_spotify.AccessToken != null)
            {
                if (File.Exists("Backup\\list_local.xml") && File.Exists("Backup\\list_spotify.xml"))
                {
                    List<LocalTrack> listLocal_ = new List<LocalTrack>();
                    List<string> songIds = new List<string>();
                    List<FullTrack> listSpotify_ = new List<FullTrack>();

                    ReadFromXML(ref listLocal_, ref songIds);
                    foreach (string songId in songIds)
                    {
                        listSpotify_.Add(_spotify.GetTrack(songId));
                    }

                    if (!(listLocal_.Count == 0 || listSpotify_.Count == 0))
                    {
                        local_list.ItemsSource = listLocal_;
                        found_list.ItemsSource = listSpotify_;
                        Search_Button.IsEnabled = true;
                        AddPlaylist_Button.IsEnabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Backups corrupted.");
                    }
                }
                else
                {
                    MessageBox.Show("No backup found.");
                }
            }
            else
            {
                MessageBox.Show("Application is unathorized");
            }
        }

        public void WriteToXML(List<LocalTrack> listLocal_, List<FullTrack> listSpotify_)
        {
            if (Directory.Exists("Backup"))
            {
                Directory.Delete("Backup", true);
            }
            Directory.CreateDirectory("Backup");

            XmlSerializer serialiser = new XmlSerializer(typeof(List<LocalTrack>));
            TextWriter FileStream = new StreamWriter("Backup\\list_local.xml");
            serialiser.Serialize(FileStream, listLocal_);
            FileStream.Close();

            serialiser = new XmlSerializer(typeof(List<string>));
            FileStream = new StreamWriter("Backup\\list_spotify.xml");
            serialiser.Serialize(FileStream, WriteFullTrack(listSpotify_));
            FileStream.Close();
        }

        private List<string> WriteFullTrack(List<FullTrack> listSpotify_)
        {
            List<string> trackIds = new List<string>();
            foreach (FullTrack fullTrack in listSpotify_)
            {
                trackIds.Add(fullTrack.Id);
            }
            return trackIds;
        }

        public void ReadFromXML(ref List<LocalTrack> listLocal_, ref List<string> listSpotify_)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<LocalTrack>));
            FileStream fs = new FileStream(@"Backup\\list_local.xml", FileMode.Open);
            listLocal_ = (List<LocalTrack>)serializer.Deserialize(fs);
            fs.Close();

            serializer = new XmlSerializer(typeof(List<string>));
            fs = new FileStream(@"Backup\\list_spotify.xml", FileMode.Open);
            listSpotify_ = (List<string>)serializer.Deserialize(fs);
            fs.Close();
        }
        #endregion
    }
}
