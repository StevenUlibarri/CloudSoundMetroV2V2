using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudSoundMetroV2.Mp3Players
{
    public interface IMp3Player
    {
        void Play(string path, int length);
        void Stop();
        void Pause();
    }
}
