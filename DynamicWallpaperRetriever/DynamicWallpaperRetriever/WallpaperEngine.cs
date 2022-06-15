using DynamicWallpaperRetriever.Slideshows;
using IDesktopWallpaperWrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWallpaperRetriever
{
    /// <summary>
    /// Provides custom advanced methods of automating and interfacing with DesktopWallpaper.
    /// </summary>
    public static class WallpaperEngine
    {

        private static readonly DesktopWallpaper _wallpaperInterface = new DesktopWallpaper();
        private static readonly HashSet<SlideshowPreset> SlideshowPresets = new HashSet<SlideshowPreset>();


        /// <summary>
        /// Obtains the Windows API COM interface for wallpaper manipulations.
        /// </summary>
        /// <returns>A wrapper instance of COM IDesktopWallpaper interface.</returns>
        public static DesktopWallpaper GetInterface()
        {
            return _wallpaperInterface;
        }
        #region Wallpapers

        internal static void CheckImageFileValidity(string imagePath)
        {
            if (!Utils.IsValidFilePath(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException("The specified wallpaper path is invalid.", imagePath);
            }
            FileInfo imageFile = new FileInfo(imagePath);
            if (!AppConfig.SupportedExtensions.Contains(imageFile.Extension.ToLowerInvariant()))
            {
                throw new FileFormatException("The specified wallpaper file does not have a supported extension. " +
                    "The file extension must be one of the following: " + String.Join(", ", AppConfig.SupportedExtensions));
            }
        }

        /// <summary>
        /// Gets the background color for all monitors, in HTML hex color code format.
        /// </summary>
        /// <returns>The hexadecimal HTML color code.</returns>
        public static string GetBackgroundHexColor()
        {
            Color bgColor = _wallpaperInterface.GetBackgroundColor();
            return ColorTranslator.ToHtml(bgColor);
        }

        /// <summary>
        /// Sets a background color for all monitors using a HTML hex color code.
        /// </summary>
        /// <param name="backgroundColorHex">The hexadecimal HTML color code.</param>
        /// <exception cref="Exception">backgroundColorHex is not a properly formatted HTML color code.</exception>
        public static void SetBackgroundHexColor(string backgroundColorHex)
        {
            // Converts color string
            Color bgColor = Color.Empty;
            try
            {
                bgColor = ColorTranslator.FromHtml(backgroundColorHex);
            }
            catch (Exception)
            {
                throw new ArgumentException(Properties.strings.ErrorHtmlColorCodeFormat);
            }

            // Apply color
            if (bgColor != Color.Empty)
            {
                _wallpaperInterface.SetBackgroundColor(bgColor);
            }
        }

        /// <summary>
        /// Convenience method to set an image file as wallpaper for all monitors.
        /// 
        /// To set the wallpaper on an individual monitor,
        /// call <see cref="DesktopWallpaper.SetWallpaper(string, string)">GetEngine().SetWallpaper(monitorID, path)</see>.
        /// </summary>
        /// <param name="imagePath">The fully-qualified path to the image file.</param>
        public static void SetWallpaper(string imagePath)
        {
            CheckImageFileValidity(imagePath);

            imagePath = Path.GetFullPath(imagePath);

            foreach (string monitorID in _wallpaperInterface.GetActiveMonitorIDs())
            {
                _wallpaperInterface.SetWallpaper(monitorID, imagePath);
            }
        }

        /// <summary>
        /// Sets the wallpaper on a specific monitor.
        /// 
        /// Note that setting wallpaper on a monitor, when a slideshow has been configured on that monitor,
        /// will disable the slideshow.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="monitorIndex"></param>
        public static void SetWallpaper(string imagePath, int monitorIndex)
        {
            string monitorID = GetInterface().GetMonitorDevicePathAt((uint)monitorIndex);
            if (IsWindowsSlideshowConfigured())
            {
                // Disables Windows slideshow
                RemoveWindowsSlideshow();
            }

            _wallpaperInterface.SetWallpaper(monitorID, imagePath);
        }

        #endregion Wallpapers

        #region Slideshows

        /// <summary>
        /// Advances the slideshow on all configured and active presets.
        /// </summary>
        /// <param name="presets">The list of configured presets.</param>
        public static void AdvanceSlideshows(IEnumerable<SlideshowPreset> presets)
        {
            foreach (SlideshowPreset preset in presets)
            {
                // Skip inactive presets
                if (!preset.Enabled) continue;

                string imagePath = preset.NextImagePath();

                foreach (string monitorID in preset.NextMonitors())
                {
                    _wallpaperInterface.SetWallpaper(monitorID, imagePath);
                }
            }
        }

        public static void AddSlideshow(SlideshowPreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException("preset");
            }
            if (IsMonitorConfiguredInSlideshow(preset.MonitorIDs))
            {
                throw new ArgumentException("One or more monitors specified in the slideshow preset is already configured in another preset.",
                    "preset");
            }

            SlideshowPresets.Add(preset);
        }

        /// <summary>
        /// Obtains a copy of all configured slideshows as an array.
        /// </summary>
        /// <returns>An array of <see cref="SlideshowPreset"/>s.</returns>
        public static SlideshowPreset[] GetSlideshows()
        {
            return SlideshowPresets.ToArray();
        }

        public static SlideshowPreset[] GetSlideshows(string monitorID)
        {
            List<SlideshowPreset> configuredSlideshows = new List<SlideshowPreset>();
            foreach (SlideshowPreset preset in SlideshowPresets)
            {
                if (preset.MonitorIDs.Contains(monitorID))
                {
                    configuredSlideshows.Add(preset);
                }
            }
            return configuredSlideshows.ToArray();
        }

        public static SlideshowPreset GetSlideshowFromId(string monitorID)
        {
            foreach (SlideshowPreset preset in SlideshowPresets)
            {
                if (preset.MonitorIDs.Contains(monitorID))
                {
                    return preset;
                }
            }
            return GetWindowsSlideshow();
        }

        public static SlideshowPreset GetWindowsSlideshow()
        {
            if (IsWindowsSlideshowConfigured())
            {
                return SlideshowPreset.FromWindowsSlideshow();
            }
            return null;
        }

        /// <summary>
        /// Removes slideshow presets with a specified name.
        /// </summary>
        /// <param name="name">The identifying name of the slideshow preset.</param>
        public static void RemoveSlideshow(string name)
        {
            // Remove Windows slideshow, if configured
            if (IsWindowsSlideshowConfigured())
            {
                RemoveWindowsSlideshow();
            }

            // Remove custom slideshow presets
            var toBeRemoved = SlideshowPresets.TakeWhile(preset => preset.Name == name);
            SlideshowPresets.RemoveWhere(x => toBeRemoved.Contains(x));
        }

        internal static bool IsMonitorConfiguredInSlideshow(string monitorID)
        {
            return IsMonitorConfiguredInSlideshow( new string[] {monitorID} );
        }

        internal static bool IsMonitorConfiguredInSlideshow(IEnumerable<string> monitorIDs)
        {
            if (IsWindowsSlideshowConfigured())
            {
                return true;
            }

            foreach (SlideshowPreset preset in SlideshowPresets)
            {
                foreach (string monitorID in monitorIDs)
                {
                    if (preset.MonitorIDs.Contains(monitorID)) return false;
                }
            }
            return true;
        }

        internal static bool IsWindowsSlideshowConfigured()
        {
            if (_wallpaperInterface.GetSlideshowStatus()
                .HasFlag(IDesktopWallpaperWrapper.Win32.DESKTOP_SLIDESHOW_STATE.DSS_SLIDESHOW))
            {
                // A Windows slideshow is currently configured
                return true;
            }
            return false;
        }

        internal static void RemoveWindowsSlideshow()
        {
            // Method: fetch image location of every image displayed, then reapply one-by-one
            // on each corresponding monitor
            string[] activeMonitorIDs = _wallpaperInterface.GetActiveMonitorIDs();
            foreach (string monitorID in activeMonitorIDs)
            {
                string imagePath = _wallpaperInterface.GetWallpaper(monitorID);
                if (File.Exists(imagePath))
                {
                    _wallpaperInterface.SetWallpaper(monitorID, imagePath);
                }
            }
        }

        #endregion Slideshows

    }
}
