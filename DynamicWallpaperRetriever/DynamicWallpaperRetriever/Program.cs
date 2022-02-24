using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DynamicWallpaperRetriever
{
    class Program
    {
        private static string WorkingDir;
        //private static Uri IMERG;
        // This Uri links to the website hosting a dynamic loop of images to be retrieved, not the images themselves.
        // Regex or another search algorithm is needed in order to retrieve files from the website's HTML source.
        //private static Uri GoesEastSite;

        /*[DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;*/

        static void Main(string[] args)
        {
            //var handle = GetConsoleWindow();

            // Hide console when run
            //ShowWindow(handle, SW_HIDE);

            TestIDesktopWallpaper();

            if (args.Length > 1)
            {
                WorkingDir = Directory.GetCurrentDirectory();

                // GOES East
                //GoesEastSite = new Uri("https://www.star.nesdis.noaa.gov/GOES/FullDisk_band.php?sat=G16&band=GEOCOLOR&length=96");
                //Console.WriteLine("Downloading GOES East image collection...");
                //DownloadGoesEast();

                // IMERG
                //IMERG = new Uri("https://trmm.gsfc.nasa.gov/trmm_rain/Events/ATLA/latest_big_half_hourly_gridded.jpg");
                /*new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Console.WriteLine("Downloading IMERG image...");
                    Download(IMERG, WorkingDir + "\\IMERG.jpg", 300 * 1000);
                }).Start();*/
                // Async requests do not work with WebClient Timeout property.
                //DownloadAsync(IMERG, WorkingDir + "\\IMERG.jpg", 30 * 1000);

                //Console.ReadLine();

                // Himawari does not have an uniform URL for its latest imagery. Thus, we need to hard-code its URL constructor.
                if (args[0] == "Himawari")
                {
                    DownloadAsync(new Uri(CurrentHimawariUrl()), args[1]);
                }
                else
                {
                    DownloadAsync(new Uri(args[0]), args[1]);
                }
            }

            
        }

        private static void TestIDesktopWallpaper()
        {
            DesktopWallpaper wallpaperEngine = new DesktopWallpaper();
            uint monitorCount = wallpaperEngine.GetMonitorDevicePathCount();

            for (uint i = 0; i < monitorCount; i++)
            {
                string monitorID = wallpaperEngine.GetMonitorDevicePathAt(i).ToString();
                Console.WriteLine("GetMonitorDevicePathAt ("+i+"): " + monitorID.ToString());
            }

            //Console.WriteLine(wallpaperEngine.GetWallpaper(null));
            //wallpaperEngine.AdvanceSlideshow();
            //wallpaperEngine.SetSlideshow(@"C:\Users\beaul\Pictures\Wallpapers\Space");
            //wallpaperEngine.SetSlideshow("testfail");
            //wallpaperEngine.EnableWallpaper();
        }

        const string userAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";

        /// <summary>
        /// Downloads a file from the internet by first downloading it to a temporary file, then renaming it to the desired file name.
        /// </summary>
        /// <param name="Path">The URL path to the file.</param>
        /// <param name="FileName">The complete path for the desired file</param>
        /// <param name="Timeout">Optional: Specify a custom timeout, in milliseconds, for high-ping resource access</param>
        static void DownloadAsync(Uri Path, string fileName, int timeout = 30*1000)
        {
            string tempName = fileName + ".temp";
            if (File.Exists(tempName))
            {
                File.Delete(tempName);
            }

            try
            {
                using (MyWebClient client = new MyWebClient())
                {
                    client.DefaultTimeout = timeout;
                    //For some resources, a 403: Forbidden error can be thrown if no user agent is provided.
                    client.Headers.Add("User-Agent: Other");
                    client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadClient_DownloadFileFinished);
                    debugWebFilePath = Path.ToString();

                    // Uncomment this section to download in the main thread
                    /*client.DownloadFile(Path, tempName);*/

                    // Uncomment this section to download asynchronously
                    /*client.DownloadFileAsync(Path, tempName);*/

                    // Uncomment this section to download asynchronously using a task object
                    var asyncDownload = client.DownloadFileTaskAsync(Path, tempName);
                    asyncDownload.Wait(timeout + 5000);
                    // Waiting
                    while (asyncDownload.Status == System.Threading.Tasks.TaskStatus.Running)
                    {
                        // Waiting for completion
                    }
                }

                // Rename temp file to desired file name
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                File.Move(tempName, fileName);
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot download resource at: " + Path.ToString());

                if (File.Exists(tempName))
                {
                    File.Delete(tempName);
                }
            }
        }

        private static string debugWebFilePath;

        static void DownloadClient_DownloadFileFinished(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Download cancelled for resource at: " + debugWebFilePath);
            }
            else if (e.Error != null)
            {
                Console.WriteLine("Download failed for resource at: " + debugWebFilePath);
                Console.WriteLine(e.Error.Message);
                Console.WriteLine(e.Error.StackTrace);
            }
            else
            {
                Console.WriteLine("Successfully downloaded resource at: " + debugWebFilePath);
            }
        }

        /*#region GoesEast

        private static void DownloadGoesEast()
        {
            try
            {
                MyWebClient client = new MyWebClient();
                // siteSrc needs to be parsed in order to retrieve image files.
                string siteSrc = client.DownloadString(GoesEastSite);
                Console.WriteLine("Successfully retrieved GoesEast site source");

                string[] imageUrl = ParseGoesEast(siteSrc);
                for (int i = 0; i < imageUrl.Length; i++)
                {
                    DownloadAsync(new Uri(imageUrl[i]), WorkingDir + "\\GoesEast" /*+ i*/ /*+ ".jpg");
                }
            }
            catch (WebException)
            {
                Console.WriteLine("Cannot access GOES East resource at this moment.");
            }
        }

        private static string[] ParseGoesEast(string htmlSrc)
        {
            string[] imageUri = new string[1];
            imageUri[0] = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/FD/GEOCOLOR/1808x1808.jpg";
            return imageUri;
        }

        #endregion GoesEast*/

        static string CurrentHimawariUrl()
        {//http://www.jma.go.jp/en/gms/imgs_c/6/visible/1/201810062150-00.png
            string baseUrl = "http://www.jma.go.jp/en/gms/imgs_c/6/visible/1/{0}{1}{2}{3}{4}-00.png";
            string[] UtcTimeFormat = new string[5]
            {
                DateTimeOffset.UtcNow.Year.ToString(),
                DateTimeOffset.UtcNow.Month.ToString().Length == 2 ? 
                    DateTimeOffset.UtcNow.Month.ToString() : 
                    DateTimeOffset.UtcNow.Month.ToString("D2"),
                DateTimeOffset.UtcNow.Day.ToString().Length == 2 ?
                    DateTimeOffset.UtcNow.Day.ToString() :
                    DateTimeOffset.UtcNow.Day.ToString("D2"),
                DateTimeOffset.UtcNow.Hour.ToString().Length == 2 ?
                    DateTimeOffset.UtcNow.Hour.ToString() :
                    DateTimeOffset.UtcNow.Hour.ToString("D2"),
                // There is a 5 minutes delay for Himawari visible band images to be online.
                ((DateTimeOffset.UtcNow.Minute-5) - (DateTimeOffset.UtcNow.Minute-5) % 10).ToString().Length == 2 ?
                    ((DateTimeOffset.UtcNow.Minute-5) - (DateTimeOffset.UtcNow.Minute-5) % 10).ToString() :
                    "00"
            };
            return String.Format(baseUrl, UtcTimeFormat[0], UtcTimeFormat[1], UtcTimeFormat[2], UtcTimeFormat[3], UtcTimeFormat[4]);
        }
    }

    class MyWebClient : WebClient
    {
        /// <summary>
        /// A deafult timeout of 30 seconds to allow for background tasks handshaking on a high latency connection.
        /// </summary>
        public int DefaultTimeout = 30 * 1000;

        protected override WebRequest GetWebRequest(Uri uri)
        {
            return GetWebRequest(uri, DefaultTimeout);
        }

        protected WebRequest GetWebRequest(Uri uri, int timeout)
        {
            if (timeout <= 0)
            {
                throw new ArgumentOutOfRangeException("timeout", "WebRequest Timeout cannot be zero or negative");
            }

            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = timeout;
            return w;
        }
    }
}
