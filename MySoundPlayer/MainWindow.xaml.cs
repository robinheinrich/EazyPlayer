using EazsyPlayer.Audio;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;

namespace EazsyPlayer
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
        private Point _dragStartPoint;
        
        // Konstruktor der MainWindow-Klasse
        public MainWindow()
        {
            InitializeComponent();
            
            SoundListBox.DataContext = Cues;

            //Startwerte für das Programm
            Soundfile sf = new Soundfile("");
            AudioDevices = sf.GetAudioDevices(); // Audioausgabegeräte abrufen
            PopulateAudioDevices(AudioDevices);
            
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

            SoundListBox.PreviewMouseLeftButtonDown += SoundListBox_PreviewMouseLeftButtonDown;
            SoundListBox.PreviewMouseMove += SoundListBox_PreviewMouseMove;
            SoundListBox.DragEnter += SoundListBox_DragEnter;
            SoundListBox.Drop += SoundListBox_Drop;
            SoundListBox.DragOver += SoundListBox_DragOver;
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
            var cue = SoundListBox.Items.GetItemAt(Index); // Nächste Sounddatei in der Liste abspielen (nicht selektierte Sounddatei)
            if (cue.GetType() == typeof(Soundfile))
            {
                Soundfile sf = SoundListBox.Items.GetItemAt(Index) as Soundfile; // Nächste Sounddatei in der Liste abspielen (nicht selektierte Sounddatei)
                sf.SetVolume((float)(VolumeSlider.Value / 100) * (float)(TrackVolume.Value / 100));
                sf.Play(); // Abspielen der Sounddatei
            }
            else if (cue.GetType() == typeof(Command))
            {
                Command cmd = SoundListBox.Items.GetItemAt(Index) as Command; // Nächste Command in der Liste abspielen (nicht selektierte Command)
                cmd.Play(); // Abspielen des Commands
            }
            else
            {
                Console.WriteLine($"Fehler in der Cueliste. Cue Typ nicht erkannt: {cue.GetType()}"); // Debug-Ausgabe
            }
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
                int SelectedCueIndex = SoundListBox.SelectedIndex; // Index der ausgewählten Sounddatei

                if (SelectedCueIndex >= 0 && SelectedCueIndex < SoundListBox.Items.Count -1)
                {
                    SoundListBox.SelectedIndex++; // Setzt den Fokus auf die nächste Sounddatei in der Liste
                } else
                {
                    SoundListBox.SelectedIndex = -1; // Wenn am Ende der Liste, zurück zum Anfang
                }
            } 
            
            else if (SoundListBox.SelectedItem is Command cmd)
            {
                cmd.Play(); // Abspielen des Commands
                int SelectedCueIndex = SoundListBox.SelectedIndex; // Index des ausgewählten Commands
                if (SelectedCueIndex >= 0 && SelectedCueIndex < SoundListBox.Items.Count - 1)
                {
                    SoundListBox.SelectedIndex++; // Setzt den Fokus auf das nächste Command in der Liste
                }
                else
                {
                    SoundListBox.SelectedIndex = -1; // Wenn am Ende der Liste, zurück zum Anfang
                }
            }
            else
            {
                Console.WriteLine("Kein Cue ausgewählt."); // Debug-Ausgabe
            }
        }

        /// <summary>
        /// Event-Handler für den Klick auf den "Stop All Sounds" Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param="e"></param>
        private void StopAllSoundsButton_Click(object sender, RoutedEventArgs e)
        {
            //Wir wollen alle Sounds stoppen, also erstmal filtern und dann stoppen.
            var soundfiles = Cues.OfType<Soundfile>().ToList();
            foreach (Soundfile sf in soundfiles)
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
        /// <param="e"></param>
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
                SoundListBox_RefreshListContent(); // Aktualisiert die ListBox, um neue Cues anzuzeigen
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
        /// Event-Handler für den Klick auf den "Add Command" Button
        /// </summary>
        /// <param="sender"></param>
        /// <param="e"></param>
        private void AddCommand_Click(object sender, RoutedEventArgs e)
        {
            Cue CurrentCue = null;
            //Wenn die Liste nicht leer ist und ein Cue ausgewählt ist, wird der aktuell ausgewählte Cue in der Liste gesetzt
            if (Cues.Count() > 0 && SoundListBox.SelectedIndex != -1)
            {
                CurrentCue = Cues.ElementAt(SoundListBox.SelectedIndex); // Aktuell ausgewählter Cue in der Liste
            }

            // Wenn die Liste nicht leer ist aber kein Cue ausgewählt ist, wird der neue Cue am Ende der Liste hinzugefügt und das Target auf null gesetzt
            if (CurrentCue == null)
            {
                Cues.Add(new Command("Play", null, 0)); // Fügt ein neues Command am Ende der Liste hinzu
            }
            else
            {
                Cues.Insert(SoundListBox.SelectedIndex + 1, new Command("Play", CurrentCue, 0)); // Fügt ein neues Command direkt nach dem aktuell ausgewählten Element ein und setzt dieses als Target
            }
            SoundListBox_RefreshListContent(); // Aktualisiert die ListBox, um neue Cues anzuzeigen
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
            var soundfiles = Cues.OfType<Soundfile>().ToList();
            foreach (Soundfile sf in soundfiles)
            {
                sf.SetVolume(sf.TrackVolume * MasterVolume); // Setzt die Lautstärke der Sounddatei
            }
        }

        /// <summary>
        /// Füllt die ComboBox mit den verfügbaren Audioausgabegeräten
        /// </summary>
        private void PopulateAudioDevices(List<string> Audiodevices)
        {
                cmbAudioDevices.ItemsSource = AudioDevices; // ComboBox mit den Geräten füllen
                if (AudioDevices.Count > 0)
                    cmbAudioDevices.SelectedIndex = 0; // Erstes Gerät auswählen
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
            SoundListBox_RefreshListContent(); // Aktualisiert die ListBox, um neue Cues anzuzeigen
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
                SoundfileTab.IsEnabled = false; // Deaktiviert den Soundfile Tab, wenn keine Sounddatei ausgewählt ist
                return; // Keine Sounddatei ausgewählt in der liste
            }
            if (SoundListBox.SelectedItem is Soundfile sf) {
                SoundfileTab.IsEnabled = true; // Aktiviert den Soundfile Tab
                SideTabControl.SelectedItem = SoundfileTab; // Wechselt zum Tab "Soundfile"
                FadeAndStopButton.IsEnabled = true; // Aktiviert den Fade and Stop Button
                sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
                cmbAudioDevices.SelectedIndex = sf.UsedAudiodevice; // Zeigt das ausgewählte Audiogerät der Sounddatei an
                TrackVolume.Value = sf.TrackVolume * 100; // Setzt die Lautstärke der Sounddatei auf den Track Volume Slider
                TrackStart.Maximum = (int)sf.Duration; // Setzt das Maximum des Startzeitpunkt-Sliders auf die Dauer der Datei
                TrackStart.Value = 0; // Setzt den Startzeitpunkt auf 0
                TrackEnd.Maximum = (int)sf.Duration; // Setzt das Maximum des Endzeitpunkt-Sliders auf die Dauer der Datei
                TrackEnd.Value = sf.Duration; // Setzt den Endzeitpunkt auf Ende der Datei
                
            }
            if ( SoundListBox.SelectedItem is Command cmd)
            {
                var soundfiles = Cues.OfType<Soundfile>().ToList();
                cmbTargetCue.ItemsSource = soundfiles; // Füllt die ComboBox mit den verfügbaren Sounddateien
                cmbTargetCue.SelectedIndex = soundfiles.IndexOf(cmd.TargetCue as Soundfile); // Setzt den Wert auf die aktuelle TargetCue des Commands
                cmbCommand.ItemsSource = Command.CommandList; // Füllt die ComboBox mit den verfügbaren Commands
                cmbCommand.SelectedIndex = Command.CommandList.IndexOf(cmd.CommandType); // Setzt den Wert auf den aktuellen Command-Typ des Commands
                SoundfileTab.IsEnabled = true; // Aktiviert den Soundfile Tab
                SideTabControl.SelectedItem = CommandTab; // Wechselt zum Tab "Command"
                FadeAndStopButton.IsEnabled = false; // Deaktiviert den Fade and Stop Button, da Commands keine Lautstärke haben
                
            }
            SoundListBox_RefreshListContent(); // Aktualisiert die ListBox, um neue Cues anzuzeigen
        }


        private void ComboboxCommand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundListBox.SelectedItem == null || !(SoundListBox.SelectedItem is Command cmd))
            {
                return; // Keine Command ausgewählt in der Liste
            }
            var comboBox = sender as ComboBox;
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                cmd.CommandType = comboBox.SelectedItem.ToString(); // Setzt den Command-Typ des Commands
            }
            SoundListBox_RefreshListContent(); // Aktualisiert die ListBox, um neue Cues anzuzeigen
        }


        private void FadeDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SoundListBox.SelectedItem == null || !(SoundListBox.SelectedItem is Command cmd))
            {
                return; // Keine Command ausgewählt in der Liste
            }
            cmd.Duration = (float)FadeDurationSlider.Value; // Setzt die Fade-Dauer des Commands
            FadeDurationLabel.Content = $"{(int)FadeDurationSlider.Value} Sek"; // Aktualisiert die Anzeige der Fade-Dauer
        }

        /// <summary>
        /// Event-Handler für die Änderung der Track-Lautstärke
        /// </summary>
        /// <param name="sender"></param>
        /// <param="e"></param>
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
        /// <param="e"></param>
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
        /// <param="e"></param>
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
        /// <param="e"></param>
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
        /// <param="e"></param>
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
                        StartTrack(currentIndex + 1); // Startet den nächsten Track automatisch
                    }
                }
            });
        }

        /// <summary>
        /// Event-Handler für die Änderung des Startzeitpunkts des Tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param="e"></param>
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
        /// <param="e"></param>
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

        /// <summary>
        /// Event-Handler für den Klick auf den "Fade and Stop" Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param="e"></param>
        private void FadeAndStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (SoundListBox.SelectedItem == null)
            {
                return; // Keine Sounddatei ausgewählt in der Liste
            }
            Soundfile sf = SoundListBox.SelectedItem as Soundfile; // Ausgewählte Sounddatei
            sf.FadeOutAndStop(); // Fadet den Sound aus und stoppt ihn
        }

        
        private void ComboboxTargetCue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           if (SoundListBox.SelectedItem == null || !(SoundListBox.SelectedItem is Command cmd))
            {
                return; // Keine Command ausgewählt in der Liste
            }
            var comboBox = sender as ComboBox;
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                // Setzt das TargetCue des Commands auf die ausgewählte Sounddatei
                cmd.TargetCue = comboBox.SelectedItem as Cue;
            }
        }


        /// <summary>
        /// Aktualisiert die ListBox-Inhalte, um die Schriftfarbe der Cues zu ändern, wenn kein TargetCue gesetzt ist und führt danach einen Refresh der Listbox aus.
        /// </summary>
        private void SoundListBox_RefreshListContent()
        {
            //foreach (var cue in Cues)
            //{
            //    if (cue is Command cmd)
            //    {
            //        if (cmd.GetTargetCue() == null)
            //        {
            //            ListBoxItem LstItem = SoundListBox.Items[Cues.IndexOf(cmd)] as ListBoxItem;
            //            LstItem.Foreground = Brushes.Red; // Setzt die Schriftfarbe auf Rot, wenn kein TargetCue gesetzt ist
            //        } else
            //        {
            //            ListBoxItem LstItem = SoundListBox.Items[Cues.IndexOf(cmd)] as ListBoxItem;
            //            LstItem.Foreground = Brushes.Black; // Setzt die Schriftfarbe auf Schwarz, wenn ein TargetCue gesetzt ist   
            //        }
            //    }
            //}

            SoundListBox.Items.Refresh(); // Aktualisiert die ListBox, um Änderungen an den Cues anzuzeigen
        }

        // DragStart-Position merken
        private void SoundListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        // DragStart erkennen (optional: nur wenn Item angeklickt)
        private void SoundListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (SoundListBox.SelectedItem != null)
                    {
                        var data = new DataObject(typeof(Cue), SoundListBox.SelectedItem);
                        DragDrop.DoDragDrop(SoundListBox, data, DragDropEffects.Move);
                    }
                }
            }
        }

        // DragEnter: Dateien oder interne Cues erlauben
        private void SoundListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Cue)) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        // DragOver: Visuelles Feedback für Move
        private void SoundListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Cue)) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        // Drop: Dateien als neue Soundfiles hinzufügen ODER Cues verschieben
        private void SoundListBox_Drop(object sender, DragEventArgs e)
        {
            var listBox = sender as ListBox;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    string ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".wav" || ext == ".mp3" || ext == ".mp4")
                    {
                        // Immer hinzufügen, auch wenn die Datei schon in der Liste ist
                        var sf = new Soundfile(file);
                        sf.PlaybackFinished += Soundfile_PlaybackFinished;
                        Cues.Add(sf);
                    }
                }
                SoundListBox_RefreshListContent(); // Aktualisiert die ListBox, um neue Cues anzuzeigen
                SoundListBox.SelectedIndex = SoundListBox.Items.Count - 1;
            }
            else if (e.Data.GetDataPresent(typeof(Cue)))
            {
                var droppedCue = e.Data.GetData(typeof(Cue)) as Cue;
                if (droppedCue == null) return;

                // Zielindex bestimmen
                Point dropPosition = e.GetPosition(listBox);
                int targetIndex = -1;
                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (item != null)
                    {
                        Rect bounds = VisualTreeHelper.GetDescendantBounds(item);
                        Point itemPos = item.TranslatePoint(new Point(0, 0), listBox);
                        bounds.Offset(itemPos.X, itemPos.Y);
                        if (bounds.Contains(dropPosition))
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }
                if (targetIndex == -1)
                    targetIndex = Cues.Count - 1;

                int oldIndex = Cues.IndexOf(droppedCue);
                if (oldIndex != -1 && oldIndex != targetIndex)
                {
                    Cues.Move(oldIndex, targetIndex);
                    SoundListBox.SelectedIndex = targetIndex;
                }
            }
        }
    }
}
