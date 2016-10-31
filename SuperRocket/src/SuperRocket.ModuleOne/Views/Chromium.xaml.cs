using CefSharp;
using CefSharp.Wpf;
using SuperRocket.ModuleOne.ResourceHandler;
using SuperRocket.ModuleOne.Services;
using SuperRocket.ModuleOne.ViewModels;
using System;
using System.IO;
using System.Windows.Controls;

namespace SuperRocket.ModuleOne.Views
{
    /// <summary>
    /// Interaction logic for Chromium.xaml
    /// </summary>
    public partial class Chromium : UserControl
    {
        public Chromium()
        {
            InitializeComponent();
            InitializeBrowser();
        }

       
        private void InitializeBrowser()
        {
            var browser =  new ChromiumWebBrowser();

            chromiumContainer.Children.Insert(0, browser);
        }
    }
}
