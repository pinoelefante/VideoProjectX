using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using VideoProjectX.Services;
using Xamarin.Forms;

namespace VideoProjectX.ViewModels
{
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            SimpleIoc.Default.Register<ProvidersManager>();
            SimpleIoc.Default.Register<SettingsManager>();
            SimpleIoc.Default.Register<DownloadsManager>();

            SimpleIoc.Default.Register<MainPageViewModel>();
            SimpleIoc.Default.Register<SettingsPageViewModel>();
        }
        public static object GetViewModel(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                try
                {
                    return SimpleIoc.Default.GetInstance(type);
                }
                catch
                {
                    Debug.WriteLine($"{typeName} - viewmodel not registered");
                    return null;
                }
            }
            else
                Debug.WriteLine($"{typeName} - type not found");
            return null;
        }
        public static T GetService<T>() => SimpleIoc.Default.GetInstance<T>();
    }
    public class ViewModelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var assemblyPath = $"VideoProjectX.ViewModels.{parameter}";
            return ViewModelLocator.GetViewModel(assemblyPath);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
