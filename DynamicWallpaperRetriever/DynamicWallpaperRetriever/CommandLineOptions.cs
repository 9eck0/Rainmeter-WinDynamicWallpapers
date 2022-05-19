using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace DynamicWallpaperRetriever
{
    public class CommandLineOptions
    {
        public enum BuiltinWallpaperProviders
        {
            GoesEast,
            GoesWest,
            Himawari,
            Imerg
        }

        #region Wallpaper download and display

        [Verb(name: "set", isDefault: true, HelpText = "Sets the wallpaper to be displayed on specific monitor(s)")]
        public class SetWallpaperOptions
        {

            [Value(index: 0, Required = true,
                HelpText = "URL of the image to retrieve.")]
            public string Url { get; set; }

            [Option(shortName: 'l', longName: "local", SetName = "SetFromLocalPath", Required = false, Default = false,
                HelpText = "Whether the image is on the local filesystem.")]
            public bool IsLocal { get; set; }

            [Option(shortName: 's', longName: "savepath", SetName = "SetFromRemotePath", Required = true, Default = null,
                HelpText = "Location to save the downloaded image file to. Can be an absolute or relative path.")]
            public string SavePath { get; set; }

            [Option(shortName: 'm', longName: "monitor", Required = false, Default = "",
                HelpText = "Monitor index or ID on which to set the wallpaper (use get -c to get active monitors count). Leave empty to display on all monitors.")]
            public string Monitor { get; set; }

            [Option(shortName: 'p', longName: "position", Required = false, Default = -1,
                HelpText = "The display option for wallpapers: 0 = center; 1 = tile; 2 = stretch; 3 = fit; 4 = fill; 5 = span")]
            public int Position { get; set; }

            [Option(shortName: 'b', longName: "bgcolor", Required = false, Default = null,
                HelpText = "An HTML Hex code of the desired background color, which is visible on areas not covered by the wallpaper.")]
            public string BackgroundColor { get; set; }

            [Usage(ApplicationAlias = AppConfig.AppName)]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("IMERG (precipitation map, 30m) bundle", new SetWallpaperOptions {
                        Url = "https://trmm.gsfc.nasa.gov/trmm_rain/Events/ATLA/latest_big_half_hourly_gridded.jpg",
                        SavePath = "IMERG.jpg",
                        Monitor = "",
                        Position = 3,
                        BackgroundColor = "#353535"
                    });
                    yield return new Example("GOES East geocolor (Americas, 10m) bundle", new SetWallpaperOptions {
                        Url = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/FD/GEOCOLOR/1808x1808.jpg",
                        SavePath = "GoesEast.jpg",
                        Monitor = "",
                        Position = 3,
                        BackgroundColor = "#000000"
                    });
                    yield return new Example("GOES West geocolor (East Pacific, 10m) bundle", new SetWallpaperOptions
                    {
                        Url = "https://cdn.star.nesdis.noaa.gov/GOES17/ABI/FD/GEOCOLOR/1808x1808.jpg",
                        SavePath = "GoesWest.jpg",
                        Monitor = "",
                        Position = 3,
                        BackgroundColor = "#000000"
                    });
                    yield return new Example("Meteosat-11 enhanced (East Atlantic, 1h) bundle", new SetWallpaperOptions
                    {
                        Url = "https://eumetview.eumetsat.int/static-images/latestImages/EUMETSAT_MSG_RGBNatColourEnhncd_FullResolution.jpg",
                        SavePath = "Meteosat11.jpg",
                        Monitor = "",
                        Position = 3,
                        BackgroundColor = "#000000"
                    });
                    yield return new Example("Meteosat-8 enchanced (Europe & Africa, 1h) bundle", new SetWallpaperOptions
                    {
                        Url = "https://eumetview.eumetsat.int/static-images/latestImages/EUMETSAT_MSGIODC_RGBNatColourEnhncd_FullResolution.jpg",
                        SavePath = "Meteosat8.jpg",
                        Monitor = "",
                        Position = 3,
                        BackgroundColor = "#000000"
                    });
                    yield return new Example("Himawari-8 geocolor (East Asia, 10min) bundle", new SetWallpaperOptions
                    {
                        Url = "https://rammb.cira.colostate.edu/ramsdis/online/images/latest_hi_res/himawari-8/full_disk_ahi_true_color.jpg",
                        SavePath = "Himawari8.jpg",
                        Monitor = "",
                        Position = 3,
                        BackgroundColor = "#000000"
                    });
                }
            }
        }

        #endregion

        #region Query current wallpapers by monitor

        [Verb(name: "get", HelpText = "Obtains current wallpaper (or other settings) and optionally outputs the result in a custom file.")]
        public class GetWallpaperOptions
        {
            [Option(shortName: 'w', longName: "wallpaper", SetName = "GetWallpaper",
                HelpText = "Obtains the current wallpaper (from monitor 0 if no index is specified).")]
            public bool Wallpaper { get; set; }

            [Option(shortName: 'm', longName: "monitorindex", SetName = "GetWallpaper" , Required = false, Default = 0,
                HelpText = "Specify a specific monitor index for the wallpaper image.")]
            public int MonitorIndex { get; set; }

            [Option(shortName: 'c', longName: "monitorcount", SetName = "GetMonitorCount",
                HelpText = "Gets the amount of active monitors.")]
            public bool MonitorCount { get; set; }

            [Option(shortName: 's', longName: "slideshow", SetName = "GetSlideshow",
                HelpText = "Gets the folder of the current slideshow. Output is empty if no slideshow is configured.")]
            public bool Slideshow { get; set; }

            [Option(shortName: 'm', longName: "monitorindex", SetName = "GetSlideshow", Required = false, Default = -1,
                HelpText = "Specify a specific monitor index of the slideshow.")]
            public int SlideshowMonitorIndex { get; set; }

            [Option(shortName: 'o', longName: "outputfile",
                HelpText = "The path of the file to output results to. Leave empty to output to console.")]
            public string OutputFile { get; set; }
        }

        [Verb(name: "slideshow", HelpText = "Options for defining a custom slideshow on select monitors.")]
        public class SlideshowOptions
        {
            [Option(shortName: 'a', longName: "advance", SetName = "advanceslideshow", Required = false,
                HelpText = "Advances all currently configured slideshows, or alternatively select a specific preset to advance using '--preset <presetname>'.")]
            public bool Advance { get; set; }

            [Option(shortName: 'p', longName: "preset", SetName = "advanceslideshow", Required = false,
                HelpText = "The name of a specific slideshow preset to advance.")]
            public string PresetName { get; set; }

            [Option(shortName: 's', Default = true, SetName = "nonrepeating", Required = false,
                HelpText = "Shuffles the slideshow in a nonrepeating way, until the entire slideshow folder has been exhausted.")]
            public bool NonRepeatingRandom { get; set; }

            [Option(shortName: 'r', Default = false, SetName = "truerandom", Required = false,
                HelpText = "Shuffles the slideshow in a random way. This means any wallpaper in the folder, excluding the current one, can be the next image.")]
            public bool TrueRandom { get; set; }
        }

        #endregion
    }
}
