using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoProjectX.Services
{
    public class SettingsManager : ObservableObject
    {
        private int videoWidth = 1080;

        public bool UseMaxQuality { get; set; } = true;
        public int MaxVideoWidth
        {
            get => videoWidth;
            set
            {
                Set(ref videoWidth, value);
                OnMaxVideoWidthChanged?.Invoke(this, value);
            }
        }
        public string DownloadFolder { get; set; } = @"D:\VideoProjectX";

        public event EventHandler<int> OnMaxVideoWidthChanged;
    }
}
