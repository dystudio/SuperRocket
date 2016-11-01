using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using System.Collections;
using Orchard.Accenture.Event.Models;

namespace Orchard.Accenture.Event.Services
{
    public interface IAppService : IDependency
    {
        AppPartRecord GetApp(string clientId);
    }
}