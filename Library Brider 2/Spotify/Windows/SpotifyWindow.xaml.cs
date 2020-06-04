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
            ScrollToLatestTrack((List<FullTrack>)e.UserState);
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

        private void Authorize(string _clientId)
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

        private bool IsAuthorized()
        {
            if (_spotify.AccessToken != null)
                return true;
            else
                return false;
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

        private List<FullTrack> ListOfFoundTracks => (List<FullTrack>)found_list.ItemsSource;

        private List<LocalTrack> GetLocalTracksFromFolder(string folderPath)
        {
            List<LocalTrack> trackList = new List<LocalTrack>();
            foreach (FileSystemInfo file in GetMusicFilesInFolder(new DirectoryInfo(folderPath)))
            {
                trackList.Add(GetLocalTrackFromPath(file.FullName));
            }

            return trackList;
        }

        private FileSystemInfo[] GetMusicFilesInFolder(DirectoryInfo folder)
        {
            return Properties.Settings.Default.ScanDepth == 1
                ? folder.GetFileSystemInfos("*.mp3", SearchOption.AllDirectories)
                : folder.GetFileSystemInfos("*.mp3", SearchOption.TopDirectoryOnly);
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
                    trackList = Properties.Settings.Default.FileOrderReversed
                        ? trackList.OrderByDescending(o => o.FileName).ToList()
                        : trackList.OrderBy(o => o.FileName).ToList();
                    break;
                case 1:
                    trackList = Properties.Settings.Default.FileOrderReversed
                        ? trackList.OrderByDescending(o => o.Author).ToList()
                        : trackList.OrderBy(o => o.Author).ToList();
                    break;
                case 2:
                    trackList = Properties.Settings.Default.FileOrderReversed
                        ? trackList.OrderByDescending(o => o.Title).ToList()
                        : trackList.OrderBy(o => o.Title).ToList();
                    break;
                case 3:
                    trackList = Properties.Settings.Default.FileOrderReversed
                        ? trackList.OrderByDescending(o => o.CreationDate).ToList()
                        : trackList.OrderBy(o => o.CreationDate).ToList();
                    break;
                case 4:
                    trackList = Properties.Settings.Default.FileOrderReversed
                        ? trackList.OrderByDescending(o => o.ModificationDate).ToList()
                        : trackList.OrderBy(o => o.ModificationDate).ToList();
                    break;
            }
        }

        #endregion

        #region external search functions

        private void FillSpotifyListWithTracks(BackgroundWorker backgroundWorker)
        {
            DeleteContainerForNotFound();

            #region statistics
            Stopwatch elapsedTimeCounter = new Stopwatch();
            elapsedTimeCounter.Start();
            #endregion

            List<FullTrack> tracksFoundInSpotify = new List<FullTrack>();

            int progressTracker = 0;

            List<LocalTrack> tracksFromLocalList = ListOfLocalTracks;
            foreach (LocalTrack localTrack in tracksFromLocalList)
            {
                if (backgroundWorker.CancellationPending)
                    break;
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
                        ProcessNotFoundFile(localTrack);

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
                Directory.Delete("Files Not Found", true);
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
            int numberOfRetries = 0;
            bool hasError;
            SearchItem result;
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

        private List<LocalTrack> ListOfLocalTracks => (List<LocalTrack>)local_list.ItemsSource;

        private void ScrollToLatestTrack(List<FullTrack> tracksFoundInSpotify)
        {
            found_list.ItemsSource = null;
            found_list.ItemsSource = tracksFoundInSpotify;
            found_list.ScrollIntoView(ListOfFoundTracks[ListOfFoundTracks.Count - 1]);
        }

        private SearchItem SearchSpotifyForTrackByFingerprint(LocalTrack local_, int resultAmountLimiter)
        {
            SearchItem result;
            int numberOfRetries = 0;
            bool hasError;

            //GetAudioFingerPrint();//
            do
            {
                result = _spotify.SearchItems(local_.FileName, SearchType.Track, resultAmountLimiter);
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
            switch (local_.SearchType)
            {
                case LocalSearchType.FULL_TAGS:
                    local_.SearchType = LocalSearchType.FILENAME_ONLY;
                    break;
                case LocalSearchType.FILENAME_ONLY:
                    local_.SearchType = LocalSearchType.AUDIO_SEARCH;
                    break;
            }
        }

        private void ProcessNotFoundFile(LocalTrack local_)
        {
            Directory.CreateDirectory("Files Not Found");
            local_.SpotifyUri = "not found";
            switch (Properties.Settings.Default.NotFoundBehaviour)
            {
                case 0:
                    SaveNotFoundTrackFileNameToContainer(local_);
                    break;
                case 1:
                    CopyNotFoundTrackToContainer(local_);
                    break;
            }
        }

        private void SaveNotFoundTrackFileNameToContainer(LocalTrack trackToSave)
        {
            File.AppendAllText("Files Not Found\\" + "not_found_tracks.txt",
                    trackToSave.FileName + Environment.NewLine);
        }

        private void CopyNotFoundTrackToContainer(LocalTrack trackToSave)
        {
            File.Copy(trackToSave.LocalPath, AppDomain.CurrentDomain.BaseDirectory +
                    "Files Not Found\\" + Path.GetFileName(trackToSave.LocalPath));
        }

        private void WriteStatisticsToFile(List<LocalTrack> tracksFromLocalList, List<FullTrack> tracksFoundInSpotify, int timeElapsed)
        {
            File.WriteAllText("statistics.txt", "Total number of songs: " + tracksFromLocalList.Count + Environment.NewLine
                + "Number of found songs: " + tracksFoundInSpotify.Count + Environment.NewLine
                + "Time elapsed (in seconds): " + timeElapsed);
        }

        #endregion

        #region playlist creation functions

        private bool IsPlaylistNameNotEmpty => !string.IsNullOrWhiteSpace(playlistName.Text);

        private ErrorResponse CreateFilledPlaylistInSpotify()
        {
            return AddTracks(CreateEmptyPlaylistInSpotify().Id, ListOfFoundTracks);
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

        #region edit found tracks functions

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            if (IsFoundListNotEmptyOrProcessed())
            {
                (ListOfFoundTracks).Remove((FullTrack)found_list.SelectedItem);
                UpdateFoundList();
            }
        }

        private bool IsFoundListNotEmptyOrProcessed()
        {
            return !backgroundWorker.IsBusy && found_list.SelectedItem != null;
        }

        private void UpdateFoundList()
        {
            var old = found_list.ItemsSource;
            found_list.ItemsSource = null;
            found_list.ItemsSource = old;
        }

        private void Replace_Button_Click(object sender, RoutedEventArgs e)
        {
            if (IsFoundListNotEmptyOrProcessed())
            {
                LocalTrack localTrack = ListOfLocalTracks.Find(x => x.SpotifyUri == ((FullTrack)found_list.SelectedItem).Id);
                if (localTrack != null)
                {
                    ReplaceDialog dialog = new ReplaceDialog(GetTrackSearchResults(localTrack, 5));
                    if (dialog.ShowDialog() == true && dialog.returnTrack != null)
                    {
                        found_list.ItemsSource = null;
                        ListOfFoundTracks[found_list.SelectedIndex] = dialog.returnTrack;
                        found_list.ItemsSource = ListOfFoundTracks;
                    }
                }
            }
        }

        private void Find_Button_Click(object sender, RoutedEventArgs e)
        {
            if (IsFoundListNotEmptyOrProcessed())
            {
                LocalTrack local_ = ListOfLocalTracks.Find(x => x.SpotifyUri == ((FullTrack)found_list.SelectedItem).Id);
                local_list.SelectedItem = local_;
                local_list.ScrollIntoView(local_);
            }
        }

        #endregion

        #region backup application state

        public void BackupApplicationState(List<LocalTrack> listLocal_, List<FullTrack> listSpotify_)
        {
            SetupBackupFolder();

            SerializeLocalTracksToFile(listLocal_);
            SerializeSpotifyTracksToFile(listSpotify_);
        }

        private void SetupBackupFolder()
        {
            if (Directory.Exists("Backup"))
            {
                Directory.Delete("Backup", true);
            }
            Directory.CreateDirectory("Backup");
        }

        private void SerializeLocalTracksToFile(List<LocalTrack> listLocal_)
        {
            XmlSerializer serialiser = new XmlSerializer(typeof(List<LocalTrack>));
            using (TextWriter FileStream = new StreamWriter("Backup\\list_local.xml"))
            {
                serialiser.Serialize(FileStream, listLocal_);
                FileStream.Close();
            }
        }

        private void SerializeSpotifyTracksToFile(List<FullTrack> listSpotify_)
        {
            XmlSerializer serialiser = new XmlSerializer(typeof(List<string>));
            using (TextWriter FileStream = new StreamWriter("Backup\\list_spotify.xml"))
            {
                serialiser.Serialize(FileStream, GetTrackIDs(listSpotify_));
                FileStream.Close();
            }
        }

        private List<string> GetTrackIDs(List<FullTrack> listSpotify_)
        {
            List<string> trackIds = new List<string>();
            listSpotify_.ForEach(x => trackIds.Add(x.Id));

            return trackIds;
        }

        public void LoadApplicationState(ref List<LocalTrack> listLocal_, ref List<string> listSpotify_)
        {
            LoadLocalListFromXML(ref listLocal_);

            LoadSpotifyListFromXML(ref listSpotify_);
        }

        private bool DoesBackupExist()
        {
            return File.Exists("Backup\\list_local.xml") && File.Exists("Backup\\list_spotify.xml");
        }

        private void LoadLocalListFromXML(ref List<LocalTrack> listLocal_)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<LocalTrack>));
            using (FileStream fs = new FileStream(@"Backup\\list_local.xml", FileMode.Open))
            {
                listLocal_ = (List<LocalTrack>)serializer.Deserialize(fs);
                fs.Close();
            }
        }

        private void LoadSpotifyListFromXML(ref List<string> listSpotify_)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
            using (FileStream fs = new FileStream(@"Backup\\list_spotify.xml", FileMode.Open))
            {
                listSpotify_ = (List<string>)serializer.Deserialize(fs);
                fs.Close();
            }
        }

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
            if (IsAuthorized())
                FillLocalListWithTracks();
            else
                MessageBox.Show("First, you must authorize the application.");
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

        private void AddPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (!backgroundWorker.IsBusy && IsPlaylistNameNotEmpty)
            {
                ErrorResponse response = CreateFilledPlaylistInSpotify();

                if (response.Error == null)
                    MessageBox.Show("Playlist " + playlistName.Text + " was created.");
                else
                    MessageBox.Show("Something went wrong. Error:" + Environment.NewLine + response.Error.Message);
            }
        }

        private void SaveBackup_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!(local_list.Items.IsEmpty || found_list.Items.IsEmpty))
                BackupApplicationState(ListOfLocalTracks, ListOfFoundTracks);
            else
                MessageBox.Show("One of lists is empty.");
        }

        private void LoadBackup_Button_Click(object sender, RoutedEventArgs e)
        {
            if (IsAuthorized())
            {
                if (DoesBackupExist())
                {
                    List<LocalTrack> listLocal_ = new List<LocalTrack>();
                    List<FullTrack> listSpotify_ = new List<FullTrack>();

                    List<string> songIds = new List<string>();

                    LoadApplicationState(ref listLocal_, ref songIds);

                    songIds.ForEach(x => listSpotify_.Add(_spotify.GetTrack(x)));

                    if (!(listLocal_.Count == 0 || listSpotify_.Count == 0))
                    {
                        local_list.ItemsSource = listLocal_;
                        found_list.ItemsSource = listSpotify_;
                        Search_Button.IsEnabled = true;
                        AddPlaylist_Button.IsEnabled = true;
                    }
                    else
                        MessageBox.Show("Backups corrupted.");
                }
                else
                    MessageBox.Show("No backup found.");
            }
            else
                MessageBox.Show("Application is unathorized");
        }
        #endregion
    }
}
