﻿
using System;
using System.Collections.ObjectModel;
using EazsyPlayer.Audio;

namespace EazsyPlayer
{
    class Command : Cue
    {
        public static ObservableCollection<string> CommandList { get; } = new ObservableCollection<string> { 
            "FadeOut",
            "FadeIn",
            "Stop",
            "Play"
        };

        public Cue TargetCue { get; set; } = null;
        public string CommandType { get; set; } = null;
        public float Duration { get; set; }

        public Command() {
        }

        public Command(string commandType, Cue targetCue, float duration)
        {
            if (CommandList.Contains(commandType) == false)
            {
                throw new ArgumentException($"Command type '{commandType}' is not valid. Valid commands are: {string.Join(", ", CommandList)}");
            }
            CommandType = commandType;
            TargetCue = targetCue;
            Duration = duration;
		}

        public override string ToString()
        {
            if (TargetCue != null && CommandType != null)
            {
            return $"{CommandType} ==> {TargetCue?.ToString()}";
            }
            return $"{CommandType} ==> No target cue set";
        }

        public override void Play()
        {
            if (TargetCue == null || CommandType == null)
			{
				Console.WriteLine("TargetCue or CommandType is not set.");
                return;
			}

			switch (CommandType)
            {
                case "FadeOut":
                    (TargetCue as Soundfile)?.FadeOutAndStop();
                    break;
                case "FadeIn":
                    break;
                case "Stop":
                    TargetCue?.Stop();
                    break;
                case "Play":
                    TargetCue?.Play();
                    break;
                default:
                    throw new InvalidOperationException("Unbekannter CommandType");
            }
        }


        public override void Stop()
        {
            // Stop() wird nicht für Commands verwendet, daher leer lassen
        }
    }
}
