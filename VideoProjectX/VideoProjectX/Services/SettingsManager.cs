using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VideoProjectX.Services
{
    public class SettingsManager : ObservableObject
    {
        public bool UseMaxQuality { get; set; }
        public int MaxVideoWidth { get; set; }
        public string DownloadFolder { get; set; }
        public event EventHandler<int> OnMaxVideoWidthChanged;

        public SettingsManager()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (!File.Exists("settings.json"))
            {
                UseMaxQuality = true;
                MaxVideoWidth = 1080;
                DownloadFolder = Path.Combine(Environment.CurrentDirectory, "Downloads");
                return;
            }
            var options = File.ReadAllText("settings.json");
            var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(options);
            foreach(var kv in settings)
            {
                switch(kv.Key)
                {
                    case nameof(UseMaxQuality):
                        UseMaxQuality = bool.Parse(kv.Value);
                        break;
                    case nameof(MaxVideoWidth):
                        MaxVideoWidth = int.Parse(kv.Value);
                        break;
                    case nameof(DownloadFolder):
                        DownloadFolder = kv.Value;
                        break;
                }
            }
        }
        public void SaveSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings.Add(nameof(UseMaxQuality), UseMaxQuality.ToString());
            settings.Add(nameof(MaxVideoWidth), MaxVideoWidth.ToString());
            settings.Add(nameof(DownloadFolder), DownloadFolder);
            var options = JsonConvert.SerializeObject(settings);
            File.WriteAllText("settings.json", options);

            OnMaxVideoWidthChanged?.Invoke(this, MaxVideoWidth);
        }
    }
}
