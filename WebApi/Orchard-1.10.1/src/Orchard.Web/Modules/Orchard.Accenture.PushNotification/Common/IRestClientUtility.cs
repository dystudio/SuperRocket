using Orchard.Accenture.PushNotification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchard.Accenture.PushNotification.Common
{
    public interface IRestClientUtility : ISingletonDependency
    {
        string GetToken(string userName, string password, string scope, string endPoint);
        void Notify(string scope,string token, string tenant, string topic, NotificationMessage message);
        void NotifyAsync(string scope,string token, string tenant, string topic, NotificationMessage message);
        void RawNotify(string scope,string token, string tenant, string topic, RawNotificationMessage message);
        void RawNotifyAsync(string scope,string token, string tenant, string topic, RawNotificationMessage message);

    }
}
