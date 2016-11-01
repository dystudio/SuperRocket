using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using System.Collections;

namespace Orchard.Accenture.Event.Services
{
    public interface IGoogleMapService : IDependency
    {
        dynamic LoadGoogleMap(string location);
    }
}