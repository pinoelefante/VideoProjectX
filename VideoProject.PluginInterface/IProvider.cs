using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoProject.PluginInterface;

namespace VideoProject.Plugins
{
    public interface IProvider
    {
        Task<List<LinkData>> GetLinkAsync(string url);
        Task<(List<string> DirectLinks, string FolderDestination, string GalleryName)> GetGalleryLinksAsync(string url);
        bool IsGallery(string url);
        bool IsImageProvider { get; }
        bool IsVideoProvider { get; }
        LinkType VideoLinkType { get; }
        IEnumerable<string> Domains { get; }
        string ProviderName { get; }
        List<string> GetLast100Videos(string baseFolder);
        bool HasLogin { get; }
        void Login();
        bool IsLogged();
    }
    public enum LinkType
    {
        DIRECT,
        HLS,
        REDIRECT
    }
}
