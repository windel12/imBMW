using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Features.Menu;
using imBMW.Features.Multimedia.Models;

namespace imBMW.Features.Multimedia
{
    public abstract class AudioPlayerBase : IAudioPlayer
    {
        private bool isPlaying;
        private bool isReady;

        //TrackInfo nowPlaying;

        public byte TrackNumber { get; set; } = 1;
        public byte DiskNumber { get; set; } = 1;
        //public string FileName { get; set; } = "";
        public bool IsRandom { get; set; } = true;

        public abstract void Play();

        public abstract void Pause();

        public abstract void Next();

        public abstract void Prev();

        public abstract bool RandomToggle(byte diskNumber);

        public abstract void AddPlaylistToQueue(byte number);

        public abstract void ClearQueue();

        public string Name { get; protected set; }

        //public TrackInfo CurrentTrack { get; set; }

        public bool Inited { get; set; }

        public bool IsReady
        {
            get { return isReady; }
            set
            {
                if (isReady == value)
                {
                    return;
                }
                isReady = value;
                OnIsReadyChanged(value);
            }
        }

        public bool IsPlaying
        {
            get { return isPlaying; }
            protected set
            {
                if (isPlaying == value)
                {
                    return;
                }
                isPlaying = value;
                OnIsPlayingChanged(value);
            }
        }

        public event IsReadyHandler IsReadyChanged;

        public event IsPlayingHandler IsPlayingChanged;

        public event NowPlayingHandler TrackChanged;

        protected virtual void OnIsReadyChanged(bool isReady)
        {
            var e = IsReadyChanged;
            if (e != null)
            {
                e(this, isReady);
            }
        }

        protected virtual void OnIsPlayingChanged(bool isPlaying)
        {
            var e = IsPlayingChanged;
            if (e != null)
            {
                e(this, isPlaying);
            }
        }

        protected virtual void OnTrackChanged(string trackName)
        {
            var e = TrackChanged;
            if (e != null)
            {
                e(this, trackName);
            }
        }
    }
}
