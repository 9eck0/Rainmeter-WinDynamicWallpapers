using CommandLine;
using CommandLine.Text;
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

            var parseResults = Parser.Default.ParseArguments<CommandLineOptions.GetOptions,
                    CommandLineOptions.SetWallpaperOptions,
                    CommandLineOptions.SlideshowOptions>(args);
            
            if (args.Length == 0 || args.Length == 1 && args[0].Trim() == "")
            {
                // No argument given. Show help

                var helpText = HelpText.AutoBuild(parseResults);
                helpText.AddDashesToOption = true;
                helpText.AddNewLineBetweenHelpSections = true;
                helpText.Copyright = new CopyrightInfo("9eck0", DateTime.Now.Year);

                helpText.AddPreOptionsLine(Environment.NewLine);

                Console.WriteLine(helpText);
            }
            else
            {
                // Execute command

                parseResults
                    .WithParsed<CommandLineOptions.GetOptions>(options => GetWallpaper(options))
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
        private static void GetWallpaper(CommandLineOptions.GetOptions options)
        {
            // Result string to output
            String result;

            bool isMonitorIndexSpecified = options.MonitorIndex >= 0;

            // Obtains monitor ID, if specified
            string monitorID;
            if (isMonitorIndexSpecified)
            {
                try
                {
                    monitorID = WallpaperEngine.GetInterface().GetMonitorDevicePathAt((uint)options.MonitorIndex);
                }
                catch (COMException)
                {
                    FaultyCommand(Properties.strings.FaultyCommandErrorPrefix + Properties.strings.ErrorGetMonitorIndexOutOfBounds);
                    return;
                }
            }
            else
            {
                monitorID = "";
            }

            // Parse operation
            if (options.MonitorCount)
            {
                // Monitor count
                result = WallpaperEngine.GetInterface().GetActiveMonitorIDs().Length.ToString();
            }
            else if (options.Slideshow)
            {
                // Obtain slideshow folder path
                if(isMonitorIndexSpecified)
                {
                    // Monitor index specified
                    Slideshows.SlideshowPreset preset = WallpaperEngine.GetSlideshowFromId(monitorID);
                    if (preset != null)
                    {
                        result = preset.SlideshowFolder;
                    }
                    else
                    {
                        result = "";
                    }
                }
                else
                {
                    // No monitor index => separate all paths using newline
                    result = String.Join(Environment.NewLine, WallpaperEngine.GetInterface().GetSlideshow());
                }
            }
            else if (options.Wallpaper)
            {
                // Obtain wallpaper location
                if (isMonitorIndexSpecified)
                {
                    // Monitor index specified
                    result = WallpaperEngine.GetInterface().GetWallpaper(monitorID);
                }
                else
                {
                    // No monitor index => separate all paths using newline
                    List<string> activeWallpapers = new List<string>();
                    foreach (String activeMonitorID in WallpaperEngine.GetInterface().GetActiveMonitorIDs())
                    {
                        activeWallpapers.Add(WallpaperEngine.GetInterface().GetWallpaper(activeMonitorID));
                    }
                    result = String.Join(Environment.NewLine, activeWallpapers);
                }
            }
            else if (options.MonitorID)
            {
                if (isMonitorIndexSpecified)
                {
                    // Monitor index specified
                    result = monitorID;
                }
                else
                {
                    // No monitor index => get all starting from index 0
                    result = String.Join(Environment.NewLine, WallpaperEngine.GetInterface().GetActiveMonitorIDs());
                }
            }
            else
            {
                // Invalid command
                FaultyCommand(Properties.strings.FaultyCommandErrorPrefix + Properties.strings.ErrorGetInvalidCommand);
                return;
            }

            // Finally, outputs to the desired stream
            if (options.OutputFile == null || options.OutputFile == "")
            {
                // Output to console
                Console.Out.WriteLine(result);
            }
            else
            {
                // Output to specified stream, overwriting any existing file
                try
                {
                    File.WriteAllText(Path.GetFullPath(options.OutputFile), result);
                }
                catch (ArgumentException)
                {
                    FaultyCommand(Properties.strings.ErrorGetOutputFilePrefix + Properties.strings.ErrorGetOutputFileIncorrect);
                }
            }
        }

        /// <summary>
        /// Functionalities:
        /// <list type="bullet">
        /// <item>Set a single wallpaper onto all monitors, or specify a monitor using either the index or monitor ID.</item>
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
                    WallpaperEngine.SetWallpaper(wallpaperFile.FullName, monitorIndex);
                }
                else if (Array.Exists(
                        WallpaperEngine.GetInterface().GetActiveMonitorIDs(),
                        x => x.Equals(options.Monitor)
                        ))
                {
                    // Specified monitor ID
                    WallpaperEngine.GetInterface().SetWallpaper(options.Monitor, wallpaperFile.FullName);
                }
                else
                {
                    FaultyCommand(Properties.strings.FaultyCommandErrorPrefix + Properties.strings.ErrorSetMonitorMalformed);
                    return;
                }
            }
            else
            {
                // Apply to all monitors
                WallpaperEngine.SetWallpaper(wallpaperFile.FullName);
            }
            Console.WriteLine("Wallpaper applied");

            // Apply wallpaper positioning

            if (options.Position >= 0 && options.Position <= 5)
            {
                DESKTOP_WALLPAPER_POSITION wallpaperPositioning = (DESKTOP_WALLPAPER_POSITION) options.Position;
                WallpaperEngine.GetInterface().SetPosition(wallpaperPositioning);
            }

            // Apply background color

            if (options.BackgroundColor != null)
            {
                try
                {
                    WallpaperEngine.SetBackgroundHexColor(options.BackgroundColor);
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
            Console.Error.WriteLine("There was an error while executing specified task.");

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
            Console.Error.WriteLine("There was an error while executing specified task.");
            Console.Error.WriteLine(message);
            
            return 1;
        }

        private static void DownloadResource(Uri url, string savepath)
        {
            // Setup
            WallpaperDownloader downloader = new WallpaperDownloader();
            downloader.DownloadProgressChanged += DownloadClient_ProgressChanged;
            downloader.DownloadCompleted += DownloadClient_DownloadFileFinished;

            // Download
            Console.WriteLine(Properties.strings.DownloadStartPrefix + url);
            downloader.Download(url, savepath);
        }

        static void DownloadClient_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            string readableBytesReceived = Utils.ToReadableBinaryBytes(e.BytesReceived);
            string readableBytesTotal = Utils.ToReadableBinaryBytes(e.TotalBytesToReceive);
            Utils.WriteTemporaryLine(Properties.strings.DownloadProgressPrefix + "{0}%, {1}/{2}".PadRight(16, ' '), e.ProgressPercentage, readableBytesReceived, readableBytesTotal);
        }

        static void DownloadClient_DownloadFileFinished(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine(Properties.strings.DownloadCancelled);
            }
            else if (e.Error != null)
            {
                Console.WriteLine(Properties.strings.ErrorDownloadPrefix + e.Error.Message);
                Console.WriteLine(e.Error.Message);
                Console.WriteLine(e.Error.StackTrace);
            }
            else
            {
                Console.WriteLine(Properties.strings.DownloadSuccess.PadRight(36, ' '));
                //WallpaperEngine.GetInterface().SetSlideshow(Environment.CurrentDirectory);
            }
        }
    }
}
