using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DynamicWallpaperRetriever
{
    class Program
    {

        static WallpaperEngine wallpaperEngine = new WallpaperEngine();

        public static string WorkingDir = Directory.GetCurrentDirectory();

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
            // Apply localization based on current system culture
            Localization.ApplyLocalization(Localization.GetSystemCulture().Name);

            if (args.Length == 0)
            {

            }
            else if (args.Length > 0)
            {
                Parser.Default.ParseArguments<CommandLineOptions.GetWallpaperOptions,
                    CommandLineOptions.SetWallpaperOptions,
                    CommandLineOptions.SlideshowOptions>(args)
                    .WithParsed<CommandLineOptions.GetWallpaperOptions>(options => GetWallpaper(options))
                    .WithParsed<CommandLineOptions.SetWallpaperOptions>(options => SetWallpaper(options))
                    .WithParsed<CommandLineOptions.SlideshowOptions>(options => ManipulateSlideshows(options))
                    .WithNotParsed(errors => FaultyCommand(errors));

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

                //string wallpaperPath = Path.Combine(Environment.CurrentDirectory, "hima.jpg");
            }

            
        }

        private static void GetWallpaper(CommandLineOptions.GetWallpaperOptions options)
        {

        }

        private static void SetWallpaper(CommandLineOptions.SetWallpaperOptions options)
        {
            // URL conversion

            if (options.Url == null || options.Url.Trim() == "")
            {
                FaultyCommand(Properties.strings.ErrorDownloadPrefix + Properties.strings.ErrorDownloadUrlNull);
            }
            UriBuilder uriBuilder = new UriBuilder(options.Url);

            // Obtain the file from the URL

            FileInfo wallpaperFile;
            if (options.IsLocal)
            {
                wallpaperFile = new FileInfo(uriBuilder.Path);
            }
            else
            {
                // Perform check of savepath availability

                string cleanSavePath;
                try
                {
                    cleanSavePath = Path.GetFullPath(options.SavePath);
                }
                catch (Exception)
                {
                    FaultyCommand(Properties.strings.ErrorDownloadPrefix + Properties.strings.ErrorDownloadUrlFormat);
                    return;
                }

                if (Directory.Exists(cleanSavePath))
                {
                    FaultyCommand(Properties.strings.ErrorDownloadPrefix + Properties.strings.ErrorDownloadFolderPath);
                    return;
                }

                // Downloads the file to the specified location

                try
                {
                    DownloadResource(uriBuilder.Uri, cleanSavePath);
                }
                catch (Exception ex)
                {
                    FaultyCommand(Properties.strings.ErrorDownloadPrefix + ex.Message);
                    return;
                }

                wallpaperFile = new FileInfo(cleanSavePath);
            }

            // Apply the wallpaper

            if (options.Monitor >= 0)
            {
                string monitorID = wallpaperEngine.GetEngine().GetMonitorDevicePathAt((uint)options.Monitor);
                wallpaperEngine.GetEngine().SetWallpaper(monitorID, wallpaperFile.FullName);
            }
            else
            {
                wallpaperEngine.SetWallpaper(wallpaperFile.FullName);
            }

            if (options.Position >= 0 && options.Position <= 5)
            {
                Win32.DESKTOP_WALLPAPER_POSITION wallpaperPositioning = (Win32.DESKTOP_WALLPAPER_POSITION) options.Position;
                wallpaperEngine.GetEngine().SetPosition(wallpaperPositioning);
            }

            if (options.BackgroundColor != null)
            {
                try
                {
                    wallpaperEngine.SetBackgroundColor(options.BackgroundColor);
                }
                catch (ArgumentException)
                {
                    FaultyCommand(Properties.strings.ErrorSetBackgroundColorPrefix + Properties.strings.ErrorHtmlColorCodeFormat);
                    return;
                }
                /*catch (Exception ex)
                {
                    Console.Error.WriteLine(Properties.strings.ErrorSetBackgroundColorPrefix + ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                }*/
            }
        }

        private static void ManipulateSlideshows(CommandLineOptions.SlideshowOptions options)
        {

        }

        private static int FaultyCommand(IEnumerable<Error> errors)
        {
            SentenceBuilder errorParser = SentenceBuilder.Create();
            foreach (Error error in errors)
            {
                if (error is HelpVerbRequestedError)
                {
                    // Explicit request of help text, no need to take care of this error
                    continue;
                }
                try
                {
                    string errorMsg = errorParser.FormatError(error);
                    if (error.StopsProcessing)
                    {
                        Console.Error.WriteLine(Properties.strings.FaultyCommandErrorPrefix + errorMsg);
                    }
                    else
                    {
                        Console.Error.WriteLine(Properties.strings.FaultyCommandWarningPrefix + errorMsg);
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.Error.WriteLine(error);
                }
            }
            return 1;
        }

        private static int FaultyCommand(String message)
        {
            Console.Error.WriteLine(message);
            
            return 1;
        }

        private static Uri lastDownloadUri;

        private static void DownloadResource(Uri url, string savepath)
        {
            // Setup
            WallpaperDownloader downloader = new WallpaperDownloader();
            downloader.DownloadCompleted += DownloadClient_DownloadFileFinished;

            // Download
            lastDownloadUri = url;
            downloader.Download(url, savepath);
        }

        static void DownloadClient_DownloadFileFinished(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Download cancelled for resource at: " + lastDownloadUri);
            }
            else if (e.Error != null)
            {
                Console.WriteLine(Properties.strings.ErrorDownloadPrefix + e.Error.Message);
                Console.WriteLine(e.Error.Message);
                Console.WriteLine(e.Error.StackTrace);
            }
            else
            {
                // TODO download finished handler
                //Console.WriteLine("Successfully downloaded resource at: " + lastDownloadUri);
                wallpaperEngine.GetEngine().SetSlideshow(Environment.CurrentDirectory);
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
