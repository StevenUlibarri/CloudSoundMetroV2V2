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
using System.Windows.Controls.Primitives;

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

        public bool RepeatOne { get; set; }
        public bool RepeatAll { get; set; }

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
                LoggedIn = false;
                IsPlaying = false;
                _blobAccess = new AzureAccess();
                _localPlayer = new StreamMp3Player();
                _sqlAccess = new SqlAccess();
                PlayerGrid.DataContext = _localPlayer;
                CurrentSongIndex = -1;

                this.Loaded += new RoutedEventHandler(LoginPrompt);
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
            _songList = null;
            _playlistList = null;
            SongDataGrid.ItemsSource = null;
            PlaylistBox.ItemsSource = null;
            RepeatAll = false;
            RepeatOne = false;
            LoggedIn = false;
            e.Handled = true;
        }

        private void UploadSongCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void UploadSongExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog chooseFile = new OpenFileDialog();
            chooseFile.Filter = "Music Files (.mp3)|*.mp3|All Files (*.*)|*.*";
            chooseFile.FilterIndex = 1;
            chooseFile.Multiselect = true;
            chooseFile.ShowDialog();
            string[] files = chooseFile.FileNames;

            if (files.Length != 0)
            {
                Task.Factory.StartNew(() =>
                {
                    foreach (string f in files)
                    {
                        _blobAccess.UploadSong(f, _userId);
                        Dispatcher.BeginInvoke(new Action(delegate()
                        {
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
            Song s = (Song)SongDataGrid.SelectedItem;
            string path = s.S_Path;
            Task.Factory.StartNew(() =>
            {
                _blobAccess.DownloadSong(Path.GetFileName(path));
            });

            e.Handled = true;
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
                        bool EventSet = false;
                        while (!EventSet)
                        {
                            if (_localPlayer.GetWaveOut() != null)
                            {
                                _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
                                EventSet = true;
                            }
                        }
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
                        bool EventSet = false;
                        while (!EventSet)
                        {
                            if (_localPlayer.GetWaveOut() != null)
                            {
                                _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
                                EventSet = true;
                            }
                        }
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

        private void MainWindow_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            Console.WriteLine("DID THIS WORK?");
            if (RepeatOne)
            {
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    _localPlayer.Stop();
                    SongDataGrid.SelectedIndex = CurrentSongIndex;
                    Song s = (Song)SongDataGrid.SelectedItem;
                    
                }));
                _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
                _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
            }
            else
            {
                if (CurrentSongIndex != SongDataGrid.Items.Count - 1)
                {
                    
                    Dispatcher.BeginInvoke(new Action(delegate()
                    {
                        _localPlayer.Stop();
                        SongDataGrid.SelectedIndex = (CurrentSongIndex == SongDataGrid.Items.Count - 1) ? 0 : ++SongDataGrid.SelectedIndex;
                        CurrentSongIndex = SongDataGrid.SelectedIndex;
                        Song s = (Song)SongDataGrid.SelectedItem;
                        _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
                        _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
                    }));
                }
                else if (CurrentSongIndex == SongDataGrid.Items.Count - 1 && RepeatAll)
                {
                    Dispatcher.BeginInvoke(new Action(delegate()
                    {
                        _localPlayer.Stop();
                        SongDataGrid.SelectedIndex = (CurrentSongIndex == SongDataGrid.Items.Count - 1) ? 0 : ++SongDataGrid.SelectedIndex;
                        CurrentSongIndex = SongDataGrid.SelectedIndex;
                        Song s = (Song)SongDataGrid.SelectedItem;
                        _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
                        _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(delegate()
                    {
                        _isPlaying = false;
                        _localPlayer.Stop();
                    }));
                }
            }
            //if repeat one play again
            //if not on last song play next
            //else
            //if on last song and repeat all play first
            //else stop
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
                bool EventSet = false;
                while (!EventSet)
                {
                    if (_localPlayer.GetWaveOut() != null)
                    {
                        _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
                        EventSet = true;
                    }
                }
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
                bool EventSet = false;
                while (!EventSet)
                {
                    if (_localPlayer.GetWaveOut() != null)
                    {
                        _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
                        EventSet = true;
                    }
                }
            }
            e.Handled = true;
        }

        //Add Playlist
        //private void ShowAddPlaylist_Click(object sender, RoutedEventArgs e)
        //{
        //    AddPlayistSection.Visibility = Visibility.Visible;
        //}


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
        //private void AddList_Click(object sender, RoutedEventArgs e)
        //{
        //    _userId = MainWindow.UserId;

        //    string NewPlaylistName = PlaylistNameBox.Text;

        //    if (!string.IsNullOrWhiteSpace(NewPlaylistName))
        //    {
        //        Playlist NewPlaylist = new Playlist();
        //        NewPlaylist.P_Name = NewPlaylistName;
        //        _sqlAccess.AddPlaylist(NewPlaylist, _userId);
        //        Dispatcher.BeginInvoke(new Action(delegate()
        //        {
        //            _songList = _sqlAccess.GetSongsForUser(_userId);
        //            SongDataGrid.ItemsSource = _songList;
        //            _playlistList = _sqlAccess.GetPlaylistsForUser(_userId);
        //            PlaylistBox.ItemsSource = _playlistList;
        //        }));
        //        PlaylistNameBox.Text = "";
        //        AddPlayistSection.Visibility = Visibility.Collapsed;
        //    }
        //}

        //private void CancelList_Click(object sender, RoutedEventArgs e)
        //{
        //    PlaylistNameBox.Text = "";
        //    AddPlayistSection.Visibility = Visibility.Collapsed;
        //}

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

        //Drag and Drop Stuff
        public Point startPoint { get; set; }

        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void SongDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
            if (e.ClickCount > 1) //must handle doubleclick here because this event eats the doubleclick event we put in the xaml
            {
                IsPlaying = true;
                _localPlayer.Stop();
                CurrentSongIndex = SongDataGrid.SelectedIndex;
                Song s = (Song)SongDataGrid.SelectedItem;
                _localPlayer.Play(s.S_Path + _blobAccess.GetSaS(), s.S_Length);
                bool EventSet = false;
                while (!EventSet)
                {
                    if (_localPlayer.GetWaveOut() != null)
                    {
                        _localPlayer.GetWaveOut().PlaybackStopped += MainWindow_PlaybackStopped;
                        EventSet = true;
                    }
                }
            }
        }

        private void SongDataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                DataGrid dGrid = sender as DataGrid;
                List<Song> dragSongs = dGrid.SelectedItems.Cast<Song>().ToList<Song>();
                DataObject songsObject = new DataObject("songs", dragSongs);

                DragDrop.DoDragDrop(SongDataGrid, songsObject, DragDropEffects.Move);
            } 
        }

        private void PlaylistBox_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("songs") ||
                sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void PlaylistBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("songs"))
            {
                using (var context = new CloudMp3Context())
                {
                    List<Song> draggedSongs = e.Data.GetData("songs") as List<Song>;
                    string playlistName = ((Label)sender).Content.ToString();

                    Playlist pl = context.Playlists.Where(x => x.P_Name == playlistName).FirstOrDefault();
                    foreach (Song s in draggedSongs)
                    {
                        pl.Songs.Add(context.Songs.Where(x => x.S_Id == s.S_Id).FirstOrDefault());
                    }
                    context.SaveChanges();
                }
            }
        }
        //End Drag and Drop Stuff

        private void Exit_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void RepeatOne_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void RepeatOne_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RepeatAll = false;
            RepAll.IsChecked = false;
            RepeatOne = !RepeatOne;
            e.Handled = true;
        }

        private void RepeatAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loggedIn)
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void RepeatAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RepeatOne = false;
            RepOne.IsChecked = false;
            RepeatAll = !RepeatAll;
            e.Handled = true;
        }

        private void VolumeUp_Click(object sender, RoutedEventArgs e)
        {
            VolBar.Value += 5;
        }

        private void VolumeDown_Click(object sender, RoutedEventArgs e)
        {
            if (VolBar.Value <= 5)
            {
                VolBar.Value = 0;
            }
            else
            {
                VolBar.Value -= 5;
            }
        }
        
    }
}
