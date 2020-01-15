using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Features.Menu;
using imBMW.Features.Multimedia.Models;

namespace imBMW.Multimedia
{
    public abstract class AudioPlayerBase : IAudioPlayer
    {
        bool isEnabled;
        //TrackInfo nowPlaying;
        protected bool isPlaying;

        public byte TrackNumber { get; set; } = 1;
        public byte DiskNumber { get; set; } = 1;
        //public string FileName { get; set; } = "";
        public bool IsRandom { get; set; } = true;


        public abstract void Play();

        public abstract void Pause();

        protected void SetPlaying(bool value)
        {
            IsPlaying = value;
        }

        public abstract void Next();

        public abstract void Prev();

        public abstract bool RandomToggle(byte diskNumber);

        public abstract string ChangeTrackTo(string fileName);

        public string Name { get; protected set; }

        //public TrackInfo CurrentTrack { get; set; }

        public bool Inited { get; set; }

        public bool IsPlaying
        {
            get;
            protected set;
        }

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            private set
            {
                if (isEnabled == value)
                {
                    return;
                }
                isEnabled = value;
                OnIsEnabledChanged(value);
            }
        }

        protected virtual void OnIsEnabledChanged(bool isEnabled)
        {
        }

        //public event IsPlayingHandler IsPlayingChanging;

        public event IsPlayingHandler IsPlayingChanged;

        //public event NowPlayingHandler TrackChanged;

        //protected virtual void OnIsPlayingChanging(bool isPlaying)
        //{
        //    var e = IsPlayingChanging;
        //    if (e != null)
        //    {
        //        e.Invoke(this, isPlaying);
        //    }
        //}

        //protected virtual void OnIsPlayingChanged(bool isPlaying)
        //{
        //    var e = IsPlayingChanged;
        //    if (e != null)
        //    {
        //        e.Invoke(this, isPlaying);
        //    }
        //}

        //protected virtual void OnTrackChanged()
        //{
        //    var e = TrackChanged;
        //    if (e != null)
        //    {
        //        e.Invoke(this, nowPlaying);
        //    }
        //}
    }
}
