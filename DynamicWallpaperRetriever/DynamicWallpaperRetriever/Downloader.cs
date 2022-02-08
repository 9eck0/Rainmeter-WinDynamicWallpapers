using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWallpaperRetriever
{
    public class WallpaperDownloader
    {
        private const string userAgent = @"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";

        /// <summary>
        /// Downloads a file from the internet by first downloading it to a temporary file, then renaming it to the desired file name.
        /// </summary>
        /// <param name="Path">The URL path to the file.</param>
        /// <param name="FileName">The complete path for the desired file</param>
        /// <param name="Timeout">Optional: Specify a custom timeout, in milliseconds, for high-ping resource access</param>
        async void DownloadAsync(Uri Path, string fileName, int timeout = 30 * 1000)
        {
            string tempName = fileName + ".temp";
            if (File.Exists(tempName))
            {
                File.Delete(tempName);
            }

            try
            {
                using (CustomTimeoutWebClient client = new CustomTimeoutWebClient())
                {
                    async Task downloadFileAsync()
                    {
                        client.DefaultTimeout = timeout;

                        //For some resources, a 403: Forbidden error can be thrown if no user agent is provided.
                        client.Headers.Add("User-Agent: " + userAgent);

                        client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadClient_DownloadFileFinished);

                        lastDownloadUrl = Path.ToString();

                        // Uncomment this section to download in the main thread
                        /*client.DownloadFile(Path, tempName);*/

                        // Uncomment this section to download asynchronously
                        /*client.DownloadFileAsync(Path, tempName);*/

                        // Uncomment this section to download asynchronously using a task object
                        var asyncDownload = client.DownloadFileTaskAsync(Path, tempName);
                        asyncDownload.Wait(timeout + 5000);
                        // Waiting
                        while (asyncDownload.Status == TaskStatus.Running)
                        {
                            // Waiting for completion
                        }
                    }

                    Task downloadFile = downloadFileAsync();
                    await downloadFile;
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

        private string lastDownloadUrl;

        void DownloadClient_DownloadFileFinished(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Download cancelled for resource at: " + lastDownloadUrl);
            }
            else if (e.Error != null)
            {
                Console.WriteLine("Download failed for resource at: " + lastDownloadUrl);
                Console.WriteLine(e.Error.Message);
                Console.WriteLine(e.Error.StackTrace);
            }
            else
            {
                Console.WriteLine("Successfully downloaded resource at: " + lastDownloadUrl);
            }

            // invoke event
            //DownloadCompleted.BeginInvoke(this, e);
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
