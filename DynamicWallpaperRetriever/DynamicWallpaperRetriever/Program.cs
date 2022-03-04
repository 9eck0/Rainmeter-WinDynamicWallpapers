using CommandLine;
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

        static DesktopWallpaper wallpaperEngine = new DesktopWallpaper();

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

            Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(options => Run(options), err =>
            {

            });

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

                WallpaperDownloader downloader = new WallpaperDownloader();
                downloader.DownloadCompleted += DownloadClient_DownloadFileFinished;

                if (args[0] == "Himawari")
                {
                    //downloader.DownloadAsync(new Uri(CurrentHimawariUrl()), args[1]);
                }
                else
                {
                    downloader.DownloadAsync(new Uri(args[0]), args[1]);
                }

                string wallpaperPath = Path.Combine(Environment.CurrentDirectory, "hima.jpg");
                Console.WriteLine(wallpaperPath);

                string monitorID = wallpaperEngine.GetMonitorDevicePathAt(1);
                wallpaperEngine.SetWallpaper(monitorID, wallpaperPath);
            }

            
        }

        private static void Run(CommandLineOptions runOptions)
        {

        }

        /// <summary>
        /// Obtains the slideshow preset from current Windows wallpaper API configuration.
        /// </summary>
        /// <returns>A <see cref="SlideshowPreset"/> based on currently configured Windows slideshow,
        /// or <c>null</c> if no slideshow is configured.</returns>
        public static SlideshowPreset FromWindowsSlideshow(string presetName = null)
        {
            string slideshowFolder = wallpaperEngine.GetSlideshowFolder();

            if (slideshowFolder == null)
            {
                return null;
            }

            presetName = presetName == null ? "Windows slideshow" : presetName;
            bool shuffle = wallpaperEngine.IsSlideshowShufflingEnabled();
            ShuffleType shuffleType = shuffle ? ShuffleType.Nonrepeating : ShuffleType.Ordered;
            return new SlideshowPreset(presetName, slideshowFolder, null, false, shuffleType);
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
                wallpaperEngine.SetSlideshow(Environment.CurrentDirectory);
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
}
