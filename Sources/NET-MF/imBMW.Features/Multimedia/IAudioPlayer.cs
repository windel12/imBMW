using System;
using Microsoft.SPOT;
using imBMW.Features.Menu;
using imBMW.Features.Multimedia.Models;

namespace imBMW.Features.Multimedia
{
    public delegate void IsReadyHandler(IAudioPlayer sender, bool isReady);

    public delegate void IsPlayingHandler(IAudioPlayer sender, bool isPlaying);

    public delegate void NowPlayingHandler(IAudioPlayer sender, string trackName);

    public interface IAudioPlayer
    {
        void Next();

        void Prev();

        void Play();

        void Pause();

        bool RandomToggle(byte diskNumber);

        string ChangeTrackTo(string fileName);

        bool IsPlaying { get; }

        byte DiskNumber { get; set; }

        byte TrackNumber { get; set; }

        bool IsRandom { get; set; }

        string Name { get; }

        //TrackInfo CurrentTrack { get; set; }

        bool Inited { get; set; }

        bool IsReady { get; set; }

        event IsReadyHandler IsReadyChanged;

        event IsPlayingHandler IsPlayingChanged;

        event NowPlayingHandler TrackChanged;
    }
}
