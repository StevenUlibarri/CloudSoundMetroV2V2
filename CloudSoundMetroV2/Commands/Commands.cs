using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CloudSoundMetroV2.Commands
{
    public class Commands
    {
        public static RoutedUICommand Login = new RoutedUICommand("Login Command", "Login", typeof(Commands));
        public static RoutedUICommand Logout = new RoutedUICommand("Logout Command", "Logout", typeof(Commands));
        public static RoutedUICommand DownloadSong = new RoutedUICommand("Download Song Command", "DownloadSong", typeof(Commands));
        public static RoutedUICommand UploadSong = new RoutedUICommand("Upload Song Command", "UploadSong", typeof(Commands));
        public static RoutedUICommand Play = new RoutedUICommand("Play and Pause Song", "Play", typeof(Commands));
        public static RoutedUICommand Stop = new RoutedUICommand("Stop Song", "Stop", typeof(Commands));
        public static RoutedUICommand Next = new RoutedUICommand("Next Song", "Next", typeof(Commands));
        public static RoutedUICommand Prev = new RoutedUICommand("Previous Song", "Prev", typeof(Commands));
    }
}
