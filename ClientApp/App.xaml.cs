using CefSharp;
using CefSharp.Example;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ClientApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //Perform dependency check to make sure all relevant resources are in our output directory.
            var settings = new CefSettings();


            settings.RemoteDebuggingPort = 8088;

            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "ids",
                SchemeHandlerFactory = new CefSharpSchemeHandlerFactory()
                //SchemeHandlerFactory = new InMemorySchemeAndResourceHandlerFactory()
            });

            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            
        }
    }
}
