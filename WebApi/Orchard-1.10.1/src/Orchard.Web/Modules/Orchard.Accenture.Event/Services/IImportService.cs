using Orchard.Accenture.Event.Models;
using Orchard.Security;
using System.Collections.Generic;
using System.Web;

namespace Orchard.Accenture.Event.Services
{
    public interface IImportService : IDependency
    {
        dynamic DeleteParticipant(int id, HttpPostedFileBase file);
        dynamic ImportParticipant(int id, HttpPostedFileBase file, IUser owner);
        dynamic ImportSessionFile(int id, HttpPostedFileBase file, IUser owner);
        dynamic GetEvents();
        IUser GetEventOwner(int id);
        dynamic GetParticipants(int eventId);
        dynamic BulkImportParticipant(int id, HttpPostedFileBase file, IUser owner);
    }
}