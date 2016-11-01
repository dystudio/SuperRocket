using Orchard.Accenture.Event.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchard.Accenture.Event.Interfaces
{
    public interface IInfoCardService : IDependency
    {
        InfoCardPartRecord Get();
        int Update(InfoCardPartRecord model);
    }
}
