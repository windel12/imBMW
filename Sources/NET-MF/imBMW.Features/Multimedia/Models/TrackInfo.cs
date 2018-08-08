using System;
using System.IO;
using Microsoft.SPOT;
using imBMW.Tools;
using Microsoft.SPOT.IO;

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

        public TrackInfo(byte diskNumber, byte trackNumber)
        {
            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                var folder = rootDirectory + "\\" + diskNumber;
                Logger.FreeMemory();
                var files = Directory.EnumerateFiles(folder);
                Logger.FreeMemory();

                bool inited = false;
                int index = 1;
                foreach (var fileObj in files)
                {
                    string file = fileObj.ToString();
                    if (file.EndsWith(".mp3") /* || file.EndsWith(".m4a")*/)
                    {
                        if (index == trackNumber)
                        {
                            Init(file);
                            inited = true;
                            break;
                        }
                        index++;
                    }
                }
                if (!inited)
                {
                    Logger.Warning("disk " + diskNumber + " have less files then" + trackNumber);
                    var filesEnumerator = Directory.EnumerateFiles(folder).GetEnumerator();
                    filesEnumerator.MoveNext();
                    Init(filesEnumerator.Current.ToString());
                }
            }
            else
            {
                Logger.Warning("Storage is not formatted. " + "Format on PC with FAT32/FAT16 first!");
            }
        }

        public void Init(string fileName)
        {
            FileName = fileName;
            FullName = FileName.Substring(VS1003Player.FileNameOffset, FileName.Length - VS1003Player.FileNameOffset - 4);
            var fileInfo = FullName.Split('-');
            if (fileInfo.Length >= 2)
            {
                Artist = fileInfo[fileInfo.Length -2].Trim();
                Title = fileInfo[fileInfo.Length - 1].Trim();
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
