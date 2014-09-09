using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudSoundMetroV2.Mp3Players
{
    public class StreamMp3Player : IMp3Player, INotifyPropertyChanged
    {
        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }

        public StreamMp3Player()
        {
            playbackState = StreamingPlaybackState.Stopped;
            timer1 = new System.Timers.Timer();
            timer1.Interval = 250;
            timer1.Elapsed += new System.Timers.ElapsedEventHandler(timer1_Tick);
            stopwatch = new System.Diagnostics.Stopwatch();
            _volume = 0.5f;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private BufferedWaveProvider bufferedWaveProvider;
        private IWavePlayer waveOut;
        private volatile StreamingPlaybackState playbackState;
        private volatile bool fullyDownloaded;
        private HttpWebRequest webRequest;
        private VolumeWaveProvider16 volumeProvider;
        private System.Timers.Timer timer1;
        private System.Diagnostics.Stopwatch stopwatch;

        private float _volume;
        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                if (volumeProvider != null)
                {
                    volumeProvider.Volume = _volume;
                }
            }
        }

        private int _songProgress;
        public int SongProgress
        {
            get { return _songProgress; }
            set
            {
                _songProgress = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SongProgress"));
                }
            }
        }

        private int _songLength;
        public int SongLength
        {
            get { return _songLength; }
            set
            {
                _songLength = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SongLength"));
                }
            }
        }

        private int _elapsedMiliseconds;
        public int ElapsedMiliseconds
        {
            get { return _elapsedMiliseconds; }
            set
            {
                _elapsedMiliseconds = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ElapsedMiliseconds"));
                }
            }
        }

        private void StreamMp3(object state)
        {
            fullyDownloaded = false;
            string path = (string)state;
            webRequest = (HttpWebRequest)WebRequest.Create(path);
            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse)webRequest.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    Console.WriteLine(e.Message);
                }
                return;
            }

            var buffer = new byte[16384 * 4];

            IMp3FrameDecompressor decompressor = null;
            try
            {
                using (var responseStream = resp.GetResponseStream())
                {
                    var readFullyStream = new ReadFullyStream(responseStream);
                    do
                    {
                        if (IsBufferNearlyFull)
                        {
                            Console.WriteLine("buffer getting full, sleeping.");
                            Thread.Sleep(500);
                        }
                        else
                        {
                            Mp3Frame frame;
                            try
                            {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);
                                if (frame == null)
                                {
                                    throw new EndOfStreamException();
                                }
                            }
                            catch (EndOfStreamException)
                            {
                                Console.WriteLine("Stream Fully Downloaded");
                                fullyDownloaded = true;
                                break;
                            }
                            catch (WebException)
                            {
                                Console.WriteLine("Stream Download Stopped");
                                break;
                            }
                            if (decompressor == null)
                            {
                                decompressor = CreateFrameDecompressor(frame);
                                bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                                bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(20);
                            }
                            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                        }

                    } while (playbackState != StreamingPlaybackState.Stopped);
                    decompressor.Dispose();
                }
            }
            finally
            {
                if (decompressor != null)
                {
                    decompressor.Dispose();
                }
            }
        }

        private bool IsBufferNearlyFull
        {
            get
            {
                return bufferedWaveProvider != null &&
                   bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                   < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
            }
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        public void Play(string path, int songLenth) //Clean me up please
        {
            SongLength = songLenth;
            if (playbackState == StreamingPlaybackState.Stopped)
            {
                playbackState = StreamingPlaybackState.Buffering;
                bufferedWaveProvider = null;
                ThreadPool.QueueUserWorkItem(StreamMp3, path);
                timer1.Enabled = true;
            }
            else if (playbackState == StreamingPlaybackState.Paused)
            {
                playbackState = StreamingPlaybackState.Buffering;
            }
            else if (playbackState == StreamingPlaybackState.Playing)
            {
                Stop();
                playbackState = StreamingPlaybackState.Buffering;
                bufferedWaveProvider = null;
                ThreadPool.QueueUserWorkItem(StreamMp3, path);
                timer1.Enabled = true;
            }
            else if (playbackState == StreamingPlaybackState.Buffering)
            {
                Stop();
                playbackState = StreamingPlaybackState.Buffering;
                bufferedWaveProvider = null;
                ThreadPool.QueueUserWorkItem(StreamMp3, path);
                timer1.Enabled = true;
            }
        }

        public void Stop()
        {
            if (playbackState != StreamingPlaybackState.Stopped)
            {
                stopwatch.Stop();
                stopwatch.Reset();
                if (!fullyDownloaded)
                {
                    webRequest.Abort();
                }

                playbackState = StreamingPlaybackState.Stopped;
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                timer1.Enabled = false;
                Thread.Sleep(500); // n.b. streaming thread may not yet have exited
                SongLength = 0;
                UpdateTimer(stopwatch.Elapsed.TotalSeconds);
            }
        }

        public void Pause()
        {
            if (playbackState == StreamingPlaybackState.Playing || playbackState == StreamingPlaybackState.Buffering)
            {
                waveOut.Pause();
                stopwatch.Stop();
                Console.WriteLine(String.Format("User requested Pause, waveOut.PlaybackState={0}", waveOut.PlaybackState));
                playbackState = StreamingPlaybackState.Paused;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (playbackState != StreamingPlaybackState.Stopped)
            {
                if (waveOut == null && bufferedWaveProvider != null)
                {
                    Console.WriteLine("waveOut Created");
                    waveOut = CreateWaveOut();
                    waveOut.PlaybackStopped += OnPlaybackStopped;
                    volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                    volumeProvider.Volume = _volume;
                    waveOut.Init(volumeProvider);
                }
                else if (bufferedWaveProvider != null)
                {
                    var bufferedSeconds = bufferedWaveProvider.BufferedDuration.TotalSeconds;
                    UpdateTimer(stopwatch.Elapsed.TotalMilliseconds);
                    if (bufferedSeconds < 0.5 && playbackState == StreamingPlaybackState.Playing && !fullyDownloaded)
                    {
                        pause();
                    }
                    else if (bufferedSeconds > 4 && playbackState == StreamingPlaybackState.Buffering)
                    {
                        play();

                    }
                    else if (fullyDownloaded && bufferedSeconds == 0)
                    {
                        Console.WriteLine("Reached end of stream");

                        Stop();
                    }
                }
            }
        }

        private void play()
        {
            waveOut.Play();
            stopwatch.Start();
            Console.WriteLine(String.Format("Started playing, waveOut.PlaybackState={0}", waveOut.PlaybackState));
            playbackState = StreamingPlaybackState.Playing;
        }

        private void pause()
        {
            playbackState = StreamingPlaybackState.Buffering;
            stopwatch.Stop();
            waveOut.Pause();
            Console.WriteLine(String.Format("Paused to buffer, waveOut.PlaybackState={0}", waveOut.PlaybackState));
        }


        private IWavePlayer CreateWaveOut()
        {
            return new WaveOut();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("Playback Stopped");
            if (e.Exception != null)
            {
                Console.WriteLine(String.Format("Playback Error {0}", e.Exception.Message));
            }
        }

        private void UpdateTimer(double totalMiliseconds)
        {
            ElapsedMiliseconds = (int)totalMiliseconds;
            SongProgress = (SongLength != 0) ? (int)(((double)ElapsedMiliseconds / (double)SongLength) * 1000) : 0;
        }

        public IWavePlayer GetWaveOut()
        {
            if (waveOut != null)
            {
                return waveOut;
            }
            else
            {
                return null;
            }
        }
    }
}
