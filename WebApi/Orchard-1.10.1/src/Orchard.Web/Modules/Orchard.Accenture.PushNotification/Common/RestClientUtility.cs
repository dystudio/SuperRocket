using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard.Accenture.PushNotification.Models;
using Orchard.Localization;
using Orchard.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchard.Accenture.PushNotification.Common
{
    public class RestClientUtility : Component, IRestClientUtility
    {
        private RestClient _client ;
        public RestClientUtility()
        {
            _client = new RestClient();
        }

        public RestClientUtility(string endPoint)
        {
            _client = new RestClient(endPoint);
        }
        public string GetToken(string userName, string password, string scope, string endPoint)
        {
            _client.BaseUrl = new Uri(endPoint);
            var request = new RestRequest(Method.POST);

            request.AddHeader("ContentType", "application/x-www-form-urlencoded");
            request.RequestFormat = DataFormat.Json;

            request.AddBody(new
            {
                grant_type = "password",
                username = userName,
                password = password,
                scope = scope
            });

            string accessToken = string.Empty;
            var response = _client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = response.Content;
                JObject jsob = (JObject)JsonConvert.DeserializeObject(content);
                accessToken = jsob["access_token"].ToString();
            }
            else
            {
                Logger.Error("Could not get token from ESO site." + response.Content);
            }
            return accessToken;
        }

        public void Notify(string scope,string token, string tenant, string topic, NotificationMessage message)
        {
            _client.BaseUrl = new Uri(scope);

            var request = new RestRequest("hubs-service/Tenants('{tenant}')/Topics('{topic}')/Default.Notify", Method.POST);
            request.AddUrlSegment("tenant", tenant);
            request.AddUrlSegment("topic", topic);

            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Content-Type", "application/json;charset=utf-8");
            request.AddBody(message);

            var response = _client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.Error("Error occurs when request the azure to Notify." + response.StatusCode);
                Logger.Error("The " + message + " was not sent.");
            }

        }
        public void NotifyAsync(string scope,string token, string tenant, string topic, NotificationMessage message)
        {
            _client.BaseUrl = new Uri(scope);

            var request = new RestRequest("hubs-service/Tenants('{tenant}')/Topics('{topic}')/Default.Notify", Method.POST);
            request.AddUrlSegment("tenant", tenant);
            request.AddUrlSegment("topic", topic);

            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Content-Type", "application/json;charset=utf-8");
            request.AddBody(message);

            _client.ExecuteAsync(request, response =>
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Logger.Error("Error occurs when request the azure to NotifyAsync." + response.StatusCode);
                    Logger.Error("The " + message + " was not sent.");
                }
            });
        }
        public void RawNotify(string scope,string token, string tenant, string topic, RawNotificationMessage message)
        {
            _client.BaseUrl = new Uri(scope);

            var request = new RestRequest("hubs-service/Tenants('{tenant}')/Topics('{topic}')/Default.RawNotify", Method.POST);
            request.AddUrlSegment("tenant", tenant);
            request.AddUrlSegment("topic", topic);

            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Content-Type", "application/json;charset=utf-8");
            request.AddBody(message);

            var response = _client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.Error("Error occurs when request the azure to RawNotify." + response.ErrorMessage);
            }
        }
        public void RawNotifyAsync(string scope,string token, string tenant, string topic, RawNotificationMessage message)
        {
            _client.BaseUrl = new Uri(scope);

            var request = new RestRequest("hubs-service/Tenants('{tenant}')/Topics('{topic}')/Default.RawNotify", Method.POST);
            request.AddUrlSegment("tenant", tenant);
            request.AddUrlSegment("topic", topic);

            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Content-Type", "application/json;charset=utf-8");
            request.AddBody(message);

            _client.ExecuteAsync(request, response =>
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Logger.Error("Error occurs when request the azure to RawNotifyAsync." + response.ErrorMessage);
                }
            });
        }
    }
}
