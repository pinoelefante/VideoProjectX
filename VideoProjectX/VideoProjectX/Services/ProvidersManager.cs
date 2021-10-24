using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using VideoProject.Plugins;
using System.Linq;
using System.Threading.Tasks;
using VideoProject.PluginInterface;

namespace VideoProjectX.Services
{
    public class ProvidersManager
    {
        private IProvider directProvider;
        private List<IProvider> providers_list;
        private static bool loaded = false;
        private SettingsManager settingsManager;
        public ProvidersManager(SettingsManager settings)
        {
            providers_list = new List<IProvider>();
            directProvider = new DirectLinkProvider();
            settingsManager = settings;
            settingsManager.OnMaxVideoWidthChanged += Settings_OnMaxVideoWidthChanged;
            LoadAssemblies();
        }

        private void Settings_OnMaxVideoWidthChanged(object sender, int newWidth)
        {
            foreach (var provider in providers_list)
            {
                if(provider is BaseProvider)
                    (provider as BaseProvider).MaxVideoWidth = settingsManager.UseMaxQuality ? 2160 : newWidth;
            }
        }

        private void LoadAssemblies()
        {
            if (loaded)
                return;
            loaded = true;
            var pluginPath = Path.Combine(Environment.CurrentDirectory, "plugins");
            if (!Directory.Exists(pluginPath))
                Directory.CreateDirectory(pluginPath);
            var files = Directory.GetFiles(pluginPath).Where(x => (Path.GetFileName(x).ToLower().StartsWith("videoproject.plugin.") || Path.GetFileName(x).ToLower().StartsWith("videoproject.plugins.") || Path.GetFileName(x).ToLower().StartsWith("plugin.")) && Path.GetFileName(x).ToLower().EndsWith(".dll")).ToList();
            foreach (var fileDll in files)
            {
                var dll = Assembly.LoadFile(fileDll);
                foreach (Type type in dll.ExportedTypes)
                {
                    if (type.GetInterface(nameof(IProvider)) != null)
                    {
                        var provider = Activator.CreateInstance(type) as IProvider;
                        providers_list.Add(provider);
                        if (provider is BaseProvider)
                        {
                            Debug.WriteLine("Provider is BaseProvider");
                            (provider as BaseProvider).MaxVideoWidth = settingsManager.UseMaxQuality ? 2160 : settingsManager.MaxVideoWidth;
                        }
                    }
                }
            }
            Debug.Write($"Plugins loaded: {providers_list.Count} [");
            foreach (var x in providers_list)
            {
                Debug.Write($"{x.ProviderName} ");
            }
            Debug.WriteLine($"]");
        }
        public IProvider GetProvider(string url)
        {
            var uri = new Uri(url);
            var domain = uri.Host;
            foreach(var provider in providers_list)
            {
                if (provider.Domains.Contains(domain))
                    return provider;
            }
            return directProvider;
        }
    }
    
    public class DirectLinkProvider : IProvider
    {
        public bool IsImageProvider => false;
        public bool IsVideoProvider => false;
        public LinkType VideoLinkType => LinkType.DIRECT;
        public IEnumerable<string> Domains => new List<string>(1);
        public string ProviderName => "direct-link";
        public bool HasLogin => false;
        public async Task<List<LinkData>> GetLinkAsync(string url)
        {
            await Task.CompletedTask;
            var filename = url.Substring(url.LastIndexOf("/") + 1);
            var fileParts = filename.Split(new char[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            // return (url, "", fileParts[0], LinkType.DIRECT);
            return LinkData.CreateAsList(url, "", fileParts[0], LinkType.DIRECT);
        }

        public async Task<(List<string> DirectLinks, string FolderDestination, string GalleryName)> GetGalleryLinksAsync(string url)
        {
            await Task.CompletedTask;
            return (null, string.Empty, string.Empty);
        }
        public List<string> GetLast100Videos(string baseFolder) => null;

        public bool IsGallery(string url) => false;
        public bool IsLogged() => false;
        public void Login() { }
        public void SetMaxVideoWidth(int width) { }
    }
}
