using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using VideoProject.Plugins;
using VideoProjectX.Services;
using Xamarin.Forms;

namespace VideoProjectX.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private DownloadsManager downloadManager;
        private ProvidersManager providers;
        public ObservableCollection<DownloadWrapper> DownloadList { get; } = new ObservableCollection<DownloadWrapper>();
        public string LogText { get; private set; }
        public MainPageViewModel(DownloadsManager down, ProvidersManager p)
        {
            downloadManager = down;
            downloadManager.ListDownloads.CollectionChanged += ListDownloads_CollectionChanged;
            providers = p;

            //MyTraceListener myTrace = new MyTraceListener();
            //myTrace.TextChanged += MyTrace_TextChanged;
            //Trace.Listeners.Add(myTrace);
        }

        private void MyTrace_TextChanged(object sender, string e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (string.IsNullOrEmpty(e))
                    return;
                LogText = $"[{DateTime.Now.ToShortTimeString()}] {e}{LogText}";
                RaisePropertyChanged(() => LogText);
            });
        }

        private void ListDownloads_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (var item in e.NewItems)
                            DownloadList.Add(item as DownloadWrapper);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        foreach (var item in e.OldItems)
                            DownloadList.Remove(item as DownloadWrapper);
                        break;
                }
            });
        }

        private RelayCommand<string> downloadCommand;
        public RelayCommand<string> DownloadCommand =>
            downloadCommand ??
            (downloadCommand = new RelayCommand<string>(async (url) =>
            {
                if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    Debug.WriteLine($"Url non valido: {url}");
                    return;
                }
                var provider = providers.GetProvider(url);
                if (provider == null)
                {
                    Debug.WriteLine($"Provider per {url} non disponibile");
                    return;
                }
                if(provider.IsGallery(url))
                {
                    try
                    {
                        var content = await provider.GetGalleryLinksAsync(url);
                        if (content.DirectLinks != null && content.DirectLinks.Count > 0)
                            downloadManager.AddDownload(content.DirectLinks, content.FolderDestination, content.GalleryName);
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
                else
                {
                    try
                    {
                        var contents = await provider.GetLinkAsync(url);
                        if (contents == null || contents.Count == 0)
                        {
                            Debug.WriteLine("broken link");
                            return;
                        }
                        /*
                        if(content.TypeLink == LinkType.REDIRECT)
                        {
                            DownloadCommand.Execute(content.DirectLink);
                            return;
                        }
                        */
                        downloadManager.AddDownload(contents);
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }));
        private RelayCommand removeCompletedCmd;
        public RelayCommand RemoveDownloadCompletedCommand =>
            removeCompletedCmd ??
            (removeCompletedCmd = new RelayCommand(() =>
            {
                for(int i=0;i<downloadManager.ListDownloads.Count;)
                {
                    if (downloadManager.ListDownloads[i] != null && downloadManager.ListDownloads[i].IsComplete)
                        downloadManager.ListDownloads.RemoveAt(i);
                    else
                        i++;
                }
            }));
    }
    public class MyTraceListener : TraceListener
    {
        public event EventHandler<string> TextChanged;
        public override void Write(string message)
        {
            TextChanged?.Invoke(this, message);
        }

        public override void WriteLine(string message)
        {
            TextChanged?.Invoke(this, message+"\n");
        }
    }
}
