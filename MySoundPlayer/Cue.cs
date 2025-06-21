using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EazsyPlayer
{
    public abstract class Cue
    {
        string CueName { get; set; }
        object TargetCue { get; set; }
        private bool _isAutoPlayNext;


        public event PropertyChangedEventHandler PropertyChanged;

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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public string toString()
        {
            return $"{CueName}";
        }

        public abstract void Play();
        public abstract void Stop();

        public object GetTargetCue()
        {
            return TargetCue;
        }

    }
}
