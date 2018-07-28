using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.Features.Multimedia.Models
{
    public class TrackInfo
    {
        private string _fullName;
        private string _title = "";
        private string _artist;

        public TrackInfo()
        {
        }

        public TrackInfo(string fileName)
        {
            FileName = fileName;
            FullName = FileName.Substring(VS1003Player.FileNameOffset, FileName.Length - VS1003Player.FileNameOffset - 4);
            var fileInfo = FullName.Split('-');
            if (fileInfo.Length == 2)
            {
                Artist = fileInfo[0].Trim();
                Title = fileInfo[1].Trim();
            }
        }

        public string FileName { get; set; } = "";

        public string FullName { get; set; } = "";

        public string Title { get; set; } = "";

        public string Artist { get; set; } = "";

        public string Album { get; set; }

        public string Genre { get; set; }

        public int Time { get; set; }
    }
}
