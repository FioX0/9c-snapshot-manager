using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SnapshotManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SnapshotManager for Nince Chronicles version 100060\n\n");
        
            using (var client = new WebClient())
            {
                client.DownloadFile("https://cdn.discordapp.com/attachments/613670425729171456/867675981653475348/config.json", "config.json");
            }

            Console.WriteLine("Config Downloaded\n");
            string path2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+ "\\Nine Chronicles\\config.json";
            Console.WriteLine(path2);


            Console.WriteLine("Move Config\n");
            string path = Environment.CurrentDirectory+"\\config.json";
            try
            {
                if (!File.Exists(path))
                {
                    // This statement ensures that the file is created,
                    // but the handle is not kept.
                    using (FileStream fs = File.Create(path)) { }
                }

                // Ensure that the target does not exist.
                if (File.Exists(path2))
                    File.Delete(path2);

                // Move the file.
                File.Move(path, path2);
                Console.WriteLine("{0} was moved to {1}.\n\n", path, path2);

                // See if the original exists now.
                if (File.Exists(path))
                {
                    Console.WriteLine("Doesn't look like I managed to move the config file.\n");
                }
                else
                {
                    Console.WriteLine("Succesfuly moved the Config File.\n");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}\n\n\n\n", e.ToString());
            }


            string pathString2 =  Environment.GetEnvironmentVariable("LocalAppData") + "\\planetarium\\";
            Console.WriteLine("Creating Folders if required\n");
            System.IO.Directory.CreateDirectory(pathString2);




            Console.WriteLine("DOWNLOADING SNAPSHOT. DO NOT CLOSE\n");
            Console.WriteLine("This download is around 3Gb, so it will take some time.\n");
            using (var client = new WebClient())
            {
                client.DownloadFile("https://snapshots.nine-chronicles.com/main/e7922c/mono/9c-main-snapshot.zip", "snapshot.zip");
            }


            Console.WriteLine("Snapshot Downloaded");
            string pathzip = Environment.CurrentDirectory + "\\snapshot.zip";

            //Delete current folder if exists, to ensure we aren't just placing new files on-top and causing hundreds of GB's to be stored.
            System.IO.Directory.Delete(pathString2 + "\\9c-main-snapshot\\", true);


            Console.WriteLine("Extracting SNAPSHOT, DO NOT CLOSE\n\n\n\n");
            ZipFile.ExtractToDirectory(pathzip, pathString2 + "\\9c-main-snapshot\\");
            Console.WriteLine("Extracted\n");
            Console.WriteLine("You can now start the game\n\n\n\n");
        }
    }
    
}
