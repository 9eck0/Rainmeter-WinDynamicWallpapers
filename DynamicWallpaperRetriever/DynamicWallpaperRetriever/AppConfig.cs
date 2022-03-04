using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWallpaperRetriever
{
    public static class AppConfig
    {
        public sealed class WallpaperConfig : ConfigurationSection
        {

            /// <summary>
            /// The monitor index on which the custom wallpaper is shown.
            /// </summary>
            [ConfigurationProperty("monitor", DefaultValue = -1, IsRequired = false)]
            public int Monitor { get; set; }

            [ConfigurationProperty("lasturl")]
            public string LastDownloadUrl { get; set; }

        }

        public sealed class SlideshowConfig : ConfigurationSection
        {
            public string SlideshowSets { get; set; }

            
        }

        private static Configuration config = ConfigurationManager.OpenExeConfiguration(ExecutablePath);

        /// <summary>
        /// Obtains the current app's running directory.
        /// </summary>
        public static string ExecutablePath
        {
            get
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                return System.IO.Path.GetDirectoryName(exePath);
            }
        }

        /// <summary>
        /// A list of supported image file formats for the Windows wallpaper API.
        /// </summary>
        public static readonly HashSet<string> SupportedExtensions = new HashSet<string>(
            new string[] {"bmp", "dib", "gif", "jfif", "jpe", "jpeg", "jpg", "png", "tif", "tiff", "wdp"});
    }
}
