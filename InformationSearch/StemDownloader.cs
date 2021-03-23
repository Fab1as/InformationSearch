using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace InformationSearch
{
    public class StemDownloader
    {
        private readonly Uri _url = new Uri(@"http://download.cdn.yandex.net/mystem/mystem-3.0-win7-64bit.zip", UriKind.Absolute);
        private readonly string _baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly object Lockobj = new object();

        public StemDownloader() { }

        public string GetLocalPath()
        {
            lock (Lockobj)
            {
                var directory = Path.Combine(_baseFolder, "MyStem");
                if (Directory.Exists(directory) == false)
                    Directory.CreateDirectory(directory);

                var myStemExePath = Path.Combine(directory, "mystem.exe");
                if (File.Exists(myStemExePath))
                    return myStemExePath;

                var myStemZipPath = Path.Combine(directory, "mystem.zip");
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(_url, myStemZipPath);
                    ZipFile.ExtractToDirectory(myStemZipPath, directory);
                    File.Delete(myStemZipPath);
                }

                return "";
            }
        }
    }
}
