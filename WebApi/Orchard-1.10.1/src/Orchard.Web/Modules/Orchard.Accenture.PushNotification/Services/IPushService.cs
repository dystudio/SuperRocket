using Orchard.Accenture.Event.Models;
using Orchard.Accenture.PushNotification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchard.Accenture.PushNotification.Services
{
    public interface IPushService : IDependency
    {
        void Notify(NotificationMessage message);
        void Notify(NotificationMessage message,string tenant,string topic);
        void RawNotify(string location, RawNotificationMessage message);
        IOrderedEnumerable<ContentManagement.ContentItem> GetEvents();
        List<String> CheckParticipants(List<String> SelectedList, int eventId);
        String GetPeopleKeys(string Participants);
        List<String> GetSessionADGroups(int EventId, int SessionId);
        List<String> GetCirclesADGroups(int EventId, int CircleId);
        List<String> GetMemberOfADGroup(string ADgroup);
        List<String> GetEventADGroups(int EventId);
        List<String> GetEventParticipants(int EventId);
        dynamic GetCircles(int EventId);
        dynamic GetSessions(int EventId);
        
        //IEnumerable<ContentManagement.ContentItem> GetSessions(int eventId);        
    }
}
