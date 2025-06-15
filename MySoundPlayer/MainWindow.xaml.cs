using MySoundPlayer.Audio;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System;
using System.Windows.Threading;

namespace MySoundPlayer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ObservableCollection<Soundfile> Soundfiles = new ObservableCollection<Soundfile>(); // Liste für Sounddateien
        private List<string> AudioDevices; // Liste für Audioausgabegeräte
        private DispatcherTimer playbackTimer;
        DispatcherTimer sliderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };

        // Konstruktor der MainWindow-Klasse
        public MainWindow()
        {
            InitializeComponent();

            //Startwerte für das Programm
            VolumeLabel.Content = $"{(int)VolumeSlider.Value} %";
            TrackVolumeLabel.Content = $"{(int)TrackVolume.Value} %";
            Soundfiles.Add(new Soundfile("")); // Null Referenz für die Sounddatei initialisieren
            SoundListBox.ItemsSource = Soundfiles;
            AudioDevices = Soundfiles.ElementAt(0).GetAudioDevices(); // Audioausgabegeräte abrufen
            PopulateAudioDevices();
            Soundfiles.Clear(); // Leere Liste initialisieren, damit die ComboBox nicht leer ist
            StartSliderUpdateTimer();
        }

        private void StartSliderUpdateTimer()
        {
            sliderTimer.Tick += delegate (object sender, EventArgs e)
            {
                foreach (var sf in Soundfiles)
                {
                    if (sf.IsPlaying)
                    {
                        sf.CurrentPosition = sf.AudioFileCurrentTime;
                    }
                        
                }
            };
            sliderTimer.Start();
        }

        public void StartTrack(int Index)
        {
            Soundfile sf = SoundListBox.Items.GetItemAt(Index) as Soundfile; // Nächste Sounddatei in der Liste abspielen (nicht selektierte Sounddatei)
            //Console.WriteLine($"Spiele Sound: {sf.DisplayName} auf Gerät: {sf.GetUsedAudioDeviceByID(sf.UsedAudiodevice)} mit Lautstärke {(TrackVolume.Value * VolumeSlider.Value) / 100}");
            sf.SetVolume((float)(VolumeSlider.Value / 100) * (float)(TrackVolume.Value / 100));
            sf.Play(); // Abspielen der Sounddatei
        }

        private void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {

            if (SoundListBox.SelectedItem is Soundfile sf)
            {
                if (sf.DisplayName == "Unbenannt")
                {
                    return;
                }
                //Console.WriteLine($"Spiele Sound: {sf.DisplayName} auf Gerät: {sf.GetUsedAudioDeviceByID(sf.UsedAudiodevice)} mit Lautstärke {(TrackVolume.Value * VolumeSlider.Value) / 100}");
                sf.SetVolume((float)(VolumeSlider.Value / 100) * (float)(TrackVolume.Value / 100));
                sf.Play(); // Optional: abspielen
                int SelectedSoundfileIndex = SoundListBox.SelectedIndex; // Index der ausgewählten Sounddatei

                if (SelectedSoundfileIndex >= 0 && SelectedSoundfileIndex < SoundListBox.Items.Count -1)
                {
                    SoundListBox.SelectedIndex++; // Setzt den Fokus auf die nächste Sounddatei in der Liste
                } else
                {
                    SoundListBox.SelectedIndex = -1; // Wenn am Ende der Liste, zurück zum Anfang
                }
            }
            else
            {
                MessageBox.Show("Bitte eine Sounddatei aus der Liste auswählen.");
            }
        }

        private void StopAllSoundsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Soundfile sf in Soundfiles)
            {
                if (sf.IsPlaying)
                {
                    sf.Stop(); // Stoppt den Sound
                    sf.SetStart(null); // Setzt den Startzeitpunkt zurück
                }
            }
        }

        private void AddSoundButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sounddatei auswählen",
                Filter = "Audio-Dateien (*.wav;*.mp3;*.mp4)|*.wav;*.mp3;*.mp4|Alle Dateien (*.*)|*.*",
                Multiselect = false //Todo: Mehrfachauswahl implemenentieren
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFile = openFileDialog.FileName;
                // Beispiel: neue Soundfile zur Liste hinzufügen
                Soundfile sf = new Soundfile(selectedFile);
                sf.PlaybackFinished += Soundfile_PlaybackFinished;
                Soundfiles.Add(sf);
                SoundListBox.Items.Refresh(); // damit neue Elemente sofort sichtbar werden
                SoundListBox.SelectedIndex = SoundListBox.Items.Count - 1; // Setzt den Fokus auf das neu hinzugefügte Element
            }
        }

        /// <summary>
        /// Master Volume Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeLabel != null && IsLoaded)
                VolumeLabel.Content = $"{(int)VolumeSlider.Value} %";
            foreach (Soundfile sf in Soundfiles)
            {
                sf.SetVolume((float)(VolumeSlider.Value / 100) * (float)(TrackVolume.Value / 100)); // Setzt die Lautstärke der Sounddatei
            }
        }

        private void PopulateAudioDevices()
        {
            AudioDevices = Soundfiles.ElementAt(0).GetAudioDevices(); // Audioausgabegeräte abrufen
            cmbAudioDevices.ItemsSource = AudioDevices; // ComboBox mit den Geräten füllen
            if (AudioDevices.Count > 0)
                cmbAudioDevices.SelectedIndex = 0; // Erstes Gerät auswählen
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der Liste
            }
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.SetOutputDevice(cmbAudioDevices.SelectedIndex); // Setzt das ausgewählte Audiogerät der Sounddatei
        }

        private void SoundListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der liste
            }
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            cmbAudioDevices.SelectedIndex = sf.UsedAudiodevice; // Zeigt das ausgewählte Audiogerät der Sounddatei an
            TrackVolume.Value = sf.GetVolume(); // Setzt die Lautstärke der Sounddatei auf den Track Volume Slider
        }

        private void TrackVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TrackVolumeLabel != null && IsLoaded)
                TrackVolumeLabel.Content = $"{(int)TrackVolume.Value} %";

            if (SoundListBox.SelectedIndex < 0) return;

            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.SetVolume((float)(TrackVolume.Value / 100)); // Setzt die Lautstärke der Sounddatei
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Timer stoppen, um Ressourcen freizugeben
            sliderTimer.Stop();
            playbackTimer?.Stop();
            // Alle Sounddateien stoppen und Ressourcen freigeben
            foreach (Soundfile sf in Soundfiles)
            {
                sf.Stop();
                sf.Dispose(); // Ressourcen freigeben
            }
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var soundfile = checkbox?.DataContext as Soundfile;
            if (soundfile != null)
            {
                soundfile.IsAutoPlayNext = true;
                // Weitere Logik
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var soundfile = checkbox?.DataContext as Soundfile;
            if (soundfile != null)
            {
                soundfile.IsAutoPlayNext = false;
                // Weitere Logik
            }
        }


        private void Soundfile_PlaybackFinished(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var currentFile = sender as Soundfile;
                if (currentFile != null && currentFile.IsAutoPlayNext)
                {
                    var currentIndex = Soundfiles.IndexOf(currentFile);
                    if (currentIndex + 1 < Soundfiles.Count)
                    {
                        SoundListBox.SelectedIndex = currentIndex + 1;
                        (SoundListBox.SelectedItem as Soundfile)?.Play();
                    }
                }
            });
        }
    }
}
