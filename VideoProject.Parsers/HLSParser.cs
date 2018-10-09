using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VideoProject.Parsers
{
    public class HLSParser : M3UParser
    {
        public HLSParser() : base() { }

        public async Task<M3UMasterPlaylist> Parse(string url)
        {
            var m3uContent = await GetContentAsync(url);
            if (!IsM3U(m3uContent))
                return null;
            var baseUrl = url.Substring(0, url.LastIndexOf("/") + 1);
            var mPlaylist = ParseMasterPlaylist(m3uContent, baseUrl);
            foreach(var playlist in mPlaylist.Playlists)
            {
                if(playlist.AudioList.Any())
                    foreach(var aPlaylist in playlist.AudioList)
                        aPlaylist.Links.AddRange(await ParsePlaylistAsync(aPlaylist.URL));

                if (playlist.SubtitlesList.Any())
                    foreach (var sPlaylist in playlist.SubtitlesList)
                        sPlaylist.Links.AddRange(await ParsePlaylistAsync(sPlaylist.URL));

                if (!string.IsNullOrEmpty(playlist.VideoPlaylistLink))
                    playlist.VideoLinks.AddRange(await ParsePlaylistAsync(playlist.VideoPlaylistLink));
            }
            return mPlaylist;
        }
        private M3UMasterPlaylist ParseMasterPlaylist(string content, string baseUrl)
        {
            M3UMasterPlaylist masterPlaylist = new M3UMasterPlaylist();
            Dictionary<string, List<M3UMediaItem>> groups = new Dictionary<string, List<M3UMediaItem>>();
            var lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i<lines.Length;i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                if(line.StartsWith("#EXT-X-MEDIA"))
                {
                    line = line.Replace("#EXT-X-MEDIA:", "");
                    var values = line.Split(new char[] { ',' });
                    var groupItem = new M3UMediaItem();
                    var groupId = "Default";
                    foreach(var value in values)
                    {
                        var kv = value.Split(new char[] { '=' });
                        kv[1] = kv[1].Replace("\"", "");
                        switch(kv[0])
                        {
                            case "TYPE":
                                groupItem.MediaType = kv[1] == "SUBTITLES" ? M3UMediaType.SUBTITLE : M3UMediaType.AUDIO;
                                break;
                            case "GROUP-ID":
                                groupId = kv[1];
                                groupItem.GroupId = kv[1];
                                break;
                            case "NAME":
                                groupItem.Name = kv[1];
                                break;
                            case "DEFAULT":
                                groupItem.Default = kv[1] == "YES";
                                break;
                            case "FORCED":
                                groupItem.Forced = kv[1] == "YES";
                                break;
                            case "LANGUAGE":
                                groupItem.Language = kv[1];
                                break;
                            case "URI":
                                groupItem.URL = kv[1];
                                break;
                        }
                    }
                }
                else if(line.StartsWith("#EXT-X-STREAM-INF"))
                {
                    line = line.Replace("#EXT-X-STREAM-INF:", "");
                    var values = line.Split(new char[] { ',' });
                    M3UPlaylist playlist = new M3UPlaylist();
                    foreach(var value in values)
                    {
                        var kv = value.Split(new char[] { '=' });
                        kv[1] = kv[1].Replace("\"", "");
                        switch (kv[0])
                        {
                            case "BANDWIDTH":
                                playlist.Bandwidth = long.Parse(kv[1]);
                                break;
                            case "CODECS":
                                playlist.Codecs = kv[1];
                                break;
                            case "RESOLUTION":
                                var res = kv[1].Split(new char[] { 'x' });
                                playlist.ResolutionHeight = int.Parse(res[1]);
                                playlist.ResolutionWidth = int.Parse(res[0]);
                                break;
                            case "AUDIO":
                                playlist.AudioGroup = kv[1];
                                break;
                            case "SUBTITLES":
                                playlist.SubtitlesGroup = kv[1];
                                break;
                        }
                    }
                    masterPlaylist.Playlists.Add(playlist);
                }
                else
                {
                    if (line.StartsWith("#"))
                        continue;
                    var url = line;
                    if (!line.ToLower().StartsWith("http"))
                        url = baseUrl + line;
                    masterPlaylist.Playlists.Last().VideoPlaylistLink = url;
                }
            }
            foreach(var playlist in masterPlaylist.Playlists)
            {
                if(!string.IsNullOrEmpty(playlist.AudioGroup) && groups.ContainsKey(playlist.AudioGroup))
                {
                    playlist.AudioList.AddRange(groups[playlist.AudioGroup].Where(x => x.MediaType == M3UMediaType.AUDIO).ToList());
                }
                if(!string.IsNullOrEmpty(playlist.SubtitlesGroup) && groups.ContainsKey(playlist.SubtitlesGroup))
                {
                    playlist.SubtitlesList.AddRange(groups[playlist.SubtitlesGroup].Where(x => x.MediaType == M3UMediaType.SUBTITLE).ToList());
                }
            }
            return masterPlaylist;
        }
    }
    public class M3UMasterPlaylist
    {
        public List<M3UPlaylist> Playlists { get; } = new List<M3UPlaylist>();
    }
    public class M3UPlaylist
    {
        public string Name { get; set; }
        public long Bandwidth { get; set; }
        public long AverageBandwidth { get; set; }
        public string Codecs { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public float FrameRate { get; set; }
        public string AudioGroup { get; set; }
        public string SubtitlesGroup { get; set; }
        public string VideoPlaylistLink { get; set; }

        public List<M3UMediaLink> VideoLinks { get; } = new List<M3UMediaLink>(50);
        public List<M3UMediaItem> AudioList { get; } = new List<M3UMediaItem>();
        public List<M3UMediaItem> SubtitlesList { get; } = new List<M3UMediaItem>();
    }
    public class M3UMedia
    {
        public string GroupId { get; set; } = "Default";
        public List<M3UMediaItem> Items { get; } = new List<M3UMediaItem>(10);
    }
    public class M3UMediaItem
    {
        public M3UMediaType MediaType { get; set; }
        public string Name { get; set; }
        public bool Default { get; set; }
        public bool Forced { get; set; }
        public string Language { get; set; }
        public string URL { get; set; }
        public string GroupId { get; set; }

        public List<M3UMediaLink> Links { get; } = new List<M3UMediaLink>();
    }
    public enum M3UMediaType
    {
        AUDIO,
        SUBTITLE
    }
    public enum HDCPLevel
    {
        NONE,
        TYPE_0
    }
}
