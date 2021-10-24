using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using VideoProject.Parsers;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Net;
using System.Threading;
using VideoProject.PluginInterface;

namespace VideoProjectX.Services
{
    public class DownloadsManager
    {
        private HLSParser hls;
        private SettingsManager settings;
        private HttpClient client;

        public ObservableCollection<DownloadWrapper> ListDownloads { get; }
        public DownloadsManager(SettingsManager s)
        {
            client = new HttpClient();
            hls = new HLSParser();
            settings = s;
            ListDownloads = new ObservableCollection<DownloadWrapper>();
            // Trace.Listeners.Add()
        }
        private void AddHLS(string hlsLink, string destination, string filename)
        {
            Task.Factory.StartNew(async () =>
            {
                var mPlaylist = await hls.Parse(hlsLink);
                M3UPlaylist playlist = null;
                if(settings.UseMaxQuality)
                {
                    playlist = mPlaylist.Playlists.Where(x => x.ResolutionWidth == mPlaylist.Playlists.Max(y => y.ResolutionWidth)).FirstOrDefault();
                }
                else
                {
                    var ordered = mPlaylist.Playlists.OrderByDescending(x => x.ResolutionWidth).ToList();
                    foreach(var p in ordered)
                    {
                        if(p.ResolutionWidth <= settings.MaxVideoWidth)
                        {
                            playlist = p;
                            break;
                        }
                    }
                    if (playlist == null && ordered.Count > 0)
                        playlist = ordered[ordered.Count-1];
                }
                if (playlist == null)
                    return;

                var links = playlist.VideoLinks.Select(x => x.Link).ToList();
                AddDownload(links, destination, filename, true);
            });
            
        }
        public void AddDownload(List<LinkData> linskData)
        {
            linskData.ForEach(link => AddDownload(link));
        }
        public void AddDownload(LinkData linkData)
        {
            AddDownload(linkData.Link, linkData.FolderDestination, linkData.Filename, linkData.TypeLink == VideoProject.Plugins.LinkType.HLS);
        }
        public void AddDownload(List<string> links, string dstFolder, string filename="", bool isHls = false)
        {
            DownloadWrapper wrapper = new DownloadWrapper()
            {
                DestinationFolder = dstFolder,
                Links = links,
                IsHLS = isHls,
                Filename = filename
            };
            ListDownloads.Add(wrapper);
            Download();
        }
        public void AddDownload(string directLink, string dstFolder, string filename="", bool isHls=false)
        {

            if (isHls)
            {
                var vfn_dot = MakeValidFilename(filename) + ".mp4";
                var dstPath = Path.Combine(settings.DownloadFolder, dstFolder);
                if (Directory.GetFiles(dstPath).Where(f => f.Equals(Path.Combine(dstPath, vfn_dot))).Any())
                {
                    Debug.WriteLine("File already download: " + vfn_dot);
                    return;
                }
                AddHLS(directLink, dstFolder, filename);
            }
            else
            {
                var links = new List<string>() { directLink };
                AddDownload(links, dstFolder, filename);
            }
        }
        private bool isDownloading = false;
        private void Download()
        {
            if (isDownloading)
                return;
            isDownloading = true;
            Task.Factory.StartNew(async () =>
            {
                DownloadWrapper download = null;
                while ((download = ListDownloads.FirstOrDefault(x => !x.IsComplete)) != null)
                {
                    if(download.Retry > 3)
                    {
                        download.IsComplete = true;
                        continue;
                    }
                    bool complete = false;
                    complete = download.IsHLS ? await DownloadHLSAsync(download) : await DownloadMultiLinkAsync(download);
                    download.IsComplete = complete;
                    if (complete)
                        download.OnComplete?.Invoke();
                    else
                        download.Retry++;
                }
                isDownloading = false;
            });
        }
        private async Task<bool> DownloadHLSAsync(DownloadWrapper download)
        {
            var filename = MakeValidFilename(download.Filename);
            // var filenameUrl = GetFilenameFromUrl(download.Links.First());
            // var fileExt = ".ts";//Path.GetExtension(filenameUrl);
            var dstFolder = Path.Combine(settings.DownloadFolder, download.DestinationFolder);
            Directory.CreateDirectory(dstFolder);
            var dstPath = Path.Combine(dstFolder, $"{filename}.video");
            try
            {
                using (var fileStream = new FileStream(dstPath, FileMode.Create))
                {
                    for (int partIndex = 0; partIndex < download.Links.Count; partIndex++)
                    {
                        Debug.WriteLine($"Downloading part {partIndex + 1}/{download.Links.Count} - {filename}");
                        download.CurrentLink = partIndex+1;
                        try
                        {
                            var hlsPart = await client.GetByteArrayAsync(download.Links[partIndex]);
                            if (hls != null)
                                fileStream.Write(hlsPart, 0, hlsPart.Length);
                            download.CompletePercentage = ((float)((partIndex + 1) * 100)) / download.Links.Count;
                        }
                        catch(HttpRequestException e)
                        {
                            Debug.WriteLine(e.Message);
                            partIndex--;
                        }
                    }
                }
                ChangeToMp4(dstFolder, filename, "", false);
                return true;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return false;
        }
        private void ChangeToMp4(string dstFolder, string filename, string fileExt, bool hasAudio = false)
        {
            var originVPath = Path.Combine(dstFolder, $"{filename}.video");
            var originAPath = Path.Combine(dstFolder, $"{filename}.audio");
            var dstPath = Path.Combine(dstFolder, $"{filename}.mp4");
            var ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg");
            var ffmpegArgs = $"-i \"{originVPath}\" {(hasAudio ? $"-i \"{originAPath}\"" : "")} -c:v copy -c:a copy \"{dstPath}\"";
            ProcessStartInfo pInfo = new ProcessStartInfo(ffmpegPath, ffmpegArgs);
            pInfo.CreateNoWindow = true;
            pInfo.UseShellExecute = false;
            pInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process ffmpegProcess = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = pInfo
            };
            ffmpegProcess.Exited += (s, e) =>
              {
                  Debug.WriteLine("Conversion complete");
                  if(File.Exists(originVPath))
                    File.Delete(originVPath);
                  if (File.Exists(originAPath))
                      File.Delete(originAPath);
              };
            try
            {
                ffmpegProcess.Start();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            
        }
        private async Task<bool> DownloadMultiLinkAsync(DownloadWrapper download)
        {
            int completeDownload = 0;
            var dstFolder = Path.Combine(settings.DownloadFolder, download.DestinationFolder);
            Directory.CreateDirectory(dstFolder);
            Debug.WriteLine($"Folder: {dstFolder}");
            for (int i=0;i<download.Links.Count;i++)
            {
                download.CurrentLink = i+1;
                var link = download.Links[i];
                var filename = (!String.IsNullOrEmpty(download.Filename) ? download.Filename + " - " : "") + GetFilenameFromUrl(link);
                var dstPath = Path.Combine(dstFolder, filename);
                Debug.WriteLine($"Downloading  {i + 1}/{download.Links.Count} - {filename}");
                if (File.Exists(dstPath))
                {
                    completeDownload++;
                    continue;
                }
                byte[] remoteContent = null;
                int retry = 0;
                do
                {
                    try
                    {
                        remoteContent = await client.GetByteArrayAsync(link);
                    }
                    catch(HttpRequestException e)
                    {
                        Debug.WriteLine($"[ERROR] {e.Message} - {link}");
                        remoteContent = null;
                        retry++;
                        Thread.Sleep(500);
                    }
                    
                } while (remoteContent == null && retry <= 2);
                if (remoteContent == null)
                    continue;
                using (var fileStream = new FileStream(dstPath, FileMode.Create))
                {
                    fileStream.Write(remoteContent, 0, remoteContent.Length);
                }
                completeDownload++;
                download.CompletePercentage = ((float)(completeDownload * 100)) / download.Links.Count;
            }
            return completeDownload > 0;
        }
        private string MakeValidFilename(string origFileName)
        {
            var name = WebUtility.HtmlDecode(origFileName);
            var invalids = Path.GetInvalidFileNameChars();
            var newName = String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return newName;
        }
        private string GetFilenameFromUrl(string url)
        {
            Uri uri = new Uri(url);
            var lastPart = uri.Segments.LastOrDefault();
            if (string.IsNullOrEmpty(lastPart))
                return string.Empty;
            var parts = lastPart.Split(new char[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            return parts[0];
        }
    }
    public class DownloadWrapper : NotificableObject
    {
        private bool complete, hls;
        private string dstFolder, filename;
        private int currentLink, retryCount;
        private float percentage;

        public bool IsComplete { get => complete; set => Set(ref complete, value); }
        public bool IsHLS { get => hls; set => Set(ref hls, value); }
        public string DestinationFolder { get => dstFolder; set => Set(ref dstFolder, value); }
        public string Filename { get => filename; set => Set(ref filename, value); }
        public List<string> Links { get; set; }
        public float CompletePercentage { get => percentage; set => Set(ref percentage, value); }
        public int CurrentLink { get => currentLink; set => Set(ref currentLink, value); }

        public Action OnComplete { get; set; }
        public int Retry { get => retryCount; set => Set(ref retryCount, value); }
    }
}
