using System;
using System.Collections.Generic;
using System.Text;
using VideoProject.Plugins;

namespace VideoProject.PluginInterface
{
    public class LinkData
    {
        public string Link { get; set; }
        public string FolderDestination { get; set; }
        public string Filename { get; set; }
        public Plugins.LinkType TypeLink { get; set; }

        public static LinkData GetEmpty()
        {
            return new LinkData() { Link = string.Empty, FolderDestination = string.Empty, Filename = string.Empty, TypeLink = LinkType.DIRECT };
        }

        public static List<LinkData> GetEmptyList()
        {
            return new List<LinkData>() { GetEmpty() };
        }

        public static List<LinkData> CreateAsList(string link, string folder, string filename, LinkType linkType)
        {
            return new List<LinkData>()
            {
                new LinkData()
                {
                    Link = link,
                    FolderDestination = folder,
                    Filename = filename,
                    TypeLink = linkType
                }
            };
        }

    }
}
