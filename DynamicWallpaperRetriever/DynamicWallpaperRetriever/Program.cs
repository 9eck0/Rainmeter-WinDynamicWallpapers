using CommandLine;
using CommandLine.Text;
using IDesktopWallpaperWrapper;
using IDesktopWallpaperWrapper.Win32;
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

            var parseResults = Parser.Default.ParseArguments<CommandLineOptions.GetWallpaperOptions,
                    CommandLineOptions.SetWallpaperOptions,
                    CommandLineOptions.SlideshowOptions>(args);
            
            if (args.Length == 0)
            {
                // No argument given. Show help

                var helpText = HelpText.AutoBuild(parseResults);
                helpText.AddDashesToOption = true;
                helpText.AddNewLineBetweenHelpSections = true;
                helpText.Copyright = new CopyrightInfo("9eck0", DateTime.Now.Year);

                helpText.AddPreOptionsLine(Environment.NewLine);

                Console.WriteLine(helpText);
            }
            else if (args.Length > 0)
            {
                // Execute command

                parseResults
                    .WithParsed<CommandLineOptions.GetWallpaperOptions>(options => GetWallpaper(options))
                    .WithParsed<CommandLineOptions.SetWallpaperOptions>(options => SetWallpaper(options))
                    .WithParsed<CommandLineOptions.SlideshowOptions>(options => ManipulateSlideshows(options))
                    .WithNotParsed(errors => FaultyCommand(errors));
            }

            
        }

        /// <summary>
        /// Functionalities:
        /// <list type="bullet">
        /// <item>Gets the file location of the wallpaper on a single monitor.</item>
        /// <item>Gets the monitor ID by index.</item>
        /// <item>Gets a list of active monitor IDs.</item>
        /// <item>Outputs to specified stream (console or file).</item>
        /// <item>Get whether the internet connection is metered or not.</item>
        /// </list>
        /// </summary>
        /// <param name="options">Set of command line arguments to be parsed.</param>
        private static void GetWallpaper(CommandLineOptions.GetWallpaperOptions options)
        {
            
        }

        /// <summary>
        /// Functionalities:
        /// <list type="bullet">
        /// <item>Set a single wallpaper.</item>
        /// <item>Specify a single monitor to set the wallpaper onto, using either the index or monitor ID.</item>
        /// <item>Sets the display option of wallpapers.</item>
        /// <item>Sets the background color.</item>
        /// </list>
        /// </summary>
        /// <param name="options">Set of command line arguments to be parsed.</param>
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
                    // Download fails: exits the wallpaper setting operation
                    FaultyCommand(Properties.strings.ErrorDownloadPrefix + ex.Message);
                    return;
                }

                wallpaperFile = new FileInfo(cleanSavePath);
            }

            // Apply the wallpaper
            options.Monitor = options.Monitor.Trim();
            if (!options.Monitor.Equals(""))
            {
                if (int.TryParse(options.Monitor, out int monitorIndex))
                {
                    // Specified monitor index
                    wallpaperEngine.SetWallpaper(wallpaperFile.FullName, monitorIndex);
                }
                else if (Array.Exists(
                        wallpaperEngine.GetEngine().GetActiveMonitorIDs(),
                        x => x.Equals(options.Monitor)
                        ))
                {
                    // Specified monitor ID
                    wallpaperEngine.GetEngine().SetWallpaper(options.Monitor, wallpaperFile.FullName);
                }
                else
                {
                    FaultyCommand(Properties.strings.FaultyCommandErrorPrefix + Properties.strings.ErrorSetMonitorMalformed);
                }
            }
            else
            {
                // Apply to all monitors
                wallpaperEngine.SetWallpaper(wallpaperFile.FullName);
            }
            Console.WriteLine("Wallpaper applied");

            // Apply wallpaper positioning

            if (options.Position >= 0 && options.Position <= 5)
            {
                DESKTOP_WALLPAPER_POSITION wallpaperPositioning = (DESKTOP_WALLPAPER_POSITION) options.Position;
                wallpaperEngine.GetEngine().SetPosition(wallpaperPositioning);
            }

            // Apply background color

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
                catch (Exception ex)
                {
                    Console.Error.WriteLine(Properties.strings.ErrorSetBackgroundColorPrefix + ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                }
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
            downloader.DownloadProgressChanged += DownloadClient_ProgressChanged;
            downloader.DownloadCompleted += DownloadClient_DownloadFileFinished;

            // Download
            lastDownloadUri = url;
            Console.WriteLine("Downloading resource from: {0}", url);
            downloader.Download(url, savepath);
        }

        static void DownloadClient_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            string readableBytesReceived = Utils.ToReadableBinaryBytes(e.BytesReceived);
            string readableBytesTotal = Utils.ToReadableBinaryBytes(e.TotalBytesToReceive);
            Utils.WriteTemporaryLine("Downloading: {0}%, {1}/{2}".PadRight(36, ' '), e.ProgressPercentage, readableBytesReceived, readableBytesTotal);
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
                Console.WriteLine("Download finished.".PadRight(36, ' '));
                wallpaperEngine.GetEngine().SetSlideshow(Environment.CurrentDirectory);
            }
        }
    }
}
