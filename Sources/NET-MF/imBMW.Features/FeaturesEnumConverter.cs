using System;
using imBMW.Features.Menu.Screens;
using imBMW.iBus.Devices.Real;
using Microsoft.SPOT;
using imBMW.Multimedia;

namespace imBMW.Features
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

        public static string ToStringValue(this GHI.Processor.Watchdog.ResetCause e)
        {
            switch (e)
            {
                case GHI.Processor.Watchdog.ResetCause.Normal: return "Normal";
                case GHI.Processor.Watchdog.ResetCause.Watchdog: return "Watchdog";
            }
            return "NotSpecified(" + e.ToString() + ")";
        }
    }
}
