using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace DynamicWallpaperRetriever
{
    /// <summary>
    /// Defines the type of progression pattern for selecting wallpaper images in a slideshow.
    /// </summary>
    [DataContract(Name = "EnumShuffleType")]
    public enum ShuffleType
    {
        /// <summary>
        /// No shuffling, going through slideshow images in an ordered way.
        /// </summary>
        [EnumMember(Value = "Ordered")]
        Ordered,

        /// <summary>
        /// Randomly selects slideshow images in a nonrepeating way, until the slideshow directory has been exhausted.
        /// </summary>
        /// <remarks>This is the default behaviour of Windows wallpaper engine.</remarks>
        [EnumMember(Value = "Nonrepeating")]
        Nonrepeating,

        /// <summary>
        /// Randomly selects an image from the slideshow directory, except the image currently being displayed.
        /// </summary>
        [EnumMember(Value = "Random")]
        Random
    }

    [DataContract(Name = "EnumMonitorProgression")]
    public enum MonitorProgression
    {
        /// <summary>
        /// Sets all configured monitors to the same image.
        /// </summary>
        [EnumMember(Value = "AllMonitors")]
        All,

        /// <summary>
        /// Perform an ordered alternation between active monitors.
        /// </summary>
        [EnumMember(Value = "Alternating")]
        Alternating,

        /// <summary>
        /// Selects a single random monitor for the next image to be displayed.
        /// </summary>
        [EnumMember(Value = "Random")]
        Random
    }

    /// <summary>
    /// A slideshow preset for this application.
    /// </summary>
    [Serializable]
    [DataContract]
    public class SlideshowPreset : ISerializable
    {
        #region Fields & Properties

        private static DesktopWallpaper _wallpaperEngine = new DesktopWallpaper();

        /// <summary>
        /// Gets or sets whether this slideshow preset is enabled.
        /// A disabled preset will not be operated on by WallpaperEngine.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The unique name of this preset.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The directory of the slideshow.
        /// </summary>
        public string SlideshowFolder { get; set; }

        /// <summary>
        /// Whether to recursively search through subfolders for image files.
        /// </summary>
        public bool IncludeSubFolders { get; set; }

        /// <summary>
        /// The solid color of the background to be shown where the wallpaper pictures don't cover.
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// A CSV formatted list of monitors on which to set the slideshow.
        /// Set to <c>null</c> to target all active monitors.
        /// </summary>
        public string[] MonitorIDs { get; set; } = null;

        /// <summary>
        /// The type of the randomness for the slideshow: ordered/nonrepeating/random
        /// </summary>
        [DataMember]
        public ShuffleType ShuffleType { get; set; } = ShuffleType.Ordered;

        /// <summary>
        /// The ordered list of image names already shuffled through.
        /// </summary>
        /// <remarks>Affects nonrepeating shuffling only.</remarks>
        public List<string> ShuffledImages { get; private set; } = new List<string>();

        /// <summary>
        /// The absolute path of the image currently on.
        /// </summary>
        /// <remarks>Affects ordered shuffling only.</remarks>
        public string CurrentImage { get; private set; } = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new slideshow configuration with current background color.
        /// </summary>
        /// <param name="presetName">An unique name for this preset.</param>
        /// <param name="slideshowFolder">The folder in which the slideshow images are stored.</param>
        /// <param name="monitorIDs">The IDs of the monitors on which the slideshow will be applied. Set to <c>null</c> to target all monitors.</param>
        /// <param name="includeSubFolders">Whether to recursively list image files from subfolders.</param>
        /// <param name="shuffleType">The type of shuffling to perform.</param>
        public SlideshowPreset(string presetName, string slideshowFolder, bool includeSubFolders = false, string[] monitorIDs = null, ShuffleType shuffleType = ShuffleType.Ordered) :
            this(presetName, _wallpaperEngine.GetBackgroundColor(), slideshowFolder, includeSubFolders, monitorIDs, shuffleType)
        {

        }

        /// <summary>
        /// Constructs a new slideshow configuration.
        /// </summary>
        /// <param name="presetName">An unique name for this preset.</param>
        /// <param name="backgroundColor">The solid color of the desktop's background, visible in areas without wallpaper coverage.</param>
        /// <param name="slideshowFolder">The folder in which the slideshow images are stored.</param>
        /// <param name="includeSubFolders">Whether to recursively list image files from subfolders.</param>
        /// <param name="monitorIDs">The IDs of the monitors on which the slideshow will be applied. Set to <c>null</c> to target all monitors.</param>
        /// <param name="shuffleType">The type of shuffling to perform.</param>
        public SlideshowPreset(string presetName, Color backgroundColor, string slideshowFolder, bool includeSubFolders = false, string[] monitorIDs = null, ShuffleType shuffleType = ShuffleType.Ordered)
        {
            Name = presetName;
            BackgroundColor = backgroundColor;
            SlideshowFolder = slideshowFolder;
            MonitorIDs = monitorIDs;
            IncludeSubFolders = includeSubFolders;
            ShuffleType = shuffleType;
        }

        // constructor for serialization purposes
        public SlideshowPreset(SerializationInfo info, StreamingContext context)
        {
            Enabled = (bool) info.GetValue("enabled", typeof(bool));

            Name = (string) info.GetValue("presetname", typeof(string));

            BackgroundColor = Color.FromArgb( (int) info.GetValue("backgroundcolor", typeof(int)) );

            SlideshowFolder = (string) info.GetValue("slideshowfolder", typeof(string));
            // Sanitize path to ensure full path
            SlideshowFolder = Path.GetFullPath(SlideshowFolder);

            IncludeSubFolders = (bool) info.GetValue("subfolderscan", typeof(bool));

            MonitorIDs = (string[]) info.GetValue("monitorids", typeof(string[]));

            ShuffleType = (ShuffleType) info.GetValue("shuffletype", typeof(ShuffleType));

            string[] shuffledImagesArray = (string[]) info.GetValue("shuffledimagepaths", typeof(string[]));
            ShuffledImages = shuffledImagesArray.ToList();
            // Sanitize every path to ensure full path
            ShuffledImages.ForEach(s => Path.GetFullPath(s));

            CurrentImage = (string) info.GetValue("currentimagepath", typeof(string));
            // Sanitize path to ensure full path
            CurrentImage = Path.GetFullPath(CurrentImage);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Obtains the slideshow preset from current Windows wallpaper API configuration.
        /// </summary>
        /// <returns>A <see cref="SlideshowPreset"/> based on currently configured Windows slideshow,
        /// or <c>null</c> if no slideshow is configured.</returns>
        public static SlideshowPreset FromWindowsSlideshow(string presetName = null)
        {
            string slideshowFolder = _wallpaperEngine.GetSlideshowFolder();

            if (slideshowFolder == null)
            {
                return null;
            }

            presetName = presetName == null ? "Windows slideshow" : presetName;
            bool shuffle = _wallpaperEngine.IsSlideshowShufflingEnabled();
            ShuffleType shuffleType = shuffle ? ShuffleType.Nonrepeating : ShuffleType.Ordered;
            return new SlideshowPreset(presetName, slideshowFolder, false, null, shuffleType);
        }

        /// <summary>
        /// Obtains the path of the next image in the slideshow and updates current preset's metadata.
        /// If more than one monitor is configured for this slideshow preset, simply call this method multiple times.
        /// 
        /// <para>
        /// Behaviour for each <see cref="ShuffleType"/>:
        /// <list type="bullet">
        ///     <item><see cref="ShuffleType.Ordered"/>: round-robin image selection</item>
        ///     <item><see cref="ShuffleType.Nonrepeating"/>: Randomly selects an image from the remaining images not yet displayed. If wallpaper folder has been exhausted, restart from beginning.</item>
        ///     <item><see cref="ShuffleType.Random"/>: Randomly selects any image in the folder.</item>
        /// </list>
        /// For all three above shuffling configurations, this method will always output an image path different from the current one.
        /// </para>
        /// 
        /// <para>
        /// This method assumes the caller uses the returned image path to set the next wallpaper before calling this method once again.
        /// Otherwise, parity between this <see cref="SlideshowPreset"/> and the actual displayed wallpaper could be lost.
        /// </para>
        /// </summary>
        /// <returns>A fully-qualified absolute path to the image file, or <c>null</c> if there is no image to be chosen from (e.g. no image in folder).</returns>
        /// <remarks>Note: this method could be time-consuming to call over enormous wallpaper directories.</remarks>
        public string NextImagePath()
        {
            // 1) Obtain a list of images
            SearchOption recursiveListFiles = IncludeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            List<string> slideshowImagePaths = Directory.EnumerateFiles(SlideshowFolder, "*.*", recursiveListFiles)
                .Where( s => AppConfig.SupportedExtensions.Contains(Path.GetExtension(s).ToLowerInvariant()))
                .Select(s => Path.GetFullPath(s))       // Sanitize every path to ensure full path
                .ToList();

            // 1.1) If there are less than 2 images in the slideshow folder, then we don't need to update current slideshow image at all
            if (slideshowImagePaths.Count == 0) { return null; }
            else if (slideshowImagePaths.Count == 1) { return slideshowImagePaths[0]; }

            // 2) depending on the shuffling configuration, we retrieve the path of the next image & update ShuffledImages and CurrentImage
            string nextPath = null;

            if (ShuffleType == ShuffleType.Ordered)
            {
                int currentIndex = ShuffledImages.IndexOf(CurrentImage);
                // Round-robin selection of images
                int nextIndex = (currentIndex + 1) % slideshowImagePaths.Count;
                nextPath = slideshowImagePaths[nextIndex];
            }
            else if (ShuffleType == ShuffleType.Nonrepeating)
            {
                // Below list only contains images not yet displayed
                List<string> remainingImages;

                // Estimates whether the wallpaper folder has been exhausted
                if (slideshowImagePaths.Count <= ShuffledImages.Count)
                {
                    // If exhausted, restart shuffling history 
                    remainingImages = slideshowImagePaths;
                    remainingImages.Remove(CurrentImage);
                    ShuffledImages = new List<string>();
                }
                else
                {
                    remainingImages = slideshowImagePaths.Except(ShuffledImages).ToList();
                }

                Random rng = new Random();

                int nextIndex = rng.Next(remainingImages.Count);
                nextPath = remainingImages[nextIndex];
            }
            else if (ShuffleType == ShuffleType.Random)
            {
                // Uses a RNG to set the next wallpaper
                Random rng = new Random();

                while (nextPath == null)
                {
                    int nextIndex = rng.Next(slideshowImagePaths.Count);
                    nextPath = slideshowImagePaths[nextIndex];

                    // Check if the randomly selected image is same as current one
                    if (nextPath == CurrentImage)
                    {
                        nextPath = null;
                    }
                }
            }

            CurrentImage = nextPath;
            ShuffledImages.Add(nextPath);

            return nextPath;
        }

        /// <summary>
        /// Obtains a list of monitors IDs corresponding to the next monitors in the slideshow
        /// </summary>
        /// <returns>An array containing a list of monitor IDs.</returns>
        public string[] NextMonitors()
        {
            List<string> nextMonitorIds = new List<string>();
            HashSet<string> activeMonitorIDs = new HashSet<string>(_wallpaperEngine.GetActiveMonitorIDs());

            foreach (string monitorID in MonitorIDs)
            {
                if (activeMonitorIDs.Contains(monitorID))
                {
                    nextMonitorIds.Add(monitorID);
                }
            }

            return nextMonitorIds.ToArray();
        }

        #endregion

        #region Serialization

        public static SlideshowPreset FromXmlString(string serialized)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SlideshowPreset));

            using (StringReader deserializerStream = new StringReader(serialized)) {
                return (SlideshowPreset) serializer.Deserialize(deserializerStream);
            }
        }

        public string ToXmlString()
        {
            XmlSerializer serializer = new XmlSerializer(this.GetType());

            using (StringWriter serializerStream = new StringWriter())
            {
                serializer.Serialize(serializerStream, this);
                return serializerStream.ToString();
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("enabled", Enabled);

            info.AddValue("presetname", Name, typeof(string));

            info.AddValue("backgroundcolor", BackgroundColor.ToArgb());

            info.AddValue("slideshowfolder", SlideshowFolder, typeof(string));

            info.AddValue("subfolderscan", IncludeSubFolders);

            info.AddValue("monitorids", MonitorIDs, typeof(string[]));

            info.AddValue("shuffletype", ShuffleType, typeof(ShuffleType));

            string[] shuffledImagesArray = ShuffledImages.ToArray();
            info.AddValue("shuffledimagepaths", shuffledImagesArray, typeof(string[]));

            info.AddValue("currentimagepath", CurrentImage, typeof(string));
        }

        #endregion
    }
}
