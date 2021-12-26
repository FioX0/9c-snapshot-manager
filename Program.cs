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
                Console.WriteLine("SnapshotManager V2.2.2\n");

                string path2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Nine Chronicles\\config.json";
                string path3 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Nine Chronicles\\configold.json";

                string text = System.IO.File.ReadAllText(path2);

                string pathString2 = string.Empty;

                if (text.Contains("monorocksdb"))
                {

                    Console.WriteLine("Current Config is monorkcsdb which is no longer valid, downloading new config file");

                    lWebClient2.Timeout = 600 * 60 * 1000;
                    lWebClient2.DownloadFile("https://cdn.discordapp.com/attachments/674880780408848404/905555413796286474/config.json", Environment.CurrentDirectory + "\\config.json");

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
                    Console.WriteLine("Already on rocksdb\n");
                }

                if (text.Contains("BlockchainStoreDirParent"))
                {
                    Console.WriteLine("Custom Path Found");
                    var path = File.ReadLines(path2).SkipWhile(line => !line.Contains("BlockchainStoreDirParent")).TakeWhile(line => line.Contains("BlockchainStoreDirParent"));
                    Console.WriteLine(path.First().ToString());
                    var newpath = path.First().ToString();
                    newpath = newpath.Remove(0,30);
                    var length = newpath.Length;
                    newpath = newpath.Remove(length - 1, 1);
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
                        lWebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(FileDone);
                        //await lWebClient.DownloadFileTaskAsync("https://snapshots.nine-chronicles.com/main/partition/full/9c-main-snapshot.zip", "snapshot.zip");
                        await lWebClient.DownloadFileTaskAsync("https://snapshots.nine-chronicles.com/main/partition/full/9c-main-snapshot.zip", Environment.CurrentDirectory + "\\snapshot.zip");
                    }
                    catch (Exception ex) { Console.WriteLine("Unstable Connection, download failed"); Console.ReadLine(); }
                }).Start();

                Thread.Sleep(10000);
                Console.WriteLine("DOWNLOADING SNAPSHOT. DO NOT CLOSE\n");
                Console.WriteLine("This download is quite large, so it will take some time.\n");
                Console.WriteLine("Downloading Snapshot.zip file to " + Environment.CurrentDirectory + "\\snapshot.zip\n");
                Console.WriteLine("If you get any error mid-download this would indicate that the connection has dropped.\n");

                ProgressBar.WriteProgressBar(0);
                Thread.Sleep(10000);
                FileInfo info = new FileInfo("snapshot.zip");
                // Snapshot Size 20027199045
                // There's no way to know the exact complete file size, so we are using estimates.
                long percentage = 0;
                download = 1;
                while (download == 1)
                {
                    info = new FileInfo(Environment.CurrentDirectory + "\\snapshot.zip");
                    percentage = (info.Length * 100) / 20027199045;
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

                //Will continue to attempt extracting if it's still finishing last % of download.
                var state = await ExtractFiles(pathzip, pathString2);
                if (!state)
                {
                    Console.WriteLine("Extraction failed, please extract manually and report this error with a screenshot of the console.");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Extracted\n");
                    string command = Environment.GetEnvironmentVariable("LocalAppData");
                    command = command + @"""\Programs\Nine Chronicles\Nine Chronicles.exe""";
                    Process p = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = @"/C " + command; // cmd.exe spesific implementation
                    startInfo.UseShellExecute = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.StartInfo = startInfo;            
                    p.Start();

                    Console.WriteLine("SnapshotManager attempted to run Nine Chronicles Automatically\nIf it didn't open, do so manually.");
                    Console.ReadLine();
                }
            }
        }

        public static async Task<bool> ExtractFiles(string pathzip, string pathstring2)
        {
            try
            {
                await DeleteFolder(pathzip, pathstring2);
                Console.WriteLine("Extracting SNAPSHOT, DO NOT CLOSE\n");
                Console.WriteLine("Attempting to Extract:");
                pathstring2 = pathstring2.Replace("\"", "");
                ZipFile.ExtractToDirectory(pathzip, pathstring2 + "\\9c-main-partition\\",true);
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

        public static async Task<bool> DeleteFolder(string pathzip, string pathstring2)
        {
            try
            {
                pathstring2 = pathstring2.Replace("\"", "");
                pathstring2 += "\\\\9c-main-partition";
                string path = pathstring2.Replace(@"\\", @"\");
                Console.WriteLine("Check if SnapshotFolder already exists\n");
                if (Directory.Exists(path))
                {
                    Directory.Delete(pathstring2, true);
                    Console.WriteLine("Deleted old snapshot data\n");
                    Thread.Sleep(4000);
                }
                else
                    Console.WriteLine("No Previous Snapshot data found\n");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed To delete old snapshot data.\n");
                Console.WriteLine(ex);
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
