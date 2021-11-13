using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VideoProject.Parsers
{
    public class M3UParser
    {
        protected HttpClient httpClient;

        public M3UParser()
        {
            httpClient = new HttpClient();
        }
        public async Task<List<M3UMediaLink>> ParsePlaylistAsync(string url)
        {
            var baseUrl = url.Substring(0, url.LastIndexOf("/") + 1);
            List<M3UMediaLink> links = new List<M3UMediaLink>(5);

            var playlistContent = await GetContentAsync(url);
            if (string.IsNullOrEmpty(playlistContent) || !IsM3U(playlistContent))
                return links;

            var lines = playlistContent.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            M3UMediaLink lastLink = null;
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.StartsWith("#EXTINF"))
                {
                    lastLink = new M3UMediaLink();
                    var values = line.Replace("#EXTINF:", "").Split(new char[] { ',' });
                    if (values.Length >= 1 && float.TryParse(values[0], out float outVal))
                        lastLink.Duration = outVal;
                    if (values.Length >= 2)
                        lastLink.Name = values[1];
                }
                else if (line.StartsWith("#EXT-X-ENDLIST"))
                    break;
                else
                {
                    if (line.StartsWith("#"))
                        continue;
                    lastLink.Link = line.ToLower().StartsWith("http") ? line : (baseUrl + line);
                    links.Add(lastLink);
                }
            }
            links.TrimExcess();
            return links;
        }
        protected bool IsM3U(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;
            return content.StartsWith("#EXTM3U");
        }
        protected async Task<string> GetContentAsync(string url)
        {
            try
            {
                var content = await httpClient.GetStringAsync(url);
                return content;
            } catch (Exception e)
            {
                return null;
            }
        }
    }
    public class M3UMediaLink
    {
        public float Duration { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
    }
}
