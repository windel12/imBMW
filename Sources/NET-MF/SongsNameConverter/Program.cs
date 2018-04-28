using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace SongsNameConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please, define path where you songs stored. Press any key to exit.");
                Console.ReadLine();
                return;
            }
            var path = args[0];
            int errorsCount = 0;
            int exceptionCount = 0;
            var mp3files = Directory.EnumerateFiles(path, "*.mp3", SearchOption.AllDirectories);
            foreach (string file in mp3files)
            {
                var taglibFile = TagLib.File.Create(file);
                var artist = taglibFile.Tag.FirstAlbumArtist;
                if (string.IsNullOrEmpty(artist?.Trim()))
                {
                    artist = taglibFile.Tag.FirstArtist;
                }
                var title = taglibFile.Tag.Title;
                if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(title))
                {
                    var directory = Directory.GetParent(file);
                    var destinationFile = string.Format("{0}\\{1} - {2}.mp3", directory.FullName, artist, title);
                    try
                    {
                        System.IO.File.Move(file, destinationFile);
                    }
                    catch (Exception ex)
                    {
                        exceptionCount++;
                        Console.WriteLine("Exception occured at renaming file '{0}' to '{1}'. Message: {2}", file, destinationFile, ex.Message);
                    }
                    finally
                    {
                        Console.WriteLine("Success! File '{0}' was changed to '{1}'", file, destinationFile);
                    }
                }
                else
                {
                    errorsCount++;
                    Console.WriteLine("Error! File '{0}' doesn't contains 'artist' or 'title' tags. Skipped!", file);
                }
            }

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Files was changed. Errors - {0}, Exceptions - {1}. Press any key to exit.", errorsCount, exceptionCount);
            Console.ReadLine();
        }
    }
}
