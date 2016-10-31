using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp.Wpf;
using CefSharp;
using SuperRocket.ModuleOne.ResourceHandler;
using System.IO;

namespace SuperRocket.ModuleOne.Services
{
    public class BrowserManager : IBrowserManager
    {
        public void CefInitialize()
        {
            if (!Cef.IsInitialized)
            {
                var settings = new CefSettings();
                settings.RemoteDebuggingPort = 8088;
                settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = CefSharpSchemeHandlerFactory.SchemeName,
                    SchemeHandlerFactory = new CefSharpSchemeHandlerFactory()
                    //SchemeHandlerFactory = new InMemorySchemeAndResourceHandlerFactory()
                });
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            }
        }
    }
}
