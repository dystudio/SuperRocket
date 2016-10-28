using CefSharp;
using CefSharp.Wpf;
using SuperRocket.ModuleOne.ResourceHandler;
using SuperRocket.ModuleOne.Services;
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
        private IBrowserManager _manager;
        public Chromium()
        {
            InitializeComponent();
            InitializeBrowser();
        }

        private void InitializeBrowser()
        {
            var browser = _manager.CreateBrowser();
            chromiumContainer.Children.Insert(0, browser);
        }
    }
}
