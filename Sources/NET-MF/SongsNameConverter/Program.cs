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
        static int errorsCount = 0;
        static int exceptionCount = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Select mode:");
            Console.WriteLine("1 - Copy tag data to file name in format: 'Artist - Title' (common format)");
            Console.WriteLine("2 - Copy tag data to file name in format: 'Title - Artist' (reverted format)");
            Console.WriteLine("3 - Move file name data(in common format) to id3 tags");
            string MODE = Console.ReadLine();
            if (MODE != "1" && MODE != "2" && MODE != "3")
            {
                Console.WriteLine("You chose wrong mode!");
                return;
            }

            Console.WriteLine("Type folder name:");
            Console.WriteLine("Or press 'enter' for all child folders");
            string folderName = Console.ReadLine();
            string basePath = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
            string path = string.IsNullOrEmpty(folderName) ? basePath : Path.Combine(basePath, folderName);

            if (!string.IsNullOrEmpty(folderName) && !Directory.Exists(Path.Combine(basePath, folderName)))
            {
                Console.WriteLine("Folder don't exist!");
                return;
            }

            var mp3filesPathes = Directory.EnumerateFiles(path, "*.mp3", string.IsNullOrEmpty(folderName) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (string filePath in mp3filesPathes)
            {
                var taglibFile = TagLib.File.Create(filePath);

                var artistFromTag = taglibFile.Tag.FirstAlbumArtist;
                if (string.IsNullOrEmpty(artistFromTag?.Trim()))
                {
                    artistFromTag = taglibFile.Tag.FirstArtist;
                }
                var titleFromTag = taglibFile.Tag.Title;


                if (MODE == "1")
                {
                    if (!string.IsNullOrEmpty(artistFromTag) && !string.IsNullOrEmpty(titleFromTag))
                    {
                        RenameFile(filePath, artistFromTag, titleFromTag);
                    }
                    else
                    {
                        errorsCount++;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Error! File '{0}' doesn't contains 'artist' or 'title' tags. Skipped!", filePath);
                        Console.ResetColor();
                    }
                }

                if (MODE == "2")
                {
                    if (!string.IsNullOrEmpty(artistFromTag) && !string.IsNullOrEmpty(titleFromTag))
                    {
                        RenameFile(filePath, titleFromTag, artistFromTag);
                    }
                    else
                    {
                        errorsCount++;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Error! File '{0}' doesn't contains 'artist' or 'title' tags. Skipped!", filePath);
                        Console.ResetColor();
                    }
                }

                if (MODE == "3")
                {
                    string artistFromFileName = "";
                    string titleFromFileName = "";
                    string fileName = Path.GetFileName(filePath);
                    byte fileExtensionLength = 4;
                    fileName = fileName.Substring(0, fileName.Length - fileExtensionLength);
                    if (fileName.Count(x => x == '-') == 1)
                    {
                        var fileInfo = fileName.Split('-');
                        if (fileInfo.Length >= 2)
                        {
                            artistFromFileName = fileInfo[fileInfo.Length - 2].Trim();
                            titleFromFileName = fileInfo[fileInfo.Length - 1].Trim();

                            if (artistFromTag != artistFromFileName || titleFromTag != titleFromFileName)
                            {
                                taglibFile.Tag.Artists = new string[1] { artistFromFileName };
                                taglibFile.Tag.Title = titleFromFileName;
                                taglibFile.Save();
                                Console.WriteLine("Tags of file '{0}' was changed! Artist from '{1}' -> '{2}'. Title from '{3}' -> '{4}'", 
                                    fileName, artistFromTag, artistFromFileName, titleFromTag, titleFromFileName);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("We have file '{0}' with more than one '-' symbol. Skipped!", fileName);
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Files was changed. Errors - {0}, Exceptions - {1}. Press any key to exit.", errorsCount, exceptionCount);
            Console.ReadLine();
        }

        public static void RenameFile(string filePath, string firstFileNamePart, string secondFileNamePart)
        {

            var directory = Directory.GetParent(filePath);
            var destinationFile = string.Format("{0}\\{1} - {2}.mp3", directory.FullName, firstFileNamePart, secondFileNamePart);
            try
            {
                System.IO.File.Move(filePath, destinationFile);
            }
            catch (Exception ex)
            {
                exceptionCount++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Exception occured at renaming file '{0}' to '{1}'. Message: {2}", filePath, destinationFile, ex.Message);
                Console.ResetColor();
            }
            finally
            {
                Console.WriteLine("Success! File '{0}' was changed to '{1}'", filePath, destinationFile);
            }
        }
    }
}
