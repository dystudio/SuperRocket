using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Orchard.Accenture.Event.Services
{
    public interface IDelegationService : IDependency
    {
        string Process(string originalOwner, string owner);

        bool CheckIfCurrentUserInAdminRole();
    }
}
