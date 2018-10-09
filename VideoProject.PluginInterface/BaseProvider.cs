using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using VideoProject.Plugins;

namespace VideoProject.Plugins
{
    public abstract class BaseProvider
    {
        protected HttpClient httpClient;
        private HttpClientHandler handler;
        private CookieContainer cookieContainer;
        protected string Username { get; private set; }
        protected string Password { get; private set; }
        public abstract IEnumerable<string> Domains { get; }
        public int MaxVideoWidth { get; set; }
        public BaseProvider()
        {
            cookieContainer = new CookieContainer();
            handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                MaxAutomaticRedirections = 2
            };
            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:62.0) Gecko/20100101 Firefox/62.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        }
        protected string MakeValidFolder(string origFileName)
        {
            var name = WebUtility.HtmlDecode(origFileName);
            var invalids = Path.GetInvalidPathChars();
            var newName = String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries));
            return newName;
        }
        protected string MakeValidFilename(string origFileName)
        {
            var name = WebUtility.HtmlDecode(origFileName);
            var invalids = Path.GetInvalidFileNameChars();
            var newName = String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries));
            return newName;
        }
        public virtual void SetLogin(string username, string password)
        {
            Username = username;
            Password = password;
        }
        public virtual void AddCookie(string name, string value)
        {
            foreach(var domain in Domains)
                cookieContainer.Add(new Cookie(name, value, "/", domain));
        }
        public virtual void SetCookie(string cookieString)
        {
            var cookies = cookieString.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var cookie in cookies)
            {
                var cookieValues = cookie.Split(new char[] { '=' });
                AddCookie(cookieValues[0], cookieValues[1]);
            }
        }
        public virtual string GetCookies()
        {
            return cookieContainer.GetCookieHeader(new Uri(Domains.GetEnumerator().Current));
        }
        public virtual List<string> GetLast100Videos(string baseFolder)
        {
            throw new NotImplementedException();
        }
    }
}
