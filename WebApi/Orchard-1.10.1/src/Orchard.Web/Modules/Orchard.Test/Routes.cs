using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.Mvc.Routes;

namespace Orchard.Test
{
    public class Routes : IRouteProvider
    {
        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[]
            {
                new RouteDescriptor
                {
                    Route = new Route("Orchard.Test/{controller}/{action}/{id}",
                        new RouteValueDictionary
                        {
                            {"area", "Orchard.Test"},
                            {"controller", "Home"},
                            {"action", "Index"},
                            {"id", UrlParameter.Optional}
                        }, new RouteValueDictionary(),
                        new RouteValueDictionary {{"area", "Orchard.Test"}},
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