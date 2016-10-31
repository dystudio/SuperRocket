// Copyright ?2010-2016 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;

namespace SuperRocket.ModuleOne.ResourceHandler
{
    public class CefSharpSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public const string SchemeName = "local";
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            if (schemeName == SchemeName && request.Url.EndsWith("CefSharp.Core.xml", System.StringComparison.OrdinalIgnoreCase))
            {
                //Convenient helper method to lookup the mimeType
                var mimeType = CefSharp.ResourceHandler.GetMimeType(".xml");
                //Load a resource handler for CefSharp.Core.xml
                //mimeType is optional and will default to text/html
                return CefSharp.ResourceHandler.FromFilePath("CefSharp.Core.xml", mimeType);
            }
            return new CefSharpLocalResourceSchemeHandler();
        }
    }
}