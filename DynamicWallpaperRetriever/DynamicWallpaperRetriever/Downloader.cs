using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWallpaperRetriever
{
    public class WallpaperDownloader
    {
        private const string userAgent = @"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";

        private CustomTimeoutWebClient client;

        public int DownloadProgressPercentage { get; private set; } = -1;
        public bool Active { get; private set; } = false;

        public string LastDownloadUrl { get; private set; }

        public void Download(Uri path, string fileName, int timeout = 30*1000)
        {
            var downloadTask = DownloadAsync(path, fileName, timeout);
            downloadTask.Wait();
        }

        /// <summary>
        /// Downloads a file from the internet by first downloading it to a temporary file, then renaming it to the desired file name.
        /// </summary>
        /// <param name="Path">The URL path to the file.</param>
        /// <param name="FileName">The complete path for the desired file</param>
        /// <param name="Timeout">Optional: Specify a custom timeout, in milliseconds, for high-ping resource access</param>
        /// <returns>The task object representing the download operation.</returns>
        public async Task DownloadAsync(Uri Path, string fileName, int timeout = 30 * 1000)
        {
            string tempName = fileName + ".temp";
            if (File.Exists(tempName))
            {
                File.Delete(tempName);
            }

            try
            {
                using (client = new CustomTimeoutWebClient())
                {
                    client.DefaultTimeout = timeout;

                    //For some resources, a 403: Forbidden error can be thrown if no user agent is provided.
                    client.Headers.Add("User-Agent: " + userAgent);

                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadClient_DownloadFileFinished);
                    client.DownloadFileCompleted += DownloadCompleted;
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadClient_DownloadProgressChanged);
                    client.DownloadProgressChanged += DownloadProgressChanged;

                    LastDownloadUrl = Path.ToString();

                    Active = true;

                    // Uncomment this section to download in the main thread
                    /*client.DownloadFile(Path, tempName);*/

                    // Uncomment this section to download asynchronously
                    /*client.DownloadFileAsync(Path, tempName);*/

                    // Uncomment this section to download asynchronously using a task object
                    //var asyncDownload = client.DownloadFileTaskAsync(Path, tempName);
                    // Waiting for completion
                    //asyncDownload.Wait(client.DefaultTimeout);

                    await client.DownloadFileTaskAsync(Path, tempName);
                }

                // Rename temp file to desired file name
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                File.Move(tempName, fileName);
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot download resource at: " + Path.ToString());

                if (File.Exists(tempName))
                {
                    File.Delete(tempName);
                }
            }
        }

        private void DownloadClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgressPercentage = e.ProgressPercentage;
        }

        void DownloadClient_DownloadFileFinished(object sender, AsyncCompletedEventArgs e)
        {
            Active = false;
        }

        private class CustomTimeoutWebClient : WebClient
        {
            /// <summary>
            /// A default timeout of 30 seconds to allow for background tasks handshaking on a high latency connection.
            /// </summary>
            public int DefaultTimeout = 30 * 1000;

            protected override WebRequest GetWebRequest(Uri uri)
            {
                return GetWebRequest(uri, DefaultTimeout);
            }

            protected WebRequest GetWebRequest(Uri uri, int timeout)
            {
                if (timeout <= 0)
                {
                    throw new ArgumentOutOfRangeException("timeout", "WebRequest Timeout cannot be zero or negative");
                }

                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = timeout;
                return w;
            }
        }

        #region Events
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        public event System.ComponentModel.AsyncCompletedEventHandler DownloadCompleted;
        #endregion Events
    }
}
