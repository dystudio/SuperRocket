using Orchard.ContentManagement;
using System.Collections.Generic;

namespace Orchard.Accenture.PushNotification.ViewModels
{
    public class NotificationMessageViewModel
    {
        public IEnumerable<ContentItem> Sessions { get; set; }
        public IEnumerable<ContentItem> Events { get; set; }
        public IEnumerable<ContentItem> Circles { get; set; }
        public List<System.String> EventADGroups { get; set; }
        public List<System.String> EventADGroupMembers { get; set; }
        public List<System.String> SessionADGroups { get; set; }
        public List<System.String> SessionADGroupMembers { get; set; }
        public List<System.String> CircleADGroups { get; set; }
        public List<System.String> CircleADGroupMembers { get; set; }
        public List<System.String> EventParticipants { get; set; }
        public List<System.String> SingleParticipants { get; set; }
        public List<System.String> SessionParticipants { get; set; }
        public List<System.String> CircleParticipants { get; set; }
        public List<System.String> EventPeopleKeys { get; set; }
        public List<System.String> SessionPeopleKeys { get; set; }
        public List<System.String> CirclePeopleKeys { get; set; }
        public List<System.String> SinglePeopleKeys { get; set; }
        public int CurrentEventId { get; set; }        
        public string Owner { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public string PeopleKey { get; set; }
        public string Result { get; set; }
        public string Error { get; set; }

    }
}
