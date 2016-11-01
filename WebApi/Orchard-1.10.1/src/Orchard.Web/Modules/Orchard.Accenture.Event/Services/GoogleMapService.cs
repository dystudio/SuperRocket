using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace Orchard.Accenture.Event.Services
{
    public class GoogleMapService : IGoogleMapService
    {

        // 1.  Get the Longitude and Latitude by Location string.
        //http://maps.google.com/maps/api/geocode/json?address={0}&language={1}
        //address is location string and language is the format like en-US
        //the response json is attached as attachment.U may check it

        //2.  Get the image by latitude and longitude.
        //http://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=18&size=600x300&maptype=roadmap
        //the url reference is the image,.
        //center parameter’ format is latitude,longitude    for example  39.9390715,116.1165916 


        private HttpClient _client;
        private readonly ISiteService _siteService;
        public GoogleMapService(ISiteService siteService)
        {
            _siteService = siteService;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }


        public dynamic LoadGoogleMap(string location)
        {
            var response = Task.Run(() => GetMapLongitudeLatitude(location)).Result;

            var googleMapResponse = response.Content.ReadAsStringAsync().Result;
            JObject jsob = (JObject)JsonConvert.DeserializeObject(googleMapResponse);
            var json = jsob.GetValue("results").ToString();
            JArray jay = (JArray)JsonConvert.DeserializeObject(json);

            string lat = string.Empty;
            string lng = string.Empty;

            foreach (JToken token in jay)
            {
                JObject jo = (JObject)token;
                lat = jo["geometry"]["location"]["lat"].ToString();
                lng = jo["geometry"]["location"]["lng"].ToString();
            }

            string position = lat + "," + lng;

            var bytes = Task.Run(() => GetMap(position)).Result;

            var result = Convert.ToBase64String(bytes).ToString();
            return result;
        }

        #region private
        private void Initialize()
        {
            var baseUrl = "http://maps.google.com/maps/api/";
            _client = new HttpClient();
            _client.BaseAddress = new Uri(baseUrl);
            _client.DefaultRequestHeaders.Accept.Clear();
        }
        private async Task<HttpResponseMessage> GetMapLongitudeLatitude(string location)
        {
            Initialize();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var culture = _siteService.GetSiteSettings().SiteCulture;
            return await _client.GetAsync(string.Format("geocode/json?address={0}&language={1}", location, culture));
        }

        private async Task<byte[]> GetMap(string position)
        {
            Initialize();
            return await _client.GetByteArrayAsync(string.Format("staticmap?center={0}&zoom=18&size=600x300&maptype=roadmap", position));
        }
        #endregion
    }
}