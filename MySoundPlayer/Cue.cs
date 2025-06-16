using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySoundPlayer
{
    public class Cue

    {
        private string name { get; set; }
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                }
            }
        }
        public Cue() { }
        public Cue(string name)
        {
            Name = name;
        }





    }
}
