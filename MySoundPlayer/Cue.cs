using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySoundPlayer
{
    public abstract class Cue
    {
        string CueName { get; set; }
        object TargetCue { get; set; }

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
