using System;
using imBMW.Features.Menu.Screens;
using imBMW.iBus.Devices.Real;
using Microsoft.SPOT;
using imBMW.Multimedia;

namespace imBMW
{
    public static class FeaturesEnumConverter
    {
        /**
        * :) Sorry, it's .NET MF,
        * so there is no pretty way to print enums
        */

        //public static string ToStringValue(this iPodViaHeadset.iPodCommand e)
        //{
        //    switch (e)
        //    {
        //        case iPodViaHeadset.iPodCommand.Next: return "Next";
        //        case iPodViaHeadset.iPodCommand.Prev: return "Prev";
        //        case iPodViaHeadset.iPodCommand.Play: return "Play";
        //        case iPodViaHeadset.iPodCommand.Pause: return "Pause";
        //        case iPodViaHeadset.iPodCommand.PlayPauseToggle: return "PlayPauseToggle";
        //        case iPodViaHeadset.iPodCommand.VoiceOverCurrent: return "VoiceOverCurrent";
        //        case iPodViaHeadset.iPodCommand.VoiceOverMenu: return "VoiceOverMenu";
        //        case iPodViaHeadset.iPodCommand.VolumeUp: return "VolumeUp";
        //        case iPodViaHeadset.iPodCommand.VolumeDown: return "VolumeDown";
        //    }
        //    return "NotSpecified(" + e.ToString() + ")";
        //}

        public static string ToStringValue(this AuxilaryHeaterStatus e)
        {
            switch (e)
            {
                case AuxilaryHeaterStatus.Unknown: return "Unknown";
                case AuxilaryHeaterStatus.Present: return "Present";
                case AuxilaryHeaterStatus.Stopped: return "Stopped";
                case AuxilaryHeaterStatus.StopPending: return "StopPending";
                case AuxilaryHeaterStatus.StartPending: return "StartPending";
                case AuxilaryHeaterStatus.Starting: return "Starting";
                case AuxilaryHeaterStatus.Started: return "Started";
            }
            return "NotSpecified(" + e.ToString() + ")";
        }

        public static string ToStringValue(this AudioSource e)
        {
            switch (e)
            {
                case AudioSource.SDCard: return "SDCard";
                case AudioSource.Bluetooth: return "Bluetooth";
            }
            return "NotSpecified(" + e.ToString() + ")";
        }
    }
}
