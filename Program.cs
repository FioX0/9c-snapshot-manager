using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using CSharpLib;

namespace SnapshotManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SnapshotManager for Nince Chronicles version 100060+\n");

            string path2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Nine Chronicles\\config.json";
            string path3 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Nine Chronicles\\configold.json";

            string text = System.IO.File.ReadAllText(path2);

            if(!text.Contains("monorocksdb"))
            {

                Console.WriteLine("Current Config is not monorkcsdb");

                using (var client = new WebClient())
                {
                    client.DownloadFile("https://cdn.discordapp.com/attachments/613670425729171456/867675981653475348/config.json", "config.json");
                }

                Console.WriteLine("Config Downloaded\n");

                File.Move(path2, path3);
                Console.WriteLine("Backed up old config as configold.json");

                //Console.WriteLine(path2);

                //// Ensure that the target does not exist.
                if (File.Exists(path2))
                    File.Delete(path2);

                Console.WriteLine("Move Config\n");
                string path = Environment.CurrentDirectory + "\\config.json";
                try
                {
                    if (!File.Exists(path))
                    {
                        // This statement ensures that the file is created,
                        // but the handle is not kept.
                        using (FileStream fs = File.Create(path)) { }
                    }

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
                    File.Move(path3, path2);
                }
            }
            else
            {
                Console.WriteLine("Already on monorockdb\n");
            }

            string pathString2 = Environment.GetEnvironmentVariable("LocalAppData") + "\\planetarium\\";
            Console.WriteLine("Creating Folders if required\n");
            System.IO.Directory.CreateDirectory(pathString2);
            File.Delete("snapshot.zip");
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                using (var client = new WebClient())
                {
                    client.DownloadFile("https://snapshots.nine-chronicles.com/main/e7922c/mono/9c-main-snapshot.zip", "snapshot.zip");
                }
            }).Start();
            Thread.Sleep(5000);
            Console.WriteLine("DOWNLOADING SNAPSHOT. DO NOT CLOSE\n");
            Console.WriteLine("This download is around 3Gb, so it will take some time.\n");

            ProgressBar.WriteProgressBar(0);
            FileInfo info = new FileInfo("snapshot.zip");
            // Snapshot Size 3427199045
            // There's no way to know the exact complete file size, so we are using estimates.
            long percentage = 0;
            while (percentage < 99)
            {
                info = new FileInfo("snapshot.zip");
                percentage = (info.Length * 100) / 3426144200;
                ProgressBar.WriteProgressBar((int)percentage, true);
                Thread.Sleep(1000);
            }
            ProgressBar.WriteProgressBar(100, true);
            Console.WriteLine("\nSnapshot Downloaded\n");
            Console.WriteLine("Preparing to extract snapshot\n");
            Thread.Sleep(20000); //Let's give it some leeway, in case it's still downloading the last %.

            
            string pathzip = Environment.CurrentDirectory + "\\snapshot.zip";

            //Delete current folder if exists, to ensure we aren't just placing new files on-top and causing hundreds of GB's to be stored.
            try
            {
                System.IO.Directory.Delete(pathString2 + "\\9c-main-snapshot\\", true);
            }
            catch(Exception ex) { }


            Console.WriteLine("Extracting SNAPSHOT, DO NOT CLOSE\n\n\n\n");
            //Will continue to attempt extracting if it's still finishing last % of download.
            while(!extractFiles(pathzip, pathString2));
            Console.WriteLine("Extracted\n");
            Console.WriteLine("You can now start the game\n");
            Console.ReadLine();
        }

        public static bool extractFiles(string pathzip, string pathstring2)
        {
            try
            {
                ZipFile.ExtractToDirectory(pathzip, pathstring2 + "\\9c-main-snapshot\\");
                return true;
            }
            catch (Exception ex)
            {
                Thread.Sleep(10000);
                return false;
            }
        }
    }

}
