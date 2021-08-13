using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpLib;

namespace SnapshotManager
{
    class Program
    {
        static WebClient lWebClient = new WebClient();
        static WebClient lWebClient2 = new WebClient();
        static int download = 1;
        static async Task Main(string[] args)
        {
            Process[] pname = Process.GetProcessesByName("Nine Chronicles");
            if (pname.Length != 0)
            {
                Console.WriteLine("Nine Chronicles is running. Close Nine Chronicles Fully before running SnapshotManager");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("SnapshotManager for Nince Chronicles version 100060+\n");

                string path2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Nine Chronicles\\config.json";
                string path3 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Nine Chronicles\\configold.json";

                string text = System.IO.File.ReadAllText(path2);

                if (!text.Contains("monorocksdb"))
                {

                    Console.WriteLine("Current Config is not monorkcsdb");

                    lWebClient2.DownloadFile("https://cdn.discordapp.com/attachments/613670425729171456/867675981653475348/config.json", "config.json");

                    Console.WriteLine("Config Downloaded\n");

                    File.Move(path2, path3, true);
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

                string pathString2 = string.Empty;
             
                if (text.Contains("BlockchainStoreDirParent"))
                {
                    Console.WriteLine("Custom Path Found");
                    var path = File.ReadLines(path2).SkipWhile(line => !line.Contains("BlockchainStoreDirParent")).TakeWhile(line => line.Contains("BlockchainStoreDirParent"));
                    Console.WriteLine(path.First().ToString());
                    var newpath = path.First().ToString();
                    newpath = newpath.Remove(0,30);
                    var length = newpath.Length;
                    newpath = newpath.Remove(length - 2, 2);
                    Console.WriteLine(newpath);
                    pathString2 = newpath;
                }
                else
                {
                    pathString2 = Environment.GetEnvironmentVariable("LocalAppData") + "\\planetarium\\";
                    Console.WriteLine("Creating Folders if required\n");
                    System.IO.Directory.CreateDirectory(pathString2);
                }


                File.Delete("snapshot.zip");
                new Thread(async () =>
                {
                    try
                    {
                        System.Net.ServicePointManager.Expect100Continue = false;
                        Thread.CurrentThread.IsBackground = true;
                        lWebClient.Timeout = 600 * 60 * 1000;
                        //lWebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(FileDone);
                        //await lWebClient.DownloadFileTaskAsync("https://snapshots.nine-chronicles.com/main/mono/9c-main-snapshot.zip", "snapshot.zip");
                        await lWebClient.DownloadFileTaskAsync("https://snapshots.nine-chronicles.com/main/mono/9c-main-snapshot.zip", "snapshot.zip");
                    }
                    catch (Exception ex) { Console.WriteLine(ex); Console.Read(); }


                }).Start();

                Thread.Sleep(10000);
                Console.WriteLine("DOWNLOADING SNAPSHOT. DO NOT CLOSE\n");
                Console.WriteLine("This download is quite large, so it will take some time.\n");

                ProgressBar.WriteProgressBar(0);
                Thread.Sleep(10000);
                FileInfo info = new FileInfo("snapshot.zip");
                // Snapshot Size 14027199045
                // There's no way to know the exact complete file size, so we are using estimates.
                long percentage = 0;
                download = 1;
                while (download == 1)
                {
                    info = new FileInfo("snapshot.zip");
                    percentage = (info.Length * 100) / 14027199045;
                    if (percentage < 99)
                    {
                        ProgressBar.WriteProgressBar((int)percentage, true);
                    }
                    Thread.Sleep(1000);
                }
                ProgressBar.WriteProgressBar(100, true);
                Console.WriteLine("\n\nSnapshot Downloaded\n");
                Console.WriteLine("Preparing to extract snapshot\n");


                string pathzip = Environment.CurrentDirectory + "\\snapshot.zip";

                //Delete current folder if exists, to ensure we aren't just placing new files on-top and causing hundreds of GB's to be stored.
                try
                {
                    System.IO.Directory.Delete(pathString2 + "\\9c-main-snapshot\\", true);
                }
                catch (Exception ex) { }


                Console.WriteLine("Extracting SNAPSHOT, DO NOT CLOSE\n\n");
                //Will continue to attempt extracting if it's still finishing last % of download.
                await ExtractFiles(pathzip, pathString2);
                Console.WriteLine("Extracted\n");
                Console.WriteLine("You can now start the game\n");
                Console.ReadLine();
            }
        }

        public static async Task<bool> ExtractFiles(string pathzip, string pathstring2)
        {
            try
            {
                Console.WriteLine("Attempting to Extract:");
                ZipFile.ExtractToDirectory(pathzip, pathstring2 + "\\9c-main-snapshot\\");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed To extract.\n");
                Console.WriteLine(ex);
                Console.ReadLine();
                return false;
            }
        }

        private class WebClient : System.Net.WebClient
        {
            public int Timeout { get; set; }

            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest lWebRequest = base.GetWebRequest(uri);
                lWebRequest.Timeout = Timeout;
                ((HttpWebRequest)lWebRequest).ReadWriteTimeout = Timeout;
                return lWebRequest;
            }
        }

        private static void FileDone(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("File download cancelled.");
                Console.ReadLine();
            }

            if (e.Error != null)
            {
                Console.WriteLine(e.Error.ToString());
                Console.ReadLine();
            }

            download = 0;
        }

    }

}
