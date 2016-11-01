using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.PushNotification.Models
{
    public class Request
    {
        public string BadgeCount { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }
        public List<string> To { get; set; }
    }

    public class NotificationMessage
    {
        public List<Request> request { get; set; }
    }

    public class RawRequest
    {
        public string TagExpression { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Custom { get; set; }
    }

    public class RawNotificationMessage
    {
        public List<RawRequest> request { get; set; }
    }
}