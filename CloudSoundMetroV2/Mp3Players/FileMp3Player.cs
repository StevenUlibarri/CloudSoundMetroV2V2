using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudSoundMetroV2.Mp3Players
{
    public class FileMp3Player : IMp3Player
    {
        public enum Mp3PlayerState
        {
            Playing,
            Stopped,
            Paused
        }

        private MediaFoundationReader _mp3Reader;
        private IWavePlayer _waveOutDevice;

        private Mp3PlayerState _playerState;

        public FileMp3Player()
        {
            _playerState = Mp3PlayerState.Stopped;
        }

        public void Play(string path, int length)
        {
            if (_playerState == Mp3PlayerState.Paused && path == null)
            {
                _playerState = Mp3PlayerState.Playing;
                _waveOutDevice.Play();
            }
            else
            {
                if (_playerState != Mp3PlayerState.Stopped)
                {
                    ClearPlayer();
                }
                _playerState = Mp3PlayerState.Playing;
                PlaySong(path);
            }
        }

        public void Stop()
        {
            if (_playerState != Mp3PlayerState.Stopped)
            {
                _playerState = Mp3PlayerState.Stopped;
                ClearPlayer();
            }
        }

        public void Pause()
        {
            if (_playerState == Mp3PlayerState.Playing)
            {
                _playerState = Mp3PlayerState.Paused;
                _waveOutDevice.Pause();
            }
        }

        private void ClearPlayer()
        {
            _waveOutDevice.Stop();
            _mp3Reader.Dispose();
            _mp3Reader = null;
            _waveOutDevice.Dispose();
            _waveOutDevice = null;
        }

        private void PlaySong(string path)
        {
            _playerState = Mp3PlayerState.Playing;
            _waveOutDevice = new WaveOut();
            _mp3Reader = new MediaFoundationReader(path);
            _waveOutDevice.Init(_mp3Reader);
            _waveOutDevice.Play();
        }
    }
}
