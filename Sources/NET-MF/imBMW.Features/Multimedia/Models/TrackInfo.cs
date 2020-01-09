using System;
using System.IO;
using Microsoft.SPOT;
using imBMW.Tools;
//using Microsoft.SPOT.IO;

namespace imBMW.Features.Multimedia.Models
{
    public struct TrackInfo
    {
        static byte FileExtensionLength = 4;

        public TrackInfo(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(FilePath);
            FileName = FileName.Substring(0, FileName.Length - FileExtensionLength);
            var fileInfo = FileName.Split('-');
            if (fileInfo.Length >= 2)
            {
                Title = fileInfo[0].Trim();
                Artist = fileInfo[1].Trim();
            }
            else
            {
                Artist = "";
                Title = FileName;
            }
        }

        public string FilePath;

        public string FileName;

        public string Title;

        public string Artist;

        //public string Album { get; set; }

        //public string Genre { get; set; }

        //public int Time { get; set; }
    }
}
