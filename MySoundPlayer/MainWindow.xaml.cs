using MySoundPlayer.Audio;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System;
using System.Windows.Threading;
using System.Windows.Input;

namespace MySoundPlayer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ObservableCollection<Cue> Cues = new ObservableCollection<Cue>(); // Liste für Sounddateien
        private List<string> AudioDevices; // Liste für Audioausgabegeräte
        private DispatcherTimer playbackTimer;
        DispatcherTimer sliderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };

        // Konstruktor der MainWindow-Klasse
        public MainWindow()
        {
            InitializeComponent();

            //Startwerte für das Programm
            Cues.Add(new Soundfile("")); // Null Referenz für die Sounddatei initialisieren
            SoundListBox.ItemsSource = Cues;
            var sf = Cues.ElementAt(0) as Soundfile; // Erstes Element in der Liste als Soundfile initialisieren
            AudioDevices = sf.GetAudioDevices(); // Audioausgabegeräte abrufen
            PopulateAudioDevices();
            Cues.Clear(); // Leere Liste initialisieren, damit die ComboBox nicht leer ist
            
            StartSliderUpdateTimer();

            VolumeLabel.Content = $"{(int)VolumeSlider.Value} %";
            TrackVolumeLabel.Content = $"{(int)TrackVolume.Value} %";

            TrackStart.Value = 0;
            TrackEnd.Value = 0;
            TrackStartLabel.Content = "0 s";
            TrackEndLabel.Content = "0 s";

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            CommandBindings.Add(new CommandBinding(PlayCommand, PlayCommand_Executed));
            InputBindings.Add(new KeyBinding(PlayCommand, new KeyGesture(Key.Space)));


        }
        //Router für Tastendrücke
        public static readonly RoutedUICommand PlayCommand = new RoutedUICommand("PlayCommand", "PlayCommand", typeof(MainWindow));

        private void PlayCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlaySoundButton_Click(OKButton, null); // Optional direkt weiterleiten
        }

        /// <summary>
        /// Event-Handler für Tastendrücke im Hauptfenster
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Space:
                    e.Handled = true; // Standardverhalten (z. B. Button drücken) verhindern
                    PlayCommand.Execute(null, this); // Ausgewählten Track abspielen "Go"
                    break;

                case Key.Escape:
                    e.Handled = true; // Standardverhalten (z. B. Fenster schließen) verhindern
                    StopAllSoundsButton_Click(OKButton, null); // Alle Sounds stoppen "Stop All Sound"
                    break;
                case Key.L:
                    e.Handled = true; // Standardverhalten (z. B. Button drücken) verhindern
                    AddSoundButton_Click(OKButton, null); // Sound der Liste hinzufügen
                    break;
                default:
                    // Andere Tasten ignorieren
                    break;
            }
        }

        /// <summary>
        /// Startet den Timer, der die aktuelle Positionen für alle Trracks aktualisiert
        /// </summary>
        private void StartSliderUpdateTimer()
        {
            sliderTimer.Tick += delegate (object sender, EventArgs e)
            {
                foreach (var Cue in Cues)
                {
                    if (Cue is Soundfile sf)
                    {
                        if (sf.IsPlaying)
                        {
                            sf.CurrentPosition = sf.AudioFileCurrentTime;
                        }
                    }
                }
            };
            sliderTimer.Start();
        }

        /// <summary>
        /// Startet die Wiedergabe der Sounddatei an der angegebenen Position in der Liste
        /// </summary>
        /// <param name="Index"></param>
        public void StartTrack(int Index)
        {
            Soundfile sf = SoundListBox.Items.GetItemAt(Index) as Soundfile; // Nächste Sounddatei in der Liste abspielen (nicht selektierte Sounddatei)
            sf.SetVolume((float)(VolumeSlider.Value / 100) * (float)(TrackVolume.Value / 100));
            sf.Play(); // Abspielen der Sounddatei
        }
        /// <summary>
        /// Event-Handler für den Klick auf den "Play Sound" Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {

            if (SoundListBox.SelectedItem is Soundfile sf)
            {
                if (sf.DisplayName == "Unbenannt")
                {
                    return;
                }
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

        /// <summary>
        /// Event-Handler für den Klick auf den "Stop All Sounds" Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopAllSoundsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Soundfile sf in Cues)
            {
                if (sf.IsPlaying)
                {
                    sf.Stop(); // Stoppt den Sound
                    sf.SetStart(null); // Setzt den Startzeitpunkt zurück
                }
            }
        }

        /// <summary>
        /// Event-Handler für den Klick auf den "Add Sound" Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddSoundButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sounddatei auswählen",
                Filter = "Audio-Dateien (*.wav;*.mp3;*.mp4)|*.wav;*.mp3;*.mp4|Alle Dateien (*.*)|*.*",
                Multiselect = true //Todo: Mehrfachauswahl implemenentieren
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Soundfile sf;
                foreach (var selectedFile in openFileDialog.FileNames)
                {
                    sf = new Soundfile(selectedFile);
                    sf.PlaybackFinished += Soundfile_PlaybackFinished;
                    Cues.Add(sf);
                }
                SoundListBox.Items.Refresh(); // damit neue Elemente sofort sichtbar werden
                SoundListBox.SelectedIndex = SoundListBox.Items.Count - 1; // Setzt den Fokus auf das letzte Element in der Liste
                
                sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
                if (sf != null)
                {
                    TrackStart.Maximum = (int)sf.Duration; // Setzt das Maximum des Startzeitpunkt-Sliders auf die Dauer der Datei
                    TrackStart.Value = 0; // Setzt den Startzeitpunkt auf 0
                    TrackEnd.Maximum = (int)sf.Duration; // Setzt das Maximum des Endzeitpunkt-Sliders auf die Dauer der Datei
                    TrackEnd.Value = sf.Duration; // Setzt den Endzeitpunkt auf Ende der Datei
                }
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
            float MasterVolume = (float)(VolumeSlider.Value / 100);

            foreach (Soundfile sf in Cues)
            {
                sf.SetVolume(sf.TrackVolume * MasterVolume); // Setzt die Lautstärke der Sounddatei
            }
        }

        /// <summary>
        /// Füllt die ComboBox mit den verfügbaren Audioausgabegeräten
        /// </summary>
        private void PopulateAudioDevices()
        {
            var Cue = Cues.ElementAt(0); // Erstes Element in der Liste als Soundfile initialisieren
            if (Cue is Soundfile sf) { 
                AudioDevices = sf.GetAudioDevices(); // Audioausgabegeräte abrufen
                cmbAudioDevices.ItemsSource = AudioDevices; // ComboBox mit den Geräten füllen
                if (AudioDevices.Count > 0)
                    cmbAudioDevices.SelectedIndex = 0; // Erstes Gerät auswählen
            }
        }

        /// <summary>
        /// Event-Handler für die Auswahl eines Audiogeräts in der ComboBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der Liste
            }
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.SetOutputDevice(cmbAudioDevices.SelectedIndex); // Setzt das ausgewählte Audiogerät der Sounddatei
        }

        /// <summary>
        /// Event-Handler für die Auswahl einer Sounddatei in der ListBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der liste
            }
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            cmbAudioDevices.SelectedIndex = sf.UsedAudiodevice; // Zeigt das ausgewählte Audiogerät der Sounddatei an
            TrackVolume.Value = sf.TrackVolume * 100; // Setzt die Lautstärke der Sounddatei auf den Track Volume Slider
            TrackStart.Maximum = (int)sf.Duration; // Setzt das Maximum des Startzeitpunkt-Sliders auf die Dauer der Datei
            TrackStart.Value = 0; // Setzt den Startzeitpunkt auf 0
            TrackEnd.Maximum = (int)sf.Duration; // Setzt das Maximum des Endzeitpunkt-Sliders auf die Dauer der Datei
            TrackEnd.Value = sf.Duration; // Setzt den Endzeitpunkt auf Ende der Datei
        }

        /// <summary>
        /// Event-Handler für die Änderung der Track-Lautstärke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TrackVolumeLabel != null && IsLoaded)
                TrackVolumeLabel.Content = $"{(int)TrackVolume.Value} %";

            if (SoundListBox.SelectedIndex < 0) return;

            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.TrackVolume = (float)(TrackVolume.Value / 100); // Setzt die Track-Lautstärke der Sounddatei
            sf.SetVolume(sf.TrackVolume * (float)(VolumeSlider.Value / 100)); // Setzt die kombinierte Lautstärke der Sounddatei inklusive Master
        }

        /// <summary>
        /// Event-Handler für das Schließen des Fensters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Timer stoppen, um Ressourcen freizugeben
            sliderTimer.Stop();
            playbackTimer?.Stop();
            // Alle Sounddateien stoppen und Ressourcen freigeben
            foreach (Soundfile sf in Cues)
            {
                sf.Stop();
                sf.Dispose(); // Ressourcen freigeben
            }
        }

        /// <summary>
        /// Event-Handler für das Aktivieren des Auto-Play-Next-Features
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Event-Handler für das Deaktivieren des Auto-Play-Next-Features
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Event-Handler für das Beenden der Wiedergabe einer Sounddatei
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Soundfile_PlaybackFinished(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var currentFile = sender as Soundfile;
                if (currentFile != null && currentFile.IsAutoPlayNext)
                {
                    var currentIndex = Cues.IndexOf(currentFile);
                    if (currentIndex + 1 < Cues.Count)
                    {
                        SoundListBox.SelectedIndex = currentIndex + 1;
                        (SoundListBox.SelectedItem as Soundfile)?.Play();
                    }
                }
            });
        }

        /// <summary>
        /// Event-Handler für die Änderung des Startzeitpunkts des Tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackStartSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der Liste
            }
            TrackStartLabel.Content = $"{TrackStart.Value} s"; // Aktualisiert die Anzeige des Startzeitpunkts
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.SetStart(TimeSpan.FromSeconds((double)TrackStart.Value)); // Setzt den Startzeitpunkt der Sounddatei
        }
        
        /// <summary>
        /// Event-Handler für die Änderung des Endzeitpunkts des Tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackEndSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der Liste
            }

            TrackEndLabel.Content = $"{TrackEnd.Value} s"; // Aktualisiert die Anzeige des Endzeitpunkts
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.SetEnd(TimeSpan.FromSeconds((double)TrackEnd.Value)); // Setzt den Endzeitpunkt der Sounddatei
        }

        private void FadeAndStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der Liste
            }
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.FadeOutAndStop(); // Fadet den Sound aus und stoppt ihn
        }
    }
}
