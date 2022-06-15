using DynamicWallpaperRetriever.Slideshows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWallpaperRetriever
{
    /// <summary>
    /// Contains this application's persistent configurations.
    /// </summary>
    /// <remarks>
    /// Default behaviour for parsing malformed configurations from disk is to revert to default value
    /// and writing it back instead of throwing an exception, as this program is intended to be executed
    /// 
    /// </remarks>
    public static class AppConfig
    {
        #region Wallpaper configs

        private const string WallpaperConfigName = "Wallpaper";
        public sealed class WallpaperConfig : ConfigurationSection
        {
            private const string LastMonitorName = WallpaperConfigName + "-LastMonitor";

            /// <summary>
            /// The monitor ID on which the custom wallpaper is shown.
            /// <c>null</c> represents all monitors.
            /// </summary>
            public string LastMonitor {
                get
                {
                    string rawValue = GetConfiguration(LastMonitorName);
                    return rawValue.Trim();
                }
                set
                {
                    SetConfiguration(LastMonitorName, value);
                }
            }

            private const string LastDownloadUrlName = WallpaperConfigName + "-LastDownloadUrl";

            /// <summary>
            /// The location of the last downloaded image resource.
            /// </summary>
            public Uri LastDownloadUrl {
                get
                {
                    string rawValue = GetConfiguration(LastMonitorName);
                    if ( !Uri.TryCreate(rawValue, UriKind.RelativeOrAbsolute, out Uri parsedValue) )
                    {
                        return null;
                    }
                    return parsedValue;
                }
                set
                {
                    SetConfiguration(LastDownloadUrlName, value.AbsoluteUri);
                }
            }

        }

        #endregion

        #region Slideshow configs

        private const string SlideshowConfigName = "Slideshow";
        public sealed class SlideshowConfig : ConfigurationSection
        {
            private const string PresetsFolderName = SlideshowConfigName + "-PresetsFolder";
            public static string DefaultPresetFolder = Utils.AddLastFolderSeparator(StartupPath) + "Slideshows";

            /// <summary>
            /// A CSV format list of slideshow preset config file paths
            /// </summary>
            public static string PresetsFolder
            {
                get
                {
                    string rawValue = GetConfiguration(PresetsFolderName);
                    if (!Utils.IsValidDirectoryPath(rawValue))
                    {
                        return DefaultPresetFolder;
                    }
                    if (!Directory.Exists(rawValue)) {
                        Directory.CreateDirectory(rawValue);
                    }
                    return rawValue;
                }
                set
                {
                    SetConfiguration(PresetsFolderName, value);
                }
            }
            

        private static SlideshowPreset[] LoadPresets()
            {
                List<SlideshowPreset> presets = new List<SlideshowPreset>();
                DirectoryInfo presetsFolder = new DirectoryInfo(PresetsFolder);
                FileInfo[] presetFiles = presetsFolder.GetFiles("*.xml");

                foreach (FileInfo presetFile in presetFiles)
                {
                    StreamReader presetReader = presetFile.OpenText();
                    string serializedPreset = presetReader.ReadToEnd();
                    presetReader.Close();

                    try
                    {
                        SlideshowPreset builtPreset = SlideshowPreset.FromXmlString(serializedPreset);
                        presets.Add(builtPreset);
                    }
                    catch (Exception)
                    {
                        // Ignore any malformed serialized SlideshowPresets, most likely due to user tampering
                    }
                }
                return presets.ToArray();
            }

            // Since I/O access can be costly, we cache presets in memory until a .xml file has changed
            private static SlideshowPreset[] _presets;
            private static bool needUpdatePresets = true;

            private static FileSystemWatcher presetsDirWatcher = null;

            /// <summary>
            /// An array of all available, valid slideshow presets read from the presets folder.
            /// </summary>
            public static SlideshowPreset[] Presets {
                get
                {
                    if (presetsDirWatcher == null)
                    {
                        presetsDirWatcher = new FileSystemWatcher(PresetsFolder, ".xml");
                        presetsDirWatcher.Changed += delegate { needUpdatePresets = true; };
                    }

                    if (needUpdatePresets)
                    {
                        _presets = LoadPresets();
                        needUpdatePresets = false;
                    }
                    return _presets;
                }
                set
                {
                    List<SlideshowPreset> activePresets = new List<SlideshowPreset>();
                    foreach (SlideshowPreset preset in value)
                    {
                        if (preset.Enabled)
                        {
                            activePresets.Add(preset);
                        }
                        UpdatePresetFile(preset);
                    }
                    ActivePresets = activePresets.ToArray();
                }
            }

            private const string ActivePresetsName = SlideshowConfigName + "-ActivePresetsList";

            private static HashSet<string> GetActivePresetsStrings() {
                
                char sep = ',';
                string rawValue = GetConfiguration(ActivePresetsName);
                // Convert escaped commas back
                rawValue = Utils.FromEscapedString(rawValue, '\\');
                return new HashSet<string>(rawValue.Split(sep).Select(x => x.Trim()));
            }

            /// <summary>
            /// Convert a list of active (Enabled) SlideshowPresets into a CSV string to update the configuration file.
            /// </summary>
            /// <param name="value">The list of active presets.</param>
            private static void SetActivePresetsStrings(IEnumerable<string> value)
            {
                char sep = ',';
                string configValue = String.Join(sep.ToString(), value);
                // Escape commas
                configValue = Utils.EscapeString(configValue, new char[sep], '\\');
                SetConfiguration(ActivePresetsName, configValue);
            }

            /// <summary>
            /// An array of active slideshow presets.
            /// </summary>
            public static SlideshowPreset[] ActivePresets
            {
                get
                {
                    HashSet<string> activePresetNames = GetActivePresetsStrings();
                    return Presets.Where(preset => activePresetNames.Contains(preset.Name)).ToArray();
                }
                private set
                {
                    SetActivePresetsStrings( value.Select(preset => preset.Name.Trim()) );
                }
            }

            /// <summary>
            /// Updates the given preset's corresponding save file, overwriting any existing file
            /// of the same preset name.
            /// </summary>
            /// <param name="preset"></param>
            public static void UpdatePresetFile(SlideshowPreset preset)
            {
                string xmlSerializedPreset = preset.ToXmlString();
                string presetFilePath = Path.Combine(PresetsFolder, preset.Name + ".xml");
                File.WriteAllText(presetFilePath, xmlSerializedPreset);
            }
        }

        #endregion

        private static string configFilePath = System.Reflection.Assembly.GetEntryAssembly().Location + ".config";
        private static Configuration config = ConfigurationManager.OpenExeConfiguration(ExecutablePath);
        private static System.Collections.Specialized.NameValueCollection AppSettingsConfigs
        {
            get
            {
                ConfigFileCheck();
                return ConfigurationManager.AppSettings;
            }
        }

        /// <summary>
        /// Obtains the current app's running directory.
        /// </summary>
        public static string ExecutablePath
        {
            get
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(exePath);
            }
        }

        /// <summary>
        /// Obtains the current app's startup directory.
        /// </summary>
        public static string StartupPath
        {
            get
            {
                string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                return Path.GetDirectoryName(exePath);
            }
        }

        /// <summary>
        /// A list of supported image file formats for the Windows wallpaper API.
        /// </summary>
        public static readonly HashSet<string> SupportedExtensions = new HashSet<string>(
            new string[] {"bmp", "dib", "gif", "jfif", "jpe", "jpeg", "jpg", "png", "tif", "tiff", "wdp"});

        //public static readonly string AppName = Path.GetFileName(StartupPath);
        public const string AppName = "DynamicWallpaperRetriever.exe";

        internal static void CreateConfigurationFile()
        {
            try
            {
                StringBuilder blankConfigFile = new StringBuilder();
                blankConfigFile.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                blankConfigFile.AppendLine("<configuration>");
                blankConfigFile.AppendLine("</configuration>");
                File.WriteAllText(configFilePath, blankConfigFile.ToString());
            }
            catch (IOException e)
            {
                Console.WriteLine("Error while creating configuration file: {0}", e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static bool ConfigFileCheck()
        {
            if (!File.Exists(configFilePath))
            {
                CreateConfigurationFile();
                return false;
            }
            return true;
        }

        public static bool IsConfigurationPresent(string key)
        {
            return AppSettingsConfigs[key] != null;
        }

        public static string GetConfiguration(string key)
        {
            return ConfigFileCheck() ? AppSettingsConfigs[key] : null;
        }

        public static void SetConfiguration(string key, string value)
        {
            // Updates value pair
            if (ConfigFileCheck() && IsConfigurationPresent(key))
            {
                AppSettingsConfigs[key] = value;
            }
            else
            {
                AppSettingsConfigs.Add(key, value);
            }

            // Saves config
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        }
    }
}
