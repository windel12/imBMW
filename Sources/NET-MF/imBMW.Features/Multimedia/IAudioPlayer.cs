using System;
using Microsoft.SPOT;
using imBMW.Features.Menu;
using imBMW.Features.Multimedia.Models;

namespace imBMW.Multimedia
{
    public delegate void IsPlayingHandler(IAudioPlayer sender, bool isPlaying);

    public delegate void NowPlayingHandler(IAudioPlayer sender, TrackInfo nowPlaying);

    public interface IAudioPlayer
    {
        void Next();

        void Prev();

        void Play();

        void Pause();

        bool RandomToggle(byte diskNumber);

        void ChangeTrackTo(string fileName);

        bool IsPlaying { get; }

        byte DiskNumber { get; set; }

        byte TrackNumber { get; set; }

        bool IsRandom { get; set; }

        string Name { get; }

        TrackInfo CurrentTrack { get; set; }

        bool Inited { get; set; }

        event IsPlayingHandler IsPlayingChanged;

        event NowPlayingHandler TrackChanged;
    }
}
