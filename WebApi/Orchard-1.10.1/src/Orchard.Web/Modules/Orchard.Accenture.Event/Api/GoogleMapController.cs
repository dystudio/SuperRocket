using Newtonsoft.Json;
using Orchard.Accenture.Event.Extension;
using Orchard.Accenture.Event.Services;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace Orchard.Accenture.Event.Api
{
    [AuthorizeAppApiAttribute]
    public class GoogleMapController : ApiController
    {
        private readonly IGoogleMapService _googleMapService;
        public GoogleMapController(
            IGoogleMapService googleMapService
            )
        {
            _googleMapService = googleMapService;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        /// <summary>
        /// GET GoogleMap/LoadMap
        /// example : http://localhost/api/GoogleMap/LoadMap?location=dalian
        /// </summary>
        /// <param name="location">location name</param>
        /// <returns>unreadable location data</returns>
        [HttpGet]
        [Route("LoadMap")]
        public HttpResponseMessage LoadMap(string location)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _googleMapService.LoadGoogleMap(location);
                var stringcontent = JsonConvert.SerializeObject(content, Newtonsoft.Json.Formatting.Indented,
                                    new JsonSerializerSettings
                                    {
                                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                    });
                response.Content = new StringContent(stringcontent, Encoding.GetEncoding("UTF-8"), "application/json");
                
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load google map :" + ex.Message);
            }
            return response;
        }
    }
}
