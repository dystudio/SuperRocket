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
        //0: scheme name 1: module name 2: default page name
        const string defaultHomePageName = "default.html";
        const string defaultUrlTemplate = "{0}://Resource/Modules/{1}/{2}";
        
        private string homePageUrl = string.Empty;
        public ChromiumWebBrowser CreateBrowser()
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
            var browser = new ChromiumWebBrowser();
            var handler = browser.ResourceHandlerFactory as DefaultResourceHandlerFactory;
            if (handler != null)
            {
                var moduleName = "Example";//It will be got from the menu click event with module name passed
                homePageUrl = string.Format(defaultUrlTemplate, CefSharpSchemeHandlerFactory.SchemeName, moduleName, defaultHomePageName);
                var defaultHomePageAbsolutePath = AppDomain.CurrentDomain.BaseDirectory + string.Format(@"Resource\Modules\{0}\{1}", moduleName,defaultHomePageName);//The path for the home page of the module
                StreamReader reader = new StreamReader(defaultHomePageAbsolutePath, System.Text.Encoding.GetEncoding("utf-8"));
                var responseBody = reader.ReadToEnd().ToString();
                reader.Close();
                var response = CefSharp.ResourceHandler.FromString(responseBody);
                handler.RegisterHandler(homePageUrl, response);
            }

            browser.LoadError += (sender, args) =>
            {
                // Don't display an error for downloaded files.
                if (args.ErrorCode == CefErrorCode.Aborted)
                {
                    return;
                }

                // Don't display an error for external protocols that we allow the OS to
                // handle. See OnProtocolExecution().
                //if (args.ErrorCode == CefErrorCode.UnknownUrlScheme)
                //{
                //	var url = args.Frame.Url;
                //	if (url.StartsWith("spotify:"))
                //	{
                //		return;
                //	}
                //}

                // Display a load error message.
                var errorBody = string.Format("<html><body bgcolor=\"white\"><h2>Failed to load URL {0} with error {1} ({2}).</h2></body></html>",
                                              args.FailedUrl, args.ErrorText, args.ErrorCode);

                args.Frame.LoadStringForUrl(errorBody, args.FailedUrl);
            };
            browser.RequestHandler = new RequestHandler();
            browser.MenuHandler = new MenuHandler();
            browser.Address = homePageUrl;
            return browser;
        }
    }
}
