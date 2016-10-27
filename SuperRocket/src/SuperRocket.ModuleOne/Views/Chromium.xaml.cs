using CefSharp;
using CefSharp.Wpf;
using SuperRocket.ModuleOne.ResourceHandler;
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

        const string defaultUrl = "ids://Modules/{0}/Default.html";
        private string homePageUrl = string.Empty;
        public Chromium()
        {
            InitializeComponent();
            InitializeBrowser();
        }

        private void InitializeBrowser()
        {
            var browser = new ChromiumWebBrowser();

            //BrowserSettings browserSettings = new BrowserSettings();
            ////browserSettings.
            //browser.BrowserSettings = browserSettings;



            var handler = browser.ResourceHandlerFactory as DefaultResourceHandlerFactory;
            if (handler != null)
            {
                var moduleName = "Example";//It will be got from the menu click event with module name passed
                homePageUrl = string.Format(defaultUrl, moduleName);
                var path = AppDomain.CurrentDomain.BaseDirectory + string.Format(@"Resource\Modules\{0}\Default.html", moduleName);//The path for the home page of the module
                StreamReader reader = new StreamReader(path, System.Text.Encoding.GetEncoding("utf-8"));
                var responseBody = reader.ReadToEnd().ToString();
                reader.Close();
                var response = CefSharp.ResourceHandler.FromString(responseBody);
                //response.Headers.Add("HeaderTest1", "HeaderTest1Value");
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

            chromiumContainer.Children.Insert(0, browser);
        }
    }
}
