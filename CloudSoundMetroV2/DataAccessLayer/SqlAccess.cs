using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudSoundMetroV2.DataAccessLayer
{
    public class SqlAccess
    {
        public SqlAccess()
        {

        }

        public bool ValidateUserName(string usrName, string pass)
        {
            bool valid = false;
            using (var context = new CloudMp3Context())
            {
                var query = (from u in context.Users
                             where u.U_UserName == usrName
                             select u).SingleOrDefault();
                if (query != null)
                {
                    if (pass == query.U_Password)
                    {
                        valid = true;
                    }
                }
            }
            return valid;
        }

        public int GetUserID(string usrName)
        {
            int id;
            using (var context = new CloudMp3Context())
            {
                var query = (from u in context.Users
                             where u.U_UserName == usrName
                             select u).SingleOrDefault();
                id = query.U_Id;
            }
            return id;
        }

        public ObservableCollection<Song> GetSongsForUser(int userId)
        {
            ObservableCollection<Song> userSongs = new ObservableCollection<Song>();

            using (var context = new CloudMp3Context())
            {
                var query = (from s in context.Songs
                             where s.S_OwnerId == userId
                             select s).ToList<Song>();
                foreach (Song x in query)
                {
                    userSongs.Add(x);
                }
            }
            userSongs = new ObservableCollection<Song>(userSongs.OrderBy(s => s.S_Artist));
            return userSongs;
        }

        public ObservableCollection<Playlist> GetPlaylistsForUser(int userId)
        {
            ObservableCollection<Playlist> userPlaylists = new ObservableCollection<Playlist>();

            using (var context = new CloudMp3Context())
            {
                var query = (from p in context.Playlists
                             where p.P_OwnerId == userId
                             select p).ToList<Playlist>();
                foreach (Playlist x in query)
                {
                    userPlaylists.Add(x);
                }
            }
            return userPlaylists;
        }

        public ObservableCollection<Song> GetSongsInPlaylist(int playlistId)
        {
            ObservableCollection<Song> playlistSongs = new ObservableCollection<Song>();

            using (var context = new CloudMp3Context())
            {
                var query = (from p in context.Playlists
                             where p.P_Id == playlistId
                             select p.Songs).SingleOrDefault();

                foreach (Song x in query)
                {
                    playlistSongs.Add(x);
                }
            }
            return playlistSongs;
        }

        public void RemoveSongFromPlaylist(int songId, int playlistId)
        {
            using (var context = new CloudMp3Context())
            {
                Song songQuery = (from s in context.Songs
                                  where s.S_Id == songId
                                  select s).First();
                Playlist playlistQuery = (from p in context.Playlists
                                          where p.P_Id == playlistId
                                          select p).First();
                songQuery.Playlists.Remove(playlistQuery);
                playlistQuery.Songs.Remove(songQuery);
                context.SaveChanges();
                GetSongsInPlaylist(playlistQuery.P_Id);
            }
        }

        public void AddSongToPlaylist(int songId, int playlistId)
        {
            using (var context = new CloudMp3Context())
            {
                Song song = (from s in context.Songs
                             where s.S_Id == songId
                             select s).SingleOrDefault();

                Playlist playlist = (from p in context.Playlists
                                     where p.P_Id == playlistId
                                     select p).SingleOrDefault();

                //songQuery.Playlists.Add(playlistQuery);
                playlist.Songs.Add(song);
                context.SaveChanges();

            }
        }

        public void RemovePlaylist(int playlistId, int userId)
        {
            int ID = userId;
            using (var context = new CloudMp3Context())
            {
                Playlist playlistQuery = (from p in context.Playlists
                                          where p.P_Id == playlistId
                                          select p).First();
                foreach (Song s in playlistQuery.Songs)
                {
                    s.Playlists.Remove(playlistQuery);
                }
                context.Playlists.Remove(playlistQuery);
                context.SaveChanges();
                GetPlaylistsForUser(ID);
            }
        }

        public void RenamePlaylist(int playlistId, int userId, string newName)
        {
            int ID = userId;
            using (var context = new CloudMp3Context())
            {
                Playlist playlistQuery = (from p in context.Playlists
                                          where p.P_Id == playlistId
                                          where p.P_OwnerId == ID
                                          select p).First();
                playlistQuery.P_Name = newName;
                context.SaveChanges();
                GetPlaylistsForUser(ID);
            }
        }

        public void AddPlaylist(Playlist playlist, int userId)
        {
            int ID = userId;
            using (var context = new CloudMp3Context())
            {
                playlist.P_OwnerId = ID;
                context.Playlists.Add(playlist);
                context.SaveChanges();
                GetPlaylistsForUser(ID);
            }

        }
    }
}
