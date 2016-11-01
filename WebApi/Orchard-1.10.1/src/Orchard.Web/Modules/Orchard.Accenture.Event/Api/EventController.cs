using Newtonsoft.Json;
using Orchard.Accenture.Event.Extension;
using Orchard.Accenture.Event.Services;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;

//using Orchard.Users.Services;

namespace Orchard.Accenture.Event.Api
{
    [AuthorizeAppApiAttribute]
    public class EventController : ApiController
    {
        //private readonly IUserService _userService;

        //private readonly IAuthenticationService _authenticationService;
        //private readonly IMembershipService _membershipService;
        //private readonly IOrchardServices _orchardServices;
        
        private static readonly char[] separator = new[] { '{', '}', ',' };
        private readonly IContentManager _contentManager;
        private readonly IContentTypesService _contentTypesService;

        public EventController(
            //IAuthenticationService authenticationService,
            //IMembershipService membershipService,
            //IOrchardServices orchardServices,
            IContentManager contentManager,
            IContentTypesService contentTypesService)
        {
            //_authenticationService = authenticationService;
            //_membershipService = membershipService;
            //_orchardServices = orchardServices;
            _contentManager = contentManager;
            _contentTypesService = contentTypesService;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }


        #region [ PRIVATE ]
        public static IEnumerable ConvertToRows(IEnumerable<Models.ContentItem> source, string[] properties = null)
        {
            foreach (var item in source)
            {
                object[] partsValues = GetPartsValues(item, properties);
                object[] fieldsValues = GetFieldsValues(item, properties);
                object[] values = new object[partsValues.Length + fieldsValues.Length];
                Array.Copy(partsValues, values, partsValues.Length);
                Array.Copy(fieldsValues, 0, values, partsValues.Length, fieldsValues.Length);
                yield return new { id = item.Id, cell = values };
            }
        }

        private static object[] GetPartsValues(Models.ContentItem item, string[] properties)
        {
            var hasProperties = (properties != null) && (properties.Length > 0);
            if (hasProperties)
            {
                List<object> partsValues = new List<object>();
                partsValues.Add(item.Id);
                if (properties.Contains("Title"))
                {
                    partsValues.Add(item.Title);
                }
                if (properties.Contains("Body"))
                {
                    partsValues.Add(item.Body);
                }
                if (properties.Contains("Owner"))
                {
                    partsValues.Add(item.Owner);
                }
                if (properties.Contains("CreateTime"))
                {
                    partsValues.Add(item.CreateTime);
                }
                return partsValues.ToArray();
            }
            else
            {
                object[] partsValues = new object[]
                        {
                            item.Id,
                            item.Title,
                            item.Body,
                            item.Owner,
                            item.CreateTime
                        };
                return partsValues;
            }
        }

        private static object[] GetFieldsValues(Models.ContentItem item, string[] properties)
        {
            var hasProperties = (properties != null) && (properties.Length > 0);
            if (hasProperties)
            {
                List<object> fieldsValues = new List<object>();
                foreach (var fieldName in item.Fields.Keys)
                {
                    if (properties.Contains(fieldName))
                    {
                        fieldsValues.Add(item.Fields[fieldName]);
                    }
                }
                return fieldsValues.ToArray();
            }
            else
            {
                return item.Fields.Values.ToArray();
            }
        }

        private StringContent Serialize(dynamic source, HttpResponseMessage response)
        {
            if (source == null)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            }
            var stringcontent = JsonConvert.SerializeObject(source, Newtonsoft.Json.Formatting.Indented,
                                    new JsonSerializerSettings
                                    {
                                        PreserveReferencesHandling = PreserveReferencesHandling.None
                                    });
            return new StringContent(stringcontent, Encoding.GetEncoding("UTF-8"), "application/json");
        }
        #endregion

        /// <summary>
        /// GET Event/GetAD
        /// example: http://localhost/api/Event/GetADGroup?eid=david.bingjian.yu&domain=dir
        /// </summary>
        /// <param name="eid">eid</param>
        /// <param name="domain">domain</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetAD")]
        public IEnumerable<string> GetAD(string eid, string domain)
        {
            Domain currentDomain = Domain.GetCurrentDomain();
            DirectoryEntry de;

            if ("dir".Equals(domain, StringComparison.OrdinalIgnoreCase))
            {
                de = new DirectoryEntry("LDAP://" + "dir", "cong.wu", "Wucong+222", AuthenticationTypes.Secure);
            }
            else
            {
                de = currentDomain.GetDirectoryEntry();
            }

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

                    yield return JsonConvert.SerializeObject(p);
                }
            }
        }
                
        /// <summary>
        /// GET Event/GetADGroup
        /// example: http://localhost/api/Event/GetADGroup?eid=david.bingjian.yu
        /// </summary>
        /// <param name="eid">eid</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetADGroup")]
        public IEnumerable<string> GetADGroup(string eid)
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
                
        /// <summary>
        /// GET Event/GetContentTypeDefinition
        /// example: http://localhost/api/Event/GetTypeDefinition?type=Participant
        /// </summary>
        /// <param name="type">Content Type Name</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetContentTypeDefinition")]
        public ContentTypeDefinition GetContentTypeDefinition(string type)
        {
            return (from t in _contentManager.GetContentTypeDefinitions()
                    where t.Name == type
                    select t).FirstOrDefault();
        }
                
        /// <summary>
        /// GET Event/GetContentTypes
        /// example : http://localhost/api/Event/GetContentTypes
        /// </summary>
        /// <returns>All content type names</returns>
        [HttpGet]
        [Route("GetContentTypes")]
        public IEnumerable<string> GetContentTypes()
        {
            return _contentTypesService.GetContentTypes();
        }

        /// <summary>
        /// GET Event/LoadContentItems
        /// example: http://localhost/api/Event/LoadContentItems?name=Participant
        /// </summary>
        /// <param name="name"></param>
        /// <param name="properties"></param>
        /// <param name="rows"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadContentItems")]
        public HttpResponseMessage LoadContentItems(string name, [FromBody]string properties, int? rows = null, int? page = null)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                int defaultRows = 10;
                var query = _contentTypesService.Query(name, string.IsNullOrWhiteSpace(properties) ? null : properties.Split(separator, StringSplitOptions.RemoveEmptyEntries));
                if (page != null)
                {
                    query.Skip(((page ?? 1) - 1) * (rows ?? defaultRows))
                                   .Take(rows ?? defaultRows);
                }

                var stringcontent = JsonConvert.SerializeObject(query, Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.None
                });

                response.Content = new StringContent(stringcontent, Encoding.GetEncoding("UTF-8"), "application/json");
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load events :" + ex.Message);
            }


            return response;
        }

        /// <summary>
        /// GET Event/LoadEvents
        /// example: http://localhost/event/api/Event/LoadEvents
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadEvents")]
        public HttpResponseMessage LoadEvents()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var query = _contentTypesService.Query("Event");

                var stringcontent = JsonConvert.SerializeObject(query, Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });

                response.Content = new StringContent(stringcontent, Encoding.GetEncoding("UTF-8"), "application/json");
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load events :" + ex.Message);
            }


            return response;
        }

        
        /// <summary>
        /// GET Event/LoadParticipants
        /// example: http://localhost/api/Event/LoadParticipants
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadParticipants")]
        public HttpResponseMessage LoadParticipants()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var query = _contentTypesService.Query("Participant");

                var stringcontent = JsonConvert.SerializeObject(query, Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.All
                });

                response.Content = new StringContent(stringcontent, Encoding.GetEncoding("UTF-8"), "application/json");
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurs when load events :" + ex.Message);
            }
            return response;
        }

        /// <summary>
        /// GET Event/Search
        /// example: http://localhost/api/Event?name=Participant&_search=super
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_search"></param>
        /// <param name="nd"></param>
        /// <param name="sidx"></param>
        /// <param name="sord"></param>
        /// <param name="properties"></param>
        /// <param name="rows"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Search")]
        public object Search(string name, bool? _search, int? nd, string sidx, string sord, [FromBody]string properties, int? rows = 10, int? page = 1)
        {
            int count;
            var data = _contentTypesService.Query(name, null, null, null, null, string.IsNullOrWhiteSpace(sidx) ? null : new[] { sidx }, new[] { sord }, ((int)page - 1) * (int)rows, (int)rows, out count, string.IsNullOrWhiteSpace(properties) ? null : properties.Split(separator, StringSplitOptions.RemoveEmptyEntries));
            return new
            {
                total = Math.Ceiling((float)count / (float)rows),
                page = page,
                records = count,
                rows = ConvertToRows(data)
            };
        }
                
        /// <summary>
        /// GET Event/Publish
        /// example: http://localhost/api/Event/Publish?name=Participant
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Search")]
        public object Publish(string name)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var content = _contentTypesService.Publish(name);

                response.Content = Serialize(content, response);
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                Logger.Error("Error occurs when load app :" + ex.Message);
            }
            return response;
        }

        // GET api/<controller>/5
        //example: http://localhost/Event/api/Event/12
        //[HttpGet]
        //[Route("GetById")]
        //public Models.ContentItem GetById(int id, [FromBody]string properties)
        //{
        //    return _contentTypesService.Get(id, string.IsNullOrWhiteSpace(properties) ? null : properties.Split(separator, StringSplitOptions.RemoveEmptyEntries));
        //}

        // GET api/<controller>?name=...
        //example: http://localhost/Event/api/Event?name=Participant
        //[HttpGet]
        //[Route("GetContentItems")]
        //public IEnumerable<Models.ContentItem> GetContentItems(string name, [FromBody]string properties, int? rows = null, int? page = null)
        //{
        //    int defaultRows = 10;
        //    var query = _contentTypesService.Query(name, string.IsNullOrWhiteSpace(properties) ? null : properties.Split(separator, StringSplitOptions.RemoveEmptyEntries));
        //    if (page != null)
        //    {
        //        return query.Skip(((page ?? 1) - 1) * (rows ?? defaultRows))
        //                        .Take(rows ?? defaultRows);
        //    }
        //    else
        //    {
        //        return query;
        //    }
        //}
        
    }
}
