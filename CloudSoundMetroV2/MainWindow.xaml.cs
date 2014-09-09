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
using MahApps.Metro.Controls;
using CloudSoundMetroV2.Mp3Players;
using System.Collections.ObjectModel;
using CloudSoundMetroV2.DataAccessLayer;
using System.IO;
using Microsoft.Win32;
using Cloudmp3.Windows;

namespace CloudSoundMetroV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private IMp3Player _localPlayer;
        private ObservableCollection<Song> _songList;
        private ObservableCollection<Playlist> _playlistList;
        private AzureAccess _blobAccess;
        private SqlAccess _sqlAccess;

        private BitmapImage _playImage = new BitmapImage(new Uri("Images/Play.png", UriKind.Relative));
        private BitmapImage _pauseImage = new BitmapImage(new Uri("Images/Pause.png", UriKind.Relative));


        private int CurrentSongIndex { get; set; }
        private static int _userId;
        public static int UserId
        {
            get { return _userId; }
        }

        public string notificatioN { get; set; }

        private bool _loggedIn;
        public bool LoggedIn
        {
            get { return _loggedIn; }
            set
            {
                _loggedIn = value;
                LoginChange();
            }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get { return _isPlaying; }
            set
            {
                _isPlaying = value;
                PlayButtonSwap();
            }
        }
        private string localMp3Directory = "C:/Users/Public/Music/CloudMp3";

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Setup();
                _userId = 1;
                LoggedIn = true;
                IsPlaying = false;
                _blobAccess = new AzureAccess();
                _localPlayer = new StreamMp3Player();
                _sqlAccess = new SqlAccess();
                PlayerGrid.DataContext = _localPlayer;
                CurrentSongIndex = -1;

                this.Loaded += new RoutedEventHandler(LoginPrompt);
                this.Loaded += delegate
                {
                    this.FileExitItem.Click += (s, e) =>
                    {
                        this.Close();
                    };
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException.Message);
            }
        }

        private void LoginPrompt(object sender, RoutedEventArgs e)
        {
            LoginExecuted(null, null);
        }

        private void PromptLogin(object sender, RoutedEventArgs e)
        {
            LoginExecuted(null, null);
        }

        private void Setup()
        {
            if (!Directory.Exists(localMp3Directory))
            {
                Directory.CreateDirectory(localMp3Directory);
            }
        }

        private void PlayButtonSwap()
        {
            ((Image)(PlayButton.Content)).Source = (IsPlaying) ? _pauseImage : _playImage;
        }

        private void NotifyUsrup()
        {
            if (_blobAccess.isCompleted.Equals(true))
            {
                notificatioN = "Song is being uploaded";
            }
        }
        private void NotifyUsrdown()
        {
            if (_blobAccess.isCompleted.Equals(true))
            {
                notificatioN = "Song is being downloaded";
            }
        }

        private void Song_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsPlaying = true;
            _localPlayer.Stop();
            CurrentSongIndex = SongDataGrid.SelectedIndex;
            Song s = (Song)SongDataGrid.SelectedItem;
            _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
        }

        private void LoginChange()
        {
            if (!_loggedIn)
            {
                IsPlaying = false;
            }
            else
            {
                CurrentSongIndex = -1;
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    _songList = _sqlAccess.GetSongsForUser(_userId);
                    SongDataGrid.ItemsSource = _songList;
                    _playlistList = _sqlAccess.GetPlaylistsForUser(_userId);
                    PlaylistBox.ItemsSource = _playlistList;
                }));
            }
        }

        private void LoginCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!_loggedIn)
                {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void LoginExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Login log = new Login();
            log.Top = this.Top + 50;
            log.Left = this.Left + 50;
            log.ShowDialog();
            log.Focus();

            if (log.UserName != null)
            {
                if (_sqlAccess.ValidateUserName(log.UserName, log.Password))
                {
                    _userId = _sqlAccess.GetUserID(log.UserName);
                    LoggedIn = true;
                    //Content = "You are logged in as " + log.UserName;
                }
                else
                {
                    MessageBox.Show("Incorrect Username or Password.");
                }
            }
            if (e != null)
            {
                e.Handled = true;
            }
        }

        private void LogoutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
                e.Handled = true;
        }

        private void LogoutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _localPlayer.Stop();
            _userId = 0;
            SongDataGrid.ItemsSource = null;
            LoggedIn = false;
            e.Handled = true;
            // This is obviously sleepy code....
            //Content = "You have logged out. Good Bye";

        }

        private void UploadSongCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }
        private void ActiveProgress()
        {
            rng.Visibility = Visibility.Visible;
            rng.IsActive = true;
        }

        private void InactProgress()
        {
            rng.IsActive = false;
            rng.Visibility = Visibility.Collapsed;
        }

        private void UploadSongExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ActiveProgress();
            OpenFileDialog chooseFile = new OpenFileDialog();
            chooseFile.Filter = "Music Files (.mp3)|*.mp3|All Files (*.*)|*.*";
            chooseFile.FilterIndex = 1;
            chooseFile.Multiselect = true;
            chooseFile.ShowDialog();
            string[] files = chooseFile.FileNames;

            if (files.Length != 0)
            {
                proglabe.Content = chooseFile.FileName + "is being Uploaded";
                Task.Factory.StartNew(() =>
                {
                    foreach (string f in files)
                    {
                        _blobAccess.UploadSong(f, _userId);
                        Dispatcher.BeginInvoke(new Action(delegate()
                        {
                            //backgroundWorker1.RunWorkerAsync();
                            _songList = _sqlAccess.GetSongsForUser(_userId);
                            if (PlaylistBox.SelectedIndex == -1)
                            {
                                SongDataGrid.ItemsSource = _songList;
                            }
                        }));
                    }
                });
            }

            e.Handled = true;
            InactProgress();
        }

        private void DownloadSongCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn && SongDataGrid.SelectedIndex != -1)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void DownloadSongExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ActiveProgress();
            Song s = (Song)SongDataGrid.SelectedItem;
            string path = s.S_Path;
            Task.Factory.StartNew(() =>
            {
                _blobAccess.DownloadSong(Path.GetFileName(path));
            });

            e.Handled = true;

            InactProgress();
        }

        private void PlayCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void PlayExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!SongDataGrid.Items.IsEmpty)
            {   
                if (!IsPlaying)
                {
                    if (SongDataGrid.SelectedIndex == -1)
                    {
                        IsPlaying = true;
                        SongDataGrid.SelectedIndex = ++SongDataGrid.SelectedIndex;
                        CurrentSongIndex = SongDataGrid.SelectedIndex;
                        Song s = (Song)SongDataGrid.SelectedItem;
                        _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
                    }
                    else
                    {
                        IsPlaying = true;
                        if (CurrentSongIndex != SongDataGrid.SelectedIndex)
                        {
                            _localPlayer.Stop();
                        }
                        CurrentSongIndex = SongDataGrid.SelectedIndex;
                        Song s = (Song)SongDataGrid.SelectedItem;
                        _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
                    }
                }
                else
                {
                    IsPlaying = false;
                    _localPlayer.Pause();
                }
            }
            e.Handled = true;
        }

        private void StopCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void StopExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            IsPlaying = false;
            _localPlayer.Stop();
            e.Handled = true;
        }

        private void NextCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void NextExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!SongDataGrid.Items.IsEmpty)
            {
                IsPlaying = true;
                _localPlayer.Stop();
                SongDataGrid.SelectedIndex = (CurrentSongIndex == SongDataGrid.Items.Count - 1) ? 0 : ++SongDataGrid.SelectedIndex;
                CurrentSongIndex = SongDataGrid.SelectedIndex;
                Song s = (Song)SongDataGrid.SelectedItem;
                _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
            }
            e.Handled = true;
        }

        private void PrevCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void PrevExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!SongDataGrid.Items.IsEmpty)
            {
                IsPlaying = true;
                _localPlayer.Stop();
                SongDataGrid.SelectedIndex = (CurrentSongIndex <= 0) ? SongDataGrid.Items.Count - 1 : --SongDataGrid.SelectedIndex;
                CurrentSongIndex = SongDataGrid.SelectedIndex;
                Song s = (Song)SongDataGrid.SelectedItem;
                _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
            }
            e.Handled = true;
        }

        //Add Playlist
        private void ShowAddPlaylist_Click(object sender, RoutedEventArgs e)
        {
            AddPlayistSection.Visibility = Visibility.Visible;
        }


        //Remove Playlist
        private void RemovePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistBox.SelectedItem != null)
            {
                Playlist SelectedPlaylist = (Playlist)PlaylistBox.SelectedItem;
                _sqlAccess.RemovePlaylist(SelectedPlaylist.P_Id, _userId);
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    _songList = _sqlAccess.GetSongsForUser(_userId);
                    SongDataGrid.ItemsSource = _songList;
                    _playlistList = _sqlAccess.GetPlaylistsForUser(_userId);
                    PlaylistBox.ItemsSource = _playlistList;
                }));
            }
        }

        //Add new Playlist Methods
        //Open Window to create new Playlist
        private void AddList_Click(object sender, RoutedEventArgs e)
        {
            _userId = MainWindow.UserId;

            string NewPlaylistName = PlaylistNameBox.Text;

            if (!string.IsNullOrWhiteSpace(NewPlaylistName))
            {
                Playlist NewPlaylist = new Playlist();
                NewPlaylist.P_Name = NewPlaylistName;
                _sqlAccess.AddPlaylist(NewPlaylist, _userId);
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    _songList = _sqlAccess.GetSongsForUser(_userId);
                    SongDataGrid.ItemsSource = _songList;
                    _playlistList = _sqlAccess.GetPlaylistsForUser(_userId);
                    PlaylistBox.ItemsSource = _playlistList;
                }));
                PlaylistNameBox.Text = "";
                AddPlayistSection.Visibility = Visibility.Collapsed;
            }
        }

        private void CancelList_Click(object sender, RoutedEventArgs e)
        {
            PlaylistNameBox.Text = "";
            AddPlayistSection.Visibility = Visibility.Collapsed;
        }

        //End Add new Playlist Methods


        private void Collection_Click(object sender, RoutedEventArgs e)
        {
            SongDataGrid.ItemsSource = _songList;
            PlaylistBox.SelectedIndex = -1;
        }

        private void PlaylistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.SelectedItem != null)
            {
                Playlist p = (Playlist)lb.SelectedItem;
                SongDataGrid.ItemsSource = _sqlAccess.GetSongsInPlaylist(p.P_Id);
            }
        }
        //End Add Song to Playlist methods

        //Begin Drag and Drop Stuff
        //Not Fully Implemented
        private void SongDataGridDrag(object sender, MouseButtonEventArgs e)
        {
            Song s = (Song)SongDataGrid.SelectedItem;
            //int id = s.S_Id;
            //DataObject obj = new DataObject(id);
            //DragDrop.DoDragDrop((DependencyObject)SongDataGrid.SelectedItem, obj, DragDropEffects.Copy);
            DragDrop.DoDragDrop(SongDataGrid, s, DragDropEffects.Copy);
        }

        private void SongDrop(object sender, DragEventArgs e)
        {
            Song s = (Song)e.Data.GetData(typeof(Song));
            //Playlist p = (Playlist)PlaylistBox.SelectedItem;
            //Point p = Mouse.GetPosition(PlaylistBox);
            //int index = PlaylistBox.
            //_sqlAccess.AddSongToPlaylist(s.S_Id, p.P_Id);
        }
        //End Drag and Drop Stuff
    }
}
