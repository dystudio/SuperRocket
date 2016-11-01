using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using System.Collections;

namespace Orchard.Accenture.Event.Services
{
    public interface IPeopleService : IDependency
    {
        dynamic GetProfile(string eid);
        dynamic LoadProfile(string eid);
        dynamic GetBulkProfile(string[] eids);
        dynamic LoadProfilesFromMRDR(string eid);
        dynamic LoadAvatar(string eid);
        dynamic LoadOriginalProfile(string eid);
    }
}