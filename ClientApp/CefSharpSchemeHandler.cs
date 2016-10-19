// Copyright © 2010-2016 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ClientApp.Properties;

namespace CefSharp.Example
{
    internal class CefSharpSchemeHandler : IResourceHandler
    {
        private static readonly IDictionary<string, string> ResourceDictionary;

        private string mimeType;
        private MemoryStream stream;
        
        static CefSharpSchemeHandler()
        {
            ResourceDictionary = new Dictionary<string, string>
            {
                { "/Example/Default.html", Resources.Default },

                { "/Example/css/animate.min.css", Resources.animate_min },
                { "/Example/css/bootstrap.min.css", Resources.animate_min },
                { "/Example/css/component.css", Resources.component },
                { "/Example/css/font-awesome.min.css", Resources.font_awesome_min },
                { "/Example/css/owl.carousel.css", Resources.owl_carousel },
                { "/Example/css/owl.theme.css", Resources.owl_theme },
                { "/Example/css/style.css", Resources.style },
                { "/Example/css/vegas.min.css", Resources.vegas_min },

                { "/Example/js/bootstrap.min.js", Resources.bootstrap_min },
                { "/Example/js/custom.js", Resources.custom },
                { "/Example/js/jquery.js", Resources.jquery },
                { "/Example/js/modernizr.custom.js", Resources.modernizr_custom },
                { "/Example/js/owl.carousel.min.js", Resources.owl_carousel_min },
                { "/Example/js/smoothscroll.js", Resources.smoothscroll },
                { "/Example/js/toucheffects.js", Resources.toucheffects },
                { "/Example/js/vegas.min.js", Resources.vegas_min },
                { "/Example/js/wow.min.js", Resources.wow_min }


            };
        }

        bool IResourceHandler.ProcessRequest(IRequest request, ICallback callback)
        {
            // The 'host' portion is entirely ignored by this scheme handler.
            var uri = new Uri(request.Url);
            var fileName = uri.AbsolutePath;

            string resource;
            var fileExtension = Path.GetExtension(fileName);
            if (ResourceDictionary.TryGetValue(fileName, out resource) && !string.IsNullOrEmpty(resource))
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        var bytes = Encoding.UTF8.GetBytes(resource);
                        stream = new MemoryStream(bytes);

                        //var fileExtension = Path.GetExtension(fileName);
                        mimeType = ResourceHandler.GetMimeType(fileExtension);

                        callback.Continue();
                    }
                });

                return true;
            }
            

            if (fileExtension == ".jpg")
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        var path = AppDomain.CurrentDomain.BaseDirectory + string.Format(@"Modules/{0}", fileName);//The path for the home page of the module
                        FileStream fs = File.OpenRead(path);
                        int filelength = 0;
                        filelength = (int)fs.Length;
                        Byte[] bytes = new Byte[filelength];
                        fs.Read(bytes, 0, filelength);

                        stream = new MemoryStream(bytes);

                        mimeType = ResourceHandler.GetMimeType(fileExtension);
                        callback.Continue();
                    }
                });
                return true;
            }
            else
            {
                callback.Dispose();
            }
            //if (!string.IsNullOrEmpty(fileName))
            //{
            //    var path = AppDomain.CurrentDomain.BaseDirectory + string.Format(@"Modules/{0}", fileName);//The path for the home page of the module

            //    //Read local file to send to client
            //    Task.Run(() =>
            //    {

            //            StreamReader reader = new StreamReader(path, System.Text.Encoding.GetEncoding("utf-8"));
            //            var responseBody = reader.ReadToEnd().ToString();

            //            var bytes = Encoding.UTF8.GetBytes(responseBody);
            //            stream = new MemoryStream(bytes);

            //            var fileExtension = Path.GetExtension(fileName);
            //            mimeType = ResourceHandler.GetMimeType(fileExtension);

            //    });

            //    return true;
            //}

            return false;
        }
        

        void IResourceHandler.GetResponseHeaders(IResponse response, out long responseLength, out string redirectUrl)
        {
            responseLength = stream == null ? 0 : stream.Length;
            redirectUrl = null;

            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusText = "OK";
            response.MimeType = mimeType;
        }

        bool IResourceHandler.ReadResponse(Stream dataOut, out int bytesRead, ICallback callback)
        {
            //Dispose the callback as it's an unmanaged resource, we don't need it in this case
            callback.Dispose();

            if(stream == null)
            {
                bytesRead = 0;
                return false;
            }

            //Data out represents an underlying buffer (typically 32kb in size).
            var buffer = new byte[dataOut.Length];
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            
            dataOut.Write(buffer, 0, buffer.Length);

            return bytesRead > 0;
        }

        bool IResourceHandler.CanGetCookie(Cookie cookie)
        {
            return true;
        }

        bool IResourceHandler.CanSetCookie(Cookie cookie)
        {
            return true;
        }

        void IResourceHandler.Cancel()
        {
            
        }

        void IDisposable.Dispose()
        {
            
        }
    }
}
