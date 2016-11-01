using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard.Accenture.Event.Common;
using Orchard.Accenture.Event.Extension;
using Orchard.Accenture.Event.Services;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web.Http;

//using Orchard.ContentManagement;
//using Orchard.Security;
//using System.DirectoryServices;
//using System.DirectoryServices.ActiveDirectory;

namespace Orchard.Accenture.Event.Api
{
    [AuthorizeAppApiAttribute]
    public class PeopleController : ApiController
    {
        //private readonly IUserService _userService;        
        
        //private readonly IAuthenticationService _authenticationService;
        //private readonly IMembershipService _membershipService;        
        //private readonly IOrchardServices _orchardServices;
        //private readonly IContentManager _contentManager;
        //private readonly IContentTypesService _contentTypesService;

        private readonly IPeopleService _peopleService;
        private readonly IOEventService _oEventService;

        public PeopleController(
            //IAuthenticationService authenticationService,
            //IMembershipService membershipService,
            //IOrchardServices orchardServices,
            //IContentManager contentManager,
            //IContentTypesService contentTypesService,
            IPeopleService peopleService,
            IOEventService oEventService)
        {
            //_authenticationService = authenticationService;
            //_membershipService = membershipService;
            //_orchardServices = orchardServices;
            //_contentManager = contentManager;
            //_contentTypesService = contentTypesService;
            _peopleService = peopleService;
            _oEventService = oEventService;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        #region [ PRIVATE ]
        private string GetEid()
        {
            string eid = string.Empty;
            var jsob = getObjectFromJWT();

            if (jsob != null)
            {
                eid = jsob.GetValue("https://federation-sts.accenture.com/schemas/claims/1/enterpriseid").ToString();
            }
            return eid;
        }
        
        private JObject getObjectFromJWT()
        {
            if (Request.Headers.Authorization != null)
            {
                string jwt = Request.Headers.Authorization.Parameter;

                if (!string.IsNullOrWhiteSpace(jwt))
                {
                    string[] tokens = jwt.Split('.');

                    if (tokens != null && tokens.Length == 3)
                    {
                        string claims = tokens[1];
                        int mod4 = claims.Length % 4;

                        if (mod4 > 0)
                        {
                            claims += new string('=', 4 - mod4);
                        }

                        return (JObject)JsonConvert.DeserializeObject(Encoding.ASCII.GetString(Convert.FromBase64String(claims)));
                    }
                }
            }

            return null;

        }

        private StringContent Serialize(dynamic source, HttpResponseMessage response)
        {
            if (source == null)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            }
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new NullToEmptyStringResolver(),
                PreserveReferencesHandling = PreserveReferencesHandling.None
            };

            var stringcontent = JsonConvert.SerializeObject(source, Newtonsoft.Json.Formatting.Indented, settings);
            return new StringContent(stringcontent, Encoding.GetEncoding("UTF-8"), "application/json");
        }

        //Not used?
        //private IEnumerable<string> GetADGroup(string eid)
        //{
        //    Domain domain = Domain.GetCurrentDomain();
        //    var de = domain.GetDirectoryEntry();
        //    var ds = new DirectorySearcher { SearchRoot = de };
        //    string strUserFilter = "(&(objectCategory=user)(objectClass=user)(Name=" + eid + "))";
        //    ds.Filter = strUserFilter;

        //    DirectoryEntry user = null;
        //    foreach (SearchResult result in ds.FindAll())
        //    {
        //        user = result.GetDirectoryEntry();
        //    }

        //    if (user != null)
        //    {
        //        PropertyCollection pcoll = user.Properties;

        //        foreach (var p in pcoll["memberof"])
        //        {
        //            string[] str = p.ToString().Split(',');
        //            yield return str[0].Substring(3);
        //        }
        //    }
        //}
        #endregion

        /// <summary>
        /// GET People/LoadAvatar
        /// example : http://localhost/api/People/LoadAvatar?eid=jerson.m.cantoneros
        /// </summary>
        /// <param name="eid">eid</param>
        /// <returns>name, peoplekey, avatar</returns>
        [HttpGet]
        [Route("LoadAvatar")]
        public HttpResponseMessage LoadAvatar(string eid)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _peopleService.LoadAvatar(eid);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }

        /// <summary>
        /// GET People/LoadMultipleParticipants
        /// example : http://localhost/api/OEvent/LoadMultipleParticipants?eid=adrian.r.c.palmares
        /// </summary>
        /// <param name="eid">eid</param>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadMultipleParticipants")]
        public HttpResponseMessage LoadMultipleParticipants(string eid)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadMultipleParticipants(eid);

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load participants :" + ex.Message);
            }
            return response;
        }
        
        /// <summary>
        /// GET People/LoadOriginalProfile
        /// example : http://localhost/api/People/LoadOriginalProfile?eid=adrian.r.c.palmares
        /// </summary>
        /// <param name="eid">eid</param>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadOriginalProfile")]
        public HttpResponseMessage LoadOriginalProfile(string eid)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _peopleService.LoadOriginalProfile(eid);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }        
                
        /// <summary>
        /// GET People/LoadParticipants
        /// example : http://localhost/api/People/LoadParticipants?eid=adrian.r.c.palmares
        /// </summary>
        /// <param name="eid">eid</param>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadParticipants")]
        public HttpResponseMessage LoadParticipants(string eid)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadParticipants(eid);

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load participants :" + ex.Message);
            }
            return response;
        }

        /// <summary>
        /// GET People/LoadProfile
        /// Needs token to work
        /// example : http://localhost/api/People/LoadProfile
        /// </summary>
        /// <returns>Profile of current user</returns>
        [HttpGet]
        [Route("LoadProfile")]
        public HttpResponseMessage LoadProfile()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var eid = GetEid();
                var content = _peopleService.GetProfile(eid);

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }
                
        /// <summary>
        /// GET People/LoadProfile
        /// example : http://localhost/Event/api/People/LoadProfile?eid=jerson.m.cantoneros
        /// </summary>
        /// <param name="eid">eid</param>
        /// <returns>Profile of eid</returns>
        [HttpGet]
        [Route("LoadProfile")]
        public HttpResponseMessage LoadProfile(string eid)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _peopleService.GetProfile(eid);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }

        /// <summary>
        /// GET People/LoadBulkProfile
        /// EID list all david.bingjian.yu (20 items)
        /// example : http://localhost/api/People/LoadBulkProfile
        /// </summary>
        /// <returns>All david.bingjian.yu profiles</returns>
        [HttpGet]
        [Route("LoadProfile")]
        public HttpResponseMessage LoadBulkProfile()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                // var eid = GetEid();
                List<string> eids = new List<string>();
                for (int i = 0; i < 20; i++)
                {
                    eids.Add("david.bingjian.yu");
                }
                //string[] edis = { "david.bingjian.yu", "jipeng.zhang" };
                var content = _peopleService.GetBulkProfile(eids.ToArray());
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }

        /// <summary>
        /// GET People/LoadProfiles
        /// No Data, supposed to be MRDR (not used)
        /// example : http://localhost/api/People/LoadProfiles?eid=jerson.m.cantoneros
        /// </summary>
        /// <param name="eid">eid</param>
        /// <returns>NULL data</returns>
        [HttpGet]
        [Route("LoadProfiles")]
        public HttpResponseMessage LoadProfiles(string eid)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _peopleService.LoadProfilesFromMRDR(eid);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }        
    }
}
