using Orchard.Accenture.Event.Common;
using Orchard.Accenture.Event.Interfaces;
using Orchard.Accenture.Event.Models;
using Orchard.Accenture.Event.ServiceModels;
using Orchard.Accenture.Event.ViewModels;
using Orchard;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Orchard.Data;
using System.Net.Http;
using Orchard.Settings;
using Orchard.Accenture.Event.Orchard.Odata.Services;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;
using Orchard.Services;
using System.Threading;
using System.Diagnostics;
//using Rebar.Soa.Client;
//using Rebar.Soa.Client.OData;
namespace Orchard.Accenture.Event.Services
{
    public class PeopleService : IPeopleService
    {

        private readonly IContentManager _contentManager;
        private readonly ICacheManager _cacheManager;
        private readonly IOrchardServices _orchardServices;
        private readonly ISiteService _siteService;
        private readonly IClock _clock;
        private HttpClient _client;


        public PeopleService(
            IContentManager contentManager,
            ICacheManager cacheManager,
            ISignals signals,
            IOrchardServices orchardServices,
            ISiteService siteService,
            IClock clock

            )
        {
            _contentManager = contentManager;
            _orchardServices = orchardServices;
            _cacheManager = cacheManager;
            _siteService = siteService;
            _clock = clock;


            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public dynamic GetProfile(string eid)
        {
            var result = _cacheManager.Get(CacheAndSignals.PROFILE_CACHE + eid, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(60)));
                return GetCachedProfile(eid);
            });
            return result;
        }

        public dynamic LoadProfile(string eid)
        {
            return GetCachedProfile(eid);
        }

        public dynamic LoadProfilesFromMRDR(string eid)
        {
            //        < key = "webservice:Domain" value = "dir" />
            //   < add key = "webservice:UserName" value = "4079_CMP_Dev" />
            //   < add key = "webservice:Password" value = "sRi4zAg7fUk*i" />
            //   < add key = "webservice:profile:Domain" value = "ds" />
            //   < add key = "webservice:profile:UserName" value = "4079_CMP_Dev" />
            //   < add key = "webservice:profile:Password" value = "sRi4zAg7fUk*i" />
            //                 var domain = "ds";
            //var userName = "4079_CMP_Dev";
            //var password = "sRi4zAg7fUk*i";
            //var baseUrl = "https://mrdr.accenture.com/1033_MRDR/Stage/Resources/People/1.0.0/";

            //var credentials = new NetworkCredential(userName, password, domain);

            //var handler = new HttpClientHandler { Credentials = credentials };
            //_client = new HttpClient(handler);
            //_client.BaseAddress = new Uri(baseUrl);
            //_client.DefaultRequestHeaders.Accept.Clear();
            //_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // var serviceURI = baseUrl.CreateServiceableUri();

            // serviceURI.AppendUriPath("PeopleEntities")
            // .AppendUriQueryParameters(new Dictionary<string, string>
            //          {
            //                 {"$top", "50" },
            //                 {"$format", "json"},
            //                 {"$filter", string.Format("EnterpriseId eq {0}", eid)},
            //                 {"$select", "PeopleKey,EnterpriseId,FirstName,LastName,PersonalDisplayName,CountryKey,CountryName,JobCode,JobCodeDescription,StandardJobCode,StandardJobDescription,OfficePhone,OrgUnitId,InternetMail,CrossWorkforceCareerLevelId,CrossWorkforceCareerLevelDescription,TalentSegmentDescription,MetroCityCode,MetroCityDescription,CareerTrackDescription,CareerCounselorName,FacilityDescription,GeographicUnitDescription,MobilePhone,MostRecentHireDate,ContractBasedCode,CapabilityDescription,SpecialtyDescription"}
            //            });

            // WebClient client = ServiceCallFactory.CreateServiceableWebClient();
            // client.Credentials = credentials;
            //var response = client.DownloadString(serviceURI.ToString());

            // return response;
            return null;
        }

        public dynamic LoadAvatar(string eid)
        {
            var result = _cacheManager.Get(CacheAndSignals.PEOPLE_AVATAR_CACHE + eid, ctx =>
            {
                ctx.Monitor(_clock.When(TimeSpan.FromMinutes(60)));
                return LoadCachedAvatar(eid);
            });
            return result;
        }

        public dynamic LoadOriginalProfile(string eid)
        {
            var response = Task.Run(() => GetPeopleProfile(eid)).Result;

            JObject jsob = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            var result = JsonConvert.DeserializeObject<List<KeyValue>>(jsob.GetValue("CupsProfile").ToString()) as IList<KeyValue>;
            return result;
        }

        public dynamic GetBulkProfile(string[] eids)
        {
            List<object> finalResults = new List<object>();
            List<ObjectPassed> results = new List<ObjectPassed>();
            List<ObjectPassed> resultsAvatar = new List<ObjectPassed>();

            List<Task<HttpResponseMessage>> list = new List<Task<HttpResponseMessage>>();
            List<Task<byte[]>> listAvatar = new List<Task<byte[]>>();
            Initialize();

            #region People
            Func<object, HttpResponseMessage> action = (object itemObj) =>
            {
                var result = _client.GetAsync(string.Format("json/People/{0}", ((ObjectPassed)itemObj).Item.ToString())).Result;
                ((ObjectPassed)itemObj).Result = result;
                results.Add((ObjectPassed)itemObj);

                var taskId = Task.CurrentId;
                var threadId = Thread.CurrentThread.ManagedThreadId;
                Debug.WriteLine("Task Id :" + taskId);
                Debug.WriteLine("Thread Id :" + threadId);

                return result;
            };


            CancellationTokenSource cancelSignal = new CancellationTokenSource();
            try
            {
                foreach (var item in eids)
                {
                    ObjectPassed pass = new ObjectPassed();
                    pass.Item = item;
                    list.Add(Task<HttpResponseMessage>.Factory.StartNew(action, pass, cancelSignal.Token));
                }

                Task[] tasks = list.ToArray();
                while (!Task.WaitAll(tasks, int.MaxValue, cancelSignal.Token)) ;

            }
            catch (AggregateException ex)
            {
                cancelSignal.Cancel();
                Logger.Error(ex.Message);
            }
            #endregion

            #region avatar
            Func<object, byte[]> actionAvatar = (object itemObj) =>
                {
                    var result = _client.GetByteArrayAsync(string.Format("People/ProfilePicture/{0}", ((ObjectPassed)itemObj).Item.ToString())).Result;
                    ((ObjectPassed)itemObj).Result = result;
                    resultsAvatar.Add((ObjectPassed)itemObj);

                    var taskId = Task.CurrentId;
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    Debug.WriteLine("Task Id :" + taskId);
                    Debug.WriteLine("Thread Id :" + threadId);

                    return result;
                };


            CancellationTokenSource cancelSignalAvatar = new CancellationTokenSource();
            try
            {
                foreach (var responseResult in results)
                {
                    ObjectPassed pass = new ObjectPassed();
                    pass.Item = responseResult.Item;
                    listAvatar.Add(Task<byte[]>.Factory.StartNew(actionAvatar, pass, cancelSignalAvatar.Token));
                }

                Task[] tasksAvatar = listAvatar.ToArray();
                while (!Task.WaitAll(tasksAvatar, int.MaxValue, cancelSignalAvatar.Token)) ;

            }
            catch (AggregateException ex)
            {
                cancelSignalAvatar.Cancel();
                Logger.Error(ex.Message);
            }
            #endregion

            #region aggregate
            foreach (var responseResult in results)
            {
                JObject jsob = (JObject)JsonConvert.DeserializeObject(((HttpResponseMessage)responseResult.Result).Content.ReadAsStringAsync().Result);
                if (jsob != null)
                {
                    var result = JsonConvert.DeserializeObject<List<KeyValue>>(jsob.GetValue("CupsProfile").ToString()) as IList<KeyValue>;

                    string avatar = string.Empty;

                    try
                    {
                        var bytes = resultsAvatar.Where(a => a.Item == responseResult.Item).FirstOrDefault().Result as byte[];
                        avatar = Convert.ToBase64String(bytes).ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("The person's avatar has some problem, please make sure it is a valid picture." + ex.Message);
                    }

                    var enterpriseId = responseResult.Item;
                    var peopleKey = result.Where(i => i.Key == "peoplekey").FirstOrDefault().Value;
                    var displayName = string.IsNullOrEmpty(result.Where(i => i.Key == "displayname").FirstOrDefault().Value) ?
                                      result.Where(i => i.Key == "firstname").FirstOrDefault().Value + " " + result.Where(i => i.Key == "lastname").FirstOrDefault().Value : result.Where(i => i.Key == "displayname").FirstOrDefault().Value;
                    var avatarData = avatar;
                    var email = result.Where(i => i.Key == "workemail").FirstOrDefault().Value;
                    var workEmail = result.Where(i => i.Key == "workemail").FirstOrDefault().Value;
                    var phone = result.Where(i => i.Key == "mobile").FirstOrDefault().Value;
                    var workPhone = result.Where(i => i.Key == "workphone").FirstOrDefault().Value;
                    var mobile = result.Where(i => i.Key == "mobile").FirstOrDefault().Value;
                    var country = result.Where(i => i.Key == "countryname").FirstOrDefault().Value;
                    var countryHome = result.Where(i => i.Key == "countryname").FirstOrDefault().Value;
                    var city = result.Where(i => i.Key == "homecity").FirstOrDefault().Value;
                    var homeCity = result.Where(i => i.Key == "homecity").FirstOrDefault().Value;
                    var location = result.Where(i => i.Key == "currentlocation").FirstOrDefault().Value;
                    var currentLocation = result.Where(i => i.Key == "currentlocation").FirstOrDefault().Value;
                    var talentSegment = result.Where(i => i.Key == "talentsegmentdescr").FirstOrDefault().Value;
                    var jobTitle = result.Where(i => i.Key == "sps-jobtitle").FirstOrDefault().Value;
                    var careerTrack = result.Where(i => i.Key == "careertrackdesc").FirstOrDefault().Value;
                    var careerLevel = result.Where(i => i.Key == "sps-jobtitle").FirstOrDefault().Value;
                    var domainSpecialty = result.Where(i => i.Key == "domainspecialty").FirstOrDefault().Value;
                    var industrySpecialty = result.Where(i => i.Key == "industryspecialtydescr").FirstOrDefault().Value;
                    var firstSecondarySpecialty = result.Where(i => i.Key == "firstsecondaryspecialtydescr").FirstOrDefault().Value;
                    var secondSecondarySpecialty = result.Where(i => i.Key == "secondsecondaryspecialtydescr").FirstOrDefault().Value;
                    var standardJobCode = result.Where(i => i.Key == "standardjobcd").FirstOrDefault().Value;
                    var timezone = result.Where(i => i.Key == "timezone").FirstOrDefault().Value;
                    var bio = result.Where(i => i.Key == "professionalbio").FirstOrDefault().Value;
                    var orglevel2desc = result.Where(i => i.Key == "orglevel2desc").FirstOrDefault().Value;
                    var currentProjects = result.Where(i => i.Key == "currentprojects").FirstOrDefault().Value;
                    var currentClient = result.Where(i => i.Key == "currentclient").FirstOrDefault().Value;
                    var profile = new
                    {
                        EnterpriseId = enterpriseId,
                        PeopleKey = peopleKey,
                        DisplayName = displayName,
                        Avatar = avatarData,
                        Email = email,
                        WorkEmail = workEmail,
                        Phone = phone,
                        WorkPhone = workPhone,
                        Mobile = mobile,
                        Country = country,
                        CountryHome = countryHome,
                        City = city,
                        HomeCity = homeCity,
                        Location = location,
                        CurrentLocation = currentLocation,
                        TalentSegment = talentSegment,
                        JobTitle = jobTitle,
                        CareerTrack = careerTrack,
                        CareerLevel = careerLevel,
                        DomainSpecialty = domainSpecialty,
                        IndustrySpecialty = industrySpecialty,
                        FirstSecondarySpecialty = firstSecondarySpecialty,
                        SecondSecondarySpecialty = secondSecondarySpecialty,
                        StandardJobCode = standardJobCode,
                        Timezone = timezone,
                        Bio = bio,
                        Orglevel2desc = orglevel2desc,
                        CurrentProjects = currentProjects,
                        CurrentClient = currentClient
                    };
                    finalResults.Add(profile);
                }
            }
            #endregion

            return finalResults;
        }
        #region private
        private dynamic GetCachedProfile(string eid)
        {
            var response = Task.Run(() => GetPeopleProfile(eid)).Result;

            JObject jsob = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            var result = JsonConvert.DeserializeObject<List<KeyValue>>(jsob.GetValue("CupsProfile").ToString()) as IList<KeyValue>;


            string avatar = string.Empty;

            try
            {
                var bytes = Task.Run(() => GetAvatar(eid)).Result;
                avatar = Convert.ToBase64String(bytes).ToString();
            }
            catch (Exception ex)
            {
                Logger.Error("The person's avatar has some problem, please make sure it is a valid picture." + ex.Message);
            }

            var enterpriseId = result.Where(i => i.Key == "enterpriseid").FirstOrDefault().Value;
            var peopleKey = result.Where(i => i.Key == "peoplekey").FirstOrDefault().Value;
            var displayName = string.IsNullOrEmpty(result.Where(i => i.Key == "displayname").FirstOrDefault().Value) ?
                              result.Where(i => i.Key == "firstname").FirstOrDefault().Value + " " + result.Where(i => i.Key == "lastname").FirstOrDefault().Value : result.Where(i => i.Key == "displayname").FirstOrDefault().Value;
            var avatarData = avatar;
            var email = result.Where(i => i.Key == "workemail").FirstOrDefault().Value;
            var workEmail = result.Where(i => i.Key == "workemail").FirstOrDefault().Value;
            var phone = result.Where(i => i.Key == "mobile").FirstOrDefault().Value;
            var workPhone = result.Where(i => i.Key == "workphone").FirstOrDefault().Value;
            var mobile = result.Where(i => i.Key == "mobile").FirstOrDefault().Value;
            var country = result.Where(i => i.Key == "countryname").FirstOrDefault().Value;
            var countryHome = result.Where(i => i.Key == "countryname").FirstOrDefault().Value;
            var city = result.Where(i => i.Key == "homecity").FirstOrDefault().Value;
            var homeCity = result.Where(i => i.Key == "homecity").FirstOrDefault().Value;
            var location = result.Where(i => i.Key == "currentlocation").FirstOrDefault().Value;
            var currentLocation = result.Where(i => i.Key == "currentlocation").FirstOrDefault().Value;
            var talentSegment = result.Where(i => i.Key == "talentsegmentdescr").FirstOrDefault().Value;
            var jobTitle = result.Where(i => i.Key == "sps-jobtitle").FirstOrDefault().Value;
            var careerTrack = result.Where(i => i.Key == "careertrackdesc").FirstOrDefault().Value;
            var careerLevel = result.Where(i => i.Key == "sps-jobtitle").FirstOrDefault().Value;
            var domainSpecialty = result.Where(i => i.Key == "domainspecialty").FirstOrDefault().Value;
            var industrySpecialty = result.Where(i => i.Key == "industryspecialtydescr").FirstOrDefault().Value;
            var firstSecondarySpecialty = result.Where(i => i.Key == "firstsecondaryspecialtydescr").FirstOrDefault().Value;
            var secondSecondarySpecialty = result.Where(i => i.Key == "secondsecondaryspecialtydescr").FirstOrDefault().Value;
            var standardJobCode = result.Where(i => i.Key == "standardjobcd").FirstOrDefault().Value;
            var timezone = result.Where(i => i.Key == "timezone").FirstOrDefault().Value;
            var bio = result.Where(i => i.Key == "professionalbio").FirstOrDefault().Value;
            var orglevel2desc = result.Where(i => i.Key == "orglevel2desc").FirstOrDefault().Value;
            var currentProjects = result.Where(i => i.Key == "currentprojects").FirstOrDefault().Value;
            var currentClient = result.Where(i => i.Key == "currentclient").FirstOrDefault().Value;
            var profile = new
            {
                EnterpriseId = enterpriseId,
                PeopleKey = peopleKey,
                DisplayName = displayName,
                Avatar = avatarData,
                Email = email,
                WorkEmail = workEmail,
                Phone = phone,
                WorkPhone = workPhone,
                Mobile = mobile,
                Country = country,
                CountryHome = countryHome,
                City = city,
                HomeCity = homeCity,
                Location = location,
                CurrentLocation = currentLocation,
                TalentSegment = talentSegment,
                JobTitle = jobTitle,
                CareerTrack = careerTrack,
                CareerLevel = careerLevel,
                DomainSpecialty = domainSpecialty,
                IndustrySpecialty = industrySpecialty,
                FirstSecondarySpecialty = firstSecondarySpecialty,
                SecondSecondarySpecialty = secondSecondarySpecialty,
                StandardJobCode = standardJobCode,
                Timezone = timezone,
                Bio = bio,
                Orglevel2desc = orglevel2desc,
                CurrentProjects = currentProjects,
                CurrentClient = currentClient

            };

            return profile;
        }


        private class ObjectPassed
        {
            public object Item { get; set; }
            public object Result { get; set; }

        }

        private dynamic LoadCachedAvatar(string eid)
        {
            var response = Task.Run(() => GetPeopleProfile(eid)).Result;

            JObject jsob = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            var result = JsonConvert.DeserializeObject<List<KeyValue>>(jsob.GetValue("CupsProfile").ToString()) as IList<KeyValue>;


            string avatar = string.Empty;

            try
            {
                var bytes = Task.Run(() => GetAvatar(eid)).Result;
                avatar = Convert.ToBase64String(bytes).ToString();
            }
            catch (Exception ex)
            {
                Logger.Error("The person's avatar has some problem, please make sure it is a valid picture." + ex.Message);
            }

            var enterpriseId = result.Where(i => i.Key == "enterpriseid").FirstOrDefault().Value;
            var peopleKey = result.Where(i => i.Key == "peoplekey").FirstOrDefault().Value;
            var data = avatar;

            return new
            {
                EnterpriseId = enterpriseId,
                PeopleKey = peopleKey,
                Data = data
            };
        }
        private void Initialize()
        {
            var part = _siteService.GetSiteSettings().As<PeopleServiceSettingsPart>();
            var baseUrl = string.IsNullOrEmpty(part.BaseUrl) ? "https://collabhub.accenture.com/" : part.BaseUrl;
            var domain = string.IsNullOrEmpty(part.Domain) ? "dir" : part.Domain;
            var userName = string.IsNullOrEmpty(part.UserName) ? "3747_CareerExplorer" : part.UserName;
            var password = string.IsNullOrEmpty(part.Password) ? "iP4gE@aB5lY1mY5" : part.Password;
            var credentials = new NetworkCredential(userName, password, domain);

            var handler = new HttpClientHandler { Credentials = credentials };
            _client = new HttpClient(handler);
            _client.BaseAddress = new Uri(baseUrl);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        private async Task<HttpResponseMessage> GetPeopleProfile(string eid)
        {
            Initialize();
            return await _client.GetAsync(string.Format("json/People/{0}", eid));
        }
        private async Task<byte[]> GetAvatar(string eid)
        {
            Initialize();
            return await _client.GetByteArrayAsync(string.Format("People/ProfilePicture/{0}", eid));
        }
        private class KeyValue
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
        #endregion
    }
}