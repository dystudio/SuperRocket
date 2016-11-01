using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard.Accenture.Event.Common;
using Orchard.Accenture.Event.Extension;
using Orchard.Accenture.Event.Services;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace Orchard.Accenture.Event.Api
{
    [AuthorizeAppApiAttribute]
    public class OEventController : ApiController
    {
        private static readonly char[] separator = new[] { '{', '}', ',' };
        private readonly IAuthenticationService _authenticationService;
        private readonly IMembershipService _membershipService;
        private readonly IOrchardServices _orchardServices;
        private readonly IContentManager _contentManager;
        private readonly IContentTypesService _contentTypesService;
        private readonly IOEventService _oEventService;

        public OEventController(
            IAuthenticationService authenticationService,
            IMembershipService membershipService,
            IOrchardServices orchardServices,
            IContentManager contentManager,
            IContentTypesService contentTypesService,
            IOEventService oEventService)
        {
            _authenticationService = authenticationService;
            _membershipService = membershipService;
            _orchardServices = orchardServices;
            _contentManager = contentManager;
            _contentTypesService = contentTypesService;
            _oEventService = oEventService;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        #region [ PRIVATE ]
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

        private IEnumerable<string> GetADGroup(string eid)
        {
            Domain domain = Domain.GetCurrentDomain();
            var de = domain.GetDirectoryEntry();
            var ds = new DirectorySearcher { SearchRoot = de };
            string strUserFilter = "(&(objectCategory=user)(objectClass=user)(Name=" + eid + "))";
            ds.Filter = strUserFilter;

            DirectoryEntry user = null;
            foreach (SearchResult result in ds.FindAll())
            {
                user = result.GetDirectoryEntry();
            }

            if (user != null)
            {
                PropertyCollection pcoll = user.Properties;

                foreach (var p in pcoll["memberof"])
                {
                    string[] str = p.ToString().Split(',');
                    yield return str[0].Substring(3);
                }
            }
        }

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
        #endregion

        //GET api/Orchard.Accenture.Event/OEvent/LoadApp
        //example : http://localhost/api/OEvent/LoadApp?app=another
        //[AuthorizeApp]
        //[CustomAuthorize]        
        [HttpGet]
        [Route("LoadApp")]
        public HttpResponseMessage LoadApp(string app)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadApp(app);

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load app :" + ex.Message);
            }
            return response;
        }

        //GET api/Orchard.Accenture.Event/OEvent/LoadEvents
        //example : http://localhost/api/OEvent/LoadEvents?app=another
        [HttpGet]
        [Route("LoadEvents")]
        public HttpResponseMessage LoadEvents(string app)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadEvents(app, GetEid(), GetADGroup(GetEid()));

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load events :" + ex.Message);
            }
            return response;
        }

        //GET api/Orchard.Accenture.Event/OEvent/LoadEvent
        //example : http://localhost/api/OEvent/LoadEvent/1
        [HttpGet]
        [Route("LoadEvent")]
        public HttpResponseMessage LoadEvent(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadEvents(id, GetADGroup(GetEid()));

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load events :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadSessions/eventId
        //example : http://localhost/api/OEvent/LoadSessions/1
        [HttpGet]
        [Route("LoadSessions")]
        public HttpResponseMessage LoadSessions(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadSessions(id, GetEid(), GetADGroup(GetEid()));
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load sessions :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadParticipants by event id & group ID
        //example : http://localhost/api/OEvent/LoadParticipants?id=43&groupId=45
        [HttpGet]
        [Route("LoadParticipants")]
        public HttpResponseMessage LoadParticipants(int? id, int? groupId)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadParticipants(id, groupId);

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load participants :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadParticipants by EID
        //example : http://localhost/api/OEvent/LoadParticipants?eid=david
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

        // GET api/Orchard.Accenture.Event/OEvent/LoadParticipants by event id & EID
        //example : http://localhost/api/OEvent/LoadParticipants?eid=david&eventId=43
        [HttpGet]
        [Route("LoadParticipants")]
        public HttpResponseMessage LoadParticipants(string eid, int eventId)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadParticipants(eid, eventId);

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load participants :" + ex.Message);
            }
            return response;
        }

        //GET api/Orchard.Accenture.Event/OEvent/LoadProfile
        //example : http://localhost/api/OEvent/LoadProfile?eid=david.bingjian.yu
        [HttpGet]
        [Route("LoadProfile")]
        public HttpResponseMessage LoadProfile(string eid)
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
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }

        //GET api/Orchard.Accenture.Event/OEvent/LoadProfile
        //example : http://localhost/api/OEvent/LoadProfile?id=participantId
        [HttpGet]
        [Route("LoadProfile")]
        public HttpResponseMessage LoadProfile(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadParticipants(id);

                response.Content = Serialize(content, response);

            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load profile :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadParticipants
        //example : http://localhost/api/OEvent/LoadParticipants
        [HttpGet]
        [Route("LoadParticipants")]
        public HttpResponseMessage LoadParticipants()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadParticipants();
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load participants :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadTerms/name
        //example : http://localhost/api/OEvent/LoadTerms?name=ParticipantGroup
        [HttpGet]
        [Route("LoadTerms")]
        public HttpResponseMessage LoadTerms(string name)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadTerms(name);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load participants :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadParticipantsLayout/eventId
        //example : http://localhost/api/OEvent/LoadParticipantsLayout/1
        [HttpGet]
        [Route("LoadParticipantsLayout")]
        public HttpResponseMessage LoadParticipantsLayout(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadParticipantsLayout(id);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load LoadParticipantsLayout :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadDocumentsLayout/eventId
        //example : http://localhost/api/OEvent/LoadDocumentsLayout/1
        [HttpGet]
        [Route("LoadDocumentsLayout")]
        public HttpResponseMessage LoadDocumentsLayout(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadDocumentsLayout(id, GetEid(), GetADGroup(GetEid()));
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load LoadDocumentsLayout :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadChildren/eventId&levelID
        //example : http://localhost/api/OEvent/LoadChildren?id=53&level=2
        [HttpGet]
        [Route("LoadChildren")]
        public HttpResponseMessage LoadChildren(int id, int level)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadChildren(id, level);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load LoadChildren :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadParticipantsByGroup/eventId&groupname
        //example : http://localhost/api/OEvent/LoadParticipantsByGroup?id=48&name=beautiful
        [HttpGet]
        [Route("LoadParticipantsByGroup")]
        public HttpResponseMessage LoadParticipantsByGroup(int? id, string name)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadParticipantsByGroup(id, name);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load participants :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadInfoCards by event id
        //example : http://localhost/api/OEvent/LoadInfoCards/1
        [HttpGet]
        [Route("LoadInfoCards")]
        public HttpResponseMessage LoadInfoCards(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadInfoCards(id);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load infoCards :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadInfoCards
        //example : http://localhost/api/OEvent/LoadInfoCards
        [HttpGet]
        [Route("LoadInfoCards")]
        public HttpResponseMessage LoadInfoCards()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadInfoCards();
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load infoCards :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadPolls by event id
        //example : http://localhost/api/OEvent/LoadPolls/1
        [HttpGet]
        [Route("LoadPolls")]
        public HttpResponseMessage LoadPolls(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadPolls(id);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load Polls :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadEvaluationList by event id
        //example : http://localhost/api/OEvent/LoadEvaluationList/1
        [HttpGet]
        [Route("LoadEvaluationList")]
        public HttpResponseMessage LoadEvaluationList(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadEvaluationList(id);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load Evaluation List :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadDocuments/EventId&groupid
        //example : http://localhost/api/OEvent/LoadDocuments?id=43&groupId=47
        [HttpGet]
        [Route("LoadDocuments")]
        public HttpResponseMessage LoadDocuments(int? id, int? groupId)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadDocuments(id, groupId, GetADGroup(GetEid()));
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load LoadDocuments :" + ex.Message);
            }
            return response;
        }

        // GET api/Orchard.Accenture.Event/OEvent/LoadAvatar/participantId
        //example : http://localhost/api/OEvent/LoadAvatar/participantId
        [HttpGet]
        [Route("LoadAvatar")]
        public HttpResponseMessage LoadAvatar(int? id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _oEventService.LoadAvatar(id);
                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load LoadAvatar :" + ex.Message);
            }
            return response;
        }        
    }
}
