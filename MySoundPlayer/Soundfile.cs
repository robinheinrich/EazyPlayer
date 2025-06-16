using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MySoundPlayer.Audio
{
    public class Soundfile : Cue, IDisposable, INotifyPropertyChanged
    {
        public event EventHandler PlaybackFinished;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private IWavePlayer outputDevice;
        private AudioFileReader audioFile;
        private ISampleProvider trimmedProvider;
        private VolumeSampleProvider volumeProvider;

        public TimeSpan startTime { get; set; } = TimeSpan.Zero;
        public TimeSpan endTime { get; set; }

        public string DisplayName { get; private set; }
        public bool IsPlaying { get; private set; } = false;
        public int UsedAudiodevice { get; internal set; } = 1;
        public float TrackVolume { get; set; } = 1.0f; // Default volume set to 1.0 (100%)
        public double Duration => audioFile?.TotalTime.TotalSeconds ?? 0;
        public double AudioFileCurrentTime => audioFile?.CurrentTime.TotalSeconds ?? 0;

        private bool _isAutoPlayNext;
        public bool IsAutoPlayNext
        {
            get => _isAutoPlayNext;
            set
            {
                if (_isAutoPlayNext != value)
                {
                    _isAutoPlayNext = value;
                    OnPropertyChanged(nameof(IsAutoPlayNext));
                }
            }
        }

        private double _currentPosition;
        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (Math.Abs(_currentPosition - value) > 0.01)
                {
                    _currentPosition = value;
                    OnPropertyChanged(nameof(CurrentPosition));
                    Seek(TimeSpan.FromSeconds(_currentPosition));
                }
            }
        }

        public Soundfile(string filePath)
        {
            Load(filePath);
        }

        public Soundfile(string filePath, TimeSpan? start = null, TimeSpan? end = null)
        {
            Load(filePath, start, end);
        }

        public void Load(string filePath, TimeSpan? start = null, TimeSpan? end = null)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            Dispose();

            audioFile = new AudioFileReader(filePath);

            double startSec = start?.TotalSeconds ?? 0;
            double endSec = end?.TotalSeconds ?? audioFile.TotalTime.TotalSeconds;
            trimmedProvider = new OffsetSampleProvider(audioFile)
            {
                SkipOver = TimeSpan.FromSeconds(startSec),
                Take = TimeSpan.FromSeconds(endSec - startSec)
            };
            volumeProvider = new VolumeSampleProvider(trimmedProvider);
            volumeProvider.Volume = 1.0f;

            trimmedProvider = volumeProvider;
            DisplayName = System.IO.Path.GetFileName(filePath);

            try
            {
                outputDevice = CreateOutputDevice();
                outputDevice.PlaybackStopped += OnPlaybackStopped;
                outputDevice.Init(trimmedProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Initialisieren des Audiogeräts: {ex.Message}");
            }
        }

        private IWavePlayer CreateOutputDevice()
        {
            return new WaveOutEvent { DeviceNumber = UsedAudiodevice };
        }

        public void Seek(TimeSpan position)
        {
            if (audioFile != null)
                audioFile.CurrentTime = position;
        }

        public void SetOutputDevice(int deviceNumber)
        {
            UsedAudiodevice = deviceNumber;
            if (audioFile != null && !IsPlaying)
            {
                outputDevice?.Stop();
                outputDevice?.Dispose();

                outputDevice = CreateOutputDevice();
                outputDevice.PlaybackStopped += OnPlaybackStopped;
                outputDevice.Init(trimmedProvider);
            }
        }

        public async void FadeOutAndStop(float durationSeconds = 2.0f, int steps = 20)
        {
            if (volumeProvider == null || outputDevice == null)
                return;

            float initialVolume = volumeProvider.Volume;
            float step = initialVolume / steps;
            int delay = (int)(durationSeconds * 1000 / steps);

            for (int i = 0; i < steps; i++)
            {
                volumeProvider.Volume -= step;
                await Task.Delay(delay);
            }

            Stop(); // Danach vollständig stoppen
        }



        public void SetStart(TimeSpan? start = null)
        {
            if (audioFile == null) return;

            double startSec = start?.TotalSeconds ?? 0;
            trimmedProvider = new OffsetSampleProvider(audioFile)
            {
                SkipOver = TimeSpan.FromSeconds(startSec)
            };

            outputDevice?.Init(trimmedProvider);
        }

        public void SetEnd(TimeSpan? end = null)
        {
            if (audioFile == null) return;

            double endSec = end?.TotalSeconds ?? audioFile.TotalTime.TotalSeconds;
            var offset = trimmedProvider as OffsetSampleProvider;

            if (offset != null)
            {
                offset.Take = TimeSpan.FromSeconds(endSec - offset.SkipOver.TotalSeconds);
                outputDevice?.Init(offset);
            }
        }

        public override void Play()
        {
            Console.WriteLine($"Gebe {ToString()} wieder.");
            outputDevice?.Play();
            IsPlaying = true;
        }

        public override void  Stop()
        {
            outputDevice?.Stop();
            IsPlaying = false;
        }

        public void Pause()
        {
            outputDevice?.Pause();
            IsPlaying = false;
        }

        public void Rewind()
        {
            if (audioFile == null)
                return;

            audioFile.Position = 0;

            // TrimmedProvider neu erzeugen (damit SkipOver etc. korrekt greifen)
            trimmedProvider = new OffsetSampleProvider(audioFile)
            {
                SkipOver = TimeSpan.FromSeconds(startTime.TotalSeconds),
                Take = TimeSpan.FromSeconds(endTime == TimeSpan.Zero
                    ? audioFile.TotalTime.TotalSeconds - startTime.TotalSeconds
                    : endTime.TotalSeconds - startTime.TotalSeconds)
            };

            outputDevice?.Stop();
            outputDevice?.Dispose();

            outputDevice = CreateOutputDevice();
            outputDevice.PlaybackStopped += OnPlaybackStopped;

            try
            {
                outputDevice.Init(trimmedProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Re-Init nach Rewind: {ex.Message}");
            }
        }


        public void SetVolume(float volume)
        {
            if (audioFile != null)
                audioFile.Volume = volume;
        }

        internal int GetVolume()
        {
            return (int)(audioFile?.Volume * 100);
        }

        public override string ToString()
        {
            return DisplayName ?? "Unbenannt";
        }

        public List<string> GetAudioDevices()
        {
            var devices = new List<string>();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                devices.Add($"{capabilities.ProductName} ({capabilities.Channels} channels)");
            }
            return devices;
        }

        public string GetUsedAudioDeviceByID(int id)
        {
            if (id < -1 || id >= WaveOut.DeviceCount)
                return "Unbekanntes Gerät";
            var capabilities = WaveOut.GetCapabilities(id);
            return $"{capabilities.ProductName} ({capabilities.Channels} channels)";
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            IsPlaying = false;
            if (audioFile != null && audioFile.Position >= audioFile.Length)
            {
                Rewind(); //Datei auf Anfang zurücksetzen
                PlaybackFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            outputDevice?.Stop();
            outputDevice?.Dispose();
            audioFile?.Dispose();
            outputDevice = null;
            audioFile = null;
        }
    }
}
