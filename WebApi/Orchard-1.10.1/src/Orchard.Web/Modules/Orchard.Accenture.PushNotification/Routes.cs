using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.Mvc.Routes;

namespace Orchard.Accenture.PushNotification
{
    public class Routes : IRouteProvider
    {
        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[]
            {
                new RouteDescriptor
                {
                    Route = new Route("Orchard.Accenture.PushNotification/{controller}/{action}/{id}",
                        new RouteValueDictionary
                        {
                            {"area", "Orchard.Accenture.PushNotification"},
                            {"controller", "Home"},
                            {"action", "Index"},
                            {"id", UrlParameter.Optional}
                        }, new RouteValueDictionary(),
                        new RouteValueDictionary {{"area", "Orchard.Accenture.PushNotification" } },
                        new MvcRouteHandler())
                },
                new RouteDescriptor
                {
                    Route = new Route("Orchard.Accenture.PushNotification/{controller}/{action}/{id}",
                        new RouteValueDictionary
                        {
                            {"area", "Orchard.Accenture.PushNotification"},
                            {"controller", "TestAdmin"},
                            {"action", "Test"},
                            {"id", UrlParameter.Optional}
                        }, new RouteValueDictionary(),
                        new RouteValueDictionary {{"area", "Orchard.Accenture.PushNotification" } },
                        new MvcRouteHandler())
                }
            };
        }

        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            foreach (RouteDescriptor route in GetRoutes())
            {
                routes.Add(route);
            }
        }
    }
}