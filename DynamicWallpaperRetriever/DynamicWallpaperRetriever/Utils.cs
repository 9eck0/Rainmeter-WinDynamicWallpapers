using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWallpaperRetriever
{
    public static class Utils
    {
        #region File system

        public static bool IsValidFilePath(string path)
        {
            if (path == null) return false;

            try
            {
                FileInfo fi = new FileInfo(path);
                if (fi is null) return false;
            }
            catch (Exception ex) when (
            ex is ArgumentException ||
            ex is ArgumentNullException ||
            ex is NotSupportedException ||
            ex is PathTooLongException)
            {
                return false;
            }
            catch (Exception ex) when (
            ex is System.Security.SecurityException ||
            ex is UnauthorizedAccessException)
            {
                // These two exceptions are usually thrown when trying to access an existing file.
                // Hence, we can check whether the path exists for the result.
                return File.Exists(path);
            }

            return true;
        }

        public static bool IsValidDirectoryPath(string path)
        {
            if (path == null) return false;

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                if (di is null) return false;
            }
            catch (Exception ex) when (
            ex is ArgumentException ||
            ex is ArgumentNullException ||
            ex is NotSupportedException)
            {
                return false;
            }
            catch (System.Security.SecurityException)
            {
                // This exception is usually thrown when trying to access an existing folder.
                // Hence, we can check whether the path exists for the result.
                return Directory.Exists(path);
            }

            return true;
        }

        /// <summary>
        /// Checks whether a path string is properly formatted.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>A boolean indicating whether the provided path is correctly formatted.</returns>
        /// <see cref="https://stackoverflow.com/questions/422090/in-c-sharp-check-that-filename-is-possibly-valid-not-that-it-exists"/>
        public static bool IsValidPath(string path)
        {
            return IsValidFilePath(path) || IsValidDirectoryPath(path);
        }

        public static string AddLastFolderSeparator(string path)
        {
            char lastChar = path[path.Length - 1];
            if (lastChar != Path.DirectorySeparatorChar ||
                lastChar != Path.AltDirectorySeparatorChar) {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }

        public static string GetDirectoryPath(string path)
        {
            path = Path.GetFullPath(path);

            if (Directory.Exists(path)) return path;

            if (path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                path += Path.DirectorySeparatorChar;
            }
            //TODO File.

            string directoryPath = Path.GetDirectoryName(path);
            return directoryPath;
        }

        #endregion File system

        #region String manipulations

        public static string EscapeString(string input, char[] targets, char escapeChar)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (targets.Contains(c) || c == escapeChar)
                {
                    sb.Append(escapeChar);
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string FromEscapedString(string input, char escapeChar)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == escapeChar)
                {
                    i++;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string ToReadableBinaryBytes(long bytes)
        {
            string[] suffixes = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
            int order = 0;
            while (bytes >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                bytes = bytes / 1024;
            }

            return String.Format("{0:0.##}{1}", bytes, suffixes[order]);
        }

        #endregion String manipulations

        #region I/O

        public static void WriteTemporaryLine(string message)
        {
            // Assuming cursor is at the end of line
            int lineWidth = Console.CursorLeft;
            // Erase current line content with spaces
            WriteTemporaryLine(Console.Out, "".PadRight(lineWidth));
            WriteTemporaryLine(Console.Out, message);
        }

        public static void WriteTemporaryLine(string format, params object[] args)
        {
            // Assuming cursor is at the end of line
            int lineWidth = Console.CursorLeft;
            // Erase current line content with spaces
            WriteTemporaryLine(Console.Out, "".PadRight(lineWidth));
            WriteTemporaryLine(Console.Out, format, args);
        }

        public static void WriteTemporaryLine(TextWriter stream, string message)
        {
            stream.Write("{0}\r", message);
        }

        public static void WriteTemporaryLine(TextWriter stream, string message, params object[] args)
        {
            stream.Write(message + "\r", args);
        }

        #endregion I/O
    }
}
