using CommandLine;

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

            [Value(index: 0, Required = true, HelpText = "URL of the image to retrieve.")]
            public string Url { get; set; }

            [Option(shortName: 'l', longName: "local", SetName = "SetFromLocalPath", Required = false, HelpText = "Whether the image is on the local filesystem.", Default = false)]
            public bool IsLocal { get; set; }

            [Option(shortName: 'm', longName: "monitor", Required = false, HelpText = "Monitor index on which to set the wallpaper (use get -c to get active monitors count). Set to -1 to display on all monitors.", Default = -1)]
            public int Monitor { get; set; }
        }

        #endregion

        #region Query current wallpapers by monitor

        [Verb(name: "get", HelpText = "Obtains current wallpaper (or other settings) and outputs the result in a custom file.")]
        public class GetWallpaperOptions
        {
            [Option(shortName: 'w', longName: "wallpaper", SetName = "GetWallpaper", HelpText = "Obtains the current wallpaper (from monitor 0 if no index is specified).")]
            public bool Wallpaper { get; set; }

            [Option(shortName: 'i', longName: "monitorindex", SetName = "GetWallpaper" , Required = false, HelpText = "Specify a specific monitor index for the wallpaper image.", Default = 0)]
            public int Index { get; set; }

            [Option(shortName: 'c', longName: "monitorcount", SetName = "GetMonitorCount", HelpText = "Gets the amount of active monitors.")]
            public bool MonitorCount { get; set; }

            [Option(shortName: 's', longName: "slideshow", SetName = "GetSlideshow", HelpText = "Gets the folder of the current slideshow. Outputs an empty file if ")]
            public bool Slideshow { get; set; }

            [Option(shortName: 'f', longName: "outputFile", Required = true, HelpText = "The path of the file to output results to.")]
            public string OutputFile { get; set; }
        }

        [Verb(name: "slideshow", HelpText = "Options for defining a custom slideshow on select monitors.")]
        public class SlideshowOptions
        {
            [Option(shortName: 'a', longName: "advance", SetName = "advanceslideshow", Required = false, HelpText = "Advances all currently configured slideshows, or alternatively select a specific preset to advance using '--preset <presetname>'.")]
            public bool Advance { get; set; }

            [Option(shortName: 'p', longName: "preset", SetName = "advanceslideshow", Required = false, HelpText = "The name of a particular preset to advance ")]
            public string PresetName { get; set; }

            [Option(shortName: 's', Default = true, SetName = "nonrepeating", Required = false, HelpText = "Shuffles the slideshow in a nonrepeating way, until the entire slideshow folder has been exhausted.")]
            public bool NonRepeatingRandom { get; set; }

            [Option(shortName: 'r', Default = false, SetName = "truerandom", Required = false, HelpText = "Shuffles the slideshow in a random way. This means any wallpaper in the folder, excluding the current one, can be the next image.")]
            public bool TrueRandom { get; set; }
        }

        #endregion
    }
}
