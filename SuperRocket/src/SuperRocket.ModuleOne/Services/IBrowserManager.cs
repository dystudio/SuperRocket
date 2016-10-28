using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperRocket.ModuleOne.Services
{
    public interface IBrowserManager
    {
        ChromiumWebBrowser CreateBrowser();
    }
}
