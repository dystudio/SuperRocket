
using Orchard.Accenture.Event.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using Orchard.Data;
using Orchard.Logging;
using Orchard.Accenture.Event.Services;
using System.Web.Mvc;
using Orchard.Mvc.Filters;
using JetBrains.Annotations;
using Orchard.WebApi.Filters;

namespace Orchard.Accenture.Event.Extension
{
    [UsedImplicitly]
    public class AuthorizeAppFilter : FilterProvider, IAuthorizationFilter, IApiFilterProvider
    {
        private readonly IAppService _service;

        public AuthorizeAppFilter(
            IAppService service
            )
        {

            _service = service;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        private static AuthorizeAppAttribute GetAuthorizeAppAttribute(ActionDescriptor descriptor)
        {

            //return descriptor.GetCustomAttributes(typeof(AuthorizeAppAttribute), true).FirstOrDefault()  as AuthorizeAppAttribute;

            return descriptor.GetCustomAttributes(typeof(AuthorizeAppAttribute), true)
                .Concat(descriptor.ControllerDescriptor.GetCustomAttributes(typeof(AuthorizeAppAttribute), true))
                .OfType<AuthorizeAppAttribute>()
                .FirstOrDefault();
        }

        public void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            var attribute = GetAuthorizeAppAttribute(filterContext.ActionDescriptor);
            if (attribute != null)
            {
                var context = filterContext.HttpContext;

                string[] values = context.Request.Headers.GetValues("ClientId");

                if (values != null && values.Count() > 0)
                {
                    string clientId = values.FirstOrDefault();

                    var app = _service.GetApp(clientId);
                    if (app == null)
                    {
                        filterContext.Result = new HttpUnauthorizedResult();
                    }
                } 
            }
        }

    }

}