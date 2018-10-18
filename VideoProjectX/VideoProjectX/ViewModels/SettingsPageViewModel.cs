using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;
using VideoProjectX.Services;

namespace VideoProjectX.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        private string dwnDirectory, maxWidth;
        private bool alwaysMax;
        private RelayCommand saveCmd;
        private SettingsManager settingsManager;
        public string DownloadDirectory { get => dwnDirectory; set => Set(ref dwnDirectory, value); }
        public bool DownloadMaxVideoQuality { get => alwaysMax; set => Set(ref alwaysMax, value); }
        public string MaxVideoWidth { get => maxWidth; set => Set(ref maxWidth, value); }

        public SettingsPageViewModel(SettingsManager s)
        {
            settingsManager = s;
            DownloadDirectory = s.DownloadFolder;
            DownloadMaxVideoQuality = s.UseMaxQuality;
            MaxVideoWidth = s.MaxVideoWidth.ToString();
        }

        public RelayCommand SaveSettingsCommand =>
            saveCmd ??
            (saveCmd = new RelayCommand(() =>
            {
                settingsManager.DownloadFolder = DownloadDirectory;
                settingsManager.MaxVideoWidth = int.Parse(MaxVideoWidth);
                settingsManager.UseMaxQuality = DownloadMaxVideoQuality;
                settingsManager.SaveSettings();
            }));
    }
}
