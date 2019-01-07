using System;
using Microsoft.SPOT;

namespace imBMW.Features.Multimedia.Models
{
    internal class DiskAndTrack
    {
        public DiskAndTrack()
        {
        }

        public DiskAndTrack(byte diskNumber, byte trackNumber, string fileName = "")
        {
            this.diskNumber = diskNumber;
            this.trackNumber = trackNumber;
            this.fileName = fileName;
        }
        public byte diskNumber;
        public byte trackNumber;
        public string fileName;

        public override string ToString()
        {
            return diskNumber.ToString() + ',' + trackNumber.ToString();
        }
    }
}
