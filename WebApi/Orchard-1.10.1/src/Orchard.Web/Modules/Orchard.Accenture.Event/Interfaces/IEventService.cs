using Orchard.Accenture.Event.Models;
using Orchard.Accenture.Event.ServiceModels;
using Orchard.Accenture.Event.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchard.Accenture.Event.Interfaces
{
    public interface IEventService : IDependency
    {
        IEnumerable<EventModel> GetEventList(EventSearch search = null, EventSort sort = null, bool allowCache = true);

        EventModel GetEvent(int id, bool allowCache = true);

        int Delete(int id);

        int Create(EventModel model);

        int Update(EventModel model);

        void TriggerSignal();
    }
}
