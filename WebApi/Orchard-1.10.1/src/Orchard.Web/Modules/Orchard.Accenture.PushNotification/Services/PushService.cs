using Orchard.Accenture.PushNotification.Common;
using Orchard.Accenture.PushNotification.Models;
using Orchard.Accenture.Event.Models;
using Orchard.Core.Common.Models;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Settings;
using Orchard.Security;
using Orchard.Taxonomies.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;


namespace Orchard.Accenture.PushNotification.Services
{
    public class PushService : IPushService
    {
        private readonly ISiteService _siteService;
        private readonly IContentManager _contentManager;
        private readonly IOrchardServices _orchardServices;
        private readonly ITaxonomyService _taxonomyService;
        private readonly IRestClientUtility _utility;           

        public PushService(
            ISiteService siteService,
            IContentManager contentManager,
            IOrchardServices orchardServices,
            ITaxonomyService taxonomyService, 
            IRestClientUtility utility)
        {
            _siteService = siteService;
            _contentManager = contentManager;
            _orchardServices = orchardServices;
            _taxonomyService = taxonomyService;
            _utility = utility;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }         

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }
        public void Notify(NotificationMessage message)
        {
            //1.Get settings
            var part = _siteService.GetSiteSettings().As<PushServiceSettingsPart>();
            var userName = part.UserName;
            var password = part.Password;
            var scope = part.Scope;
            var endpoint = part.Endpoint;

            var pushTenant = part.PushTenant;
            var pushTopic = part.PushTopic;

            //2.Get token by setting
            string token = string.Empty;

            try 
            {
                token = _utility.GetToken(userName, password, scope, endpoint);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

            try
            {
                //2.Notify
                //_utility.Notify(token, pushTenant, pushTopic, message);
                _utility.NotifyAsync(scope,token, pushTenant, pushTopic, message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void Notify(NotificationMessage message,string tenant,string topic)
        {
            //1.Get settings
            var part = _siteService.GetSiteSettings().As<PushServiceSettingsPart>();
            var userName = part.UserName;
            var password = part.Password;
            var scope = part.Scope;
            var endpoint = part.Endpoint;

            //2.Get token by setting
            string token = string.Empty;

            try
            {
                token = _utility.GetToken(userName, password, scope, endpoint);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

            try
            {
                //2.Notify
                _utility.Notify(scope,token, tenant, topic, message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void RawNotify(string location, RawNotificationMessage message)
        {
            //1.Get settings
            var part = _siteService.GetSiteSettings().As<PushServiceSettingsPart>();
            var userName = part.UserName;
            var password = part.Password;
            var scope = part.Scope;
            var endpoint = part.Endpoint;

            var pushTenant = part.PushTenant;
            var pushTopic = part.PushTopic;

            //2.Get token by setting
            var token = _utility.GetToken(userName, password, scope, endpoint);

            //2.Notify
            _utility.RawNotify(scope,token, pushTenant, pushTopic, message);
        }

        //Get all the events in the database
        public IOrderedEnumerable<ContentManagement.ContentItem> GetEvents()
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser;  

            var query = _contentManager.Query(VersionOptions.Published, "Event")
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id);                       

            IOrderedEnumerable<ContentManagement.ContentItem> result = query.List().OrderBy(p => ((dynamic)p).TitlePart.Title);
            return result;  
        }        

        //Get all the added participants of the selected event.
        public List<String> GetEventParticipants(int eventId)
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser;

            var query = _contentManager.Query(VersionOptions.Published, "Participant")
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id)
                .Where<ParticipantPartRecord>(p => 
                    p.EventIds.StartsWith(eventId.ToString() + ",")
                    || p.EventIds.Contains("," + eventId.ToString() + ",")
                    || p.EventIds.EndsWith("," + eventId.ToString())
                    || (!p.EventIds.Contains(",") && p.EventIds == eventId.ToString())); 
            var results = query.List();

            List<String> EventsParticipants = new List<String>();
            
            foreach (var item in results)
            {
                if (!((((dynamic)item).VersionRecord != null)
                    && ((((dynamic)item).VersionRecord.Published == false)
                    || (((dynamic)item).VersionRecord.Published && ((dynamic)item).VersionRecord.Latest == false))))
                {
                    List<String> EIDs = item.As<ParticipantPart>().EnterpriseId.Split(',').ToList();                    
                    foreach (var term in EIDs)
                    {
                        EventsParticipants.Add(term);
                        GetPeopleKeys(term);
                    }
                }
            }

            return EventsParticipants;
        }

        //Check if the participants are included in the event 
        public List<String> CheckParticipants(List<String> SelectedList, int eventId)
        {
            List<String> EventParticipants = GetEventParticipants(eventId);
            List<String> PushParticipants = new List<String>();

            foreach (var participants in EventParticipants)
            {
                foreach (var member in SelectedList)
                {
                    if (member == participants)
                    {
                        PushParticipants.Add(member);
                    }
                }
            }
            return PushParticipants;
        }

        //Get the peoplekeys of the participants
        public String GetPeopleKeys(string EID)
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser;

            var query = _contentManager.Query(VersionOptions.Published, "Participant")
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id)
                .Where<ParticipantPartRecord>(p => p.EnterpriseId == EID);
            var results = query.List();

            List<String> PeopleKeys = new List<String>();

            foreach (var item in results)
            {
                if (!((((dynamic)item).VersionRecord != null)
                    && ((((dynamic)item).VersionRecord.Published == false)
                    || (((dynamic)item).VersionRecord.Published && ((dynamic)item).VersionRecord.Latest == false))))
                {
                    string termNames = item.As<ParticipantPart>().PeopleKey;                   
                    PeopleKeys.Add(termNames);                    
                }
            }
            if (PeopleKeys.Count > 0)
            {
                return PeopleKeys.FirstOrDefault();
            }
            else
            {
                return "";
            }
            
        }

        //Get all the AD Groups of the event
        public List<String> GetEventADGroups(int eventId) 
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser;

            var query = _contentManager.Query(VersionOptions.Published, "Event")
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id)                
                .Where<EventPartRecord>(p => p.Id == eventId);

            var results = query.List();    
            
            List<String> EventsADGroup = new List<String>();

            foreach (var item in results)
            {
                if (!((((dynamic)item).VersionRecord != null)
                    && ((((dynamic)item).VersionRecord.Published == false)
                    || (((dynamic)item).VersionRecord.Published && ((dynamic)item).VersionRecord.Latest == false))))
                {
                    List<String> termNames = item.As<EventPart>().ADGroups.Split(',').ToList();
                    foreach (var term in termNames)
                    {                        
                        EventsADGroup.Add(term);
                    }          
                }
            }

            return EventsADGroup;
        }

        //Get all the AD Group of the selected session
        public List<String> GetSessionADGroups(int eventId, int sessionId)
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser; 

            var query = _contentManager.Query(VersionOptions.Published, "Session")          
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id)
                .Where<SessionPartRecord>(p =>
                    p.AgendaEventPickerIds.StartsWith(eventId.ToString() + ",")
                    || p.AgendaEventPickerIds.Contains("," + eventId.ToString() + ",")
                    || p.AgendaEventPickerIds.EndsWith("," + eventId.ToString())
                    || (!p.AgendaEventPickerIds.Contains(",") && p.AgendaEventPickerIds == eventId.ToString()));

            var results = query.List();           
           
            List<String> SessionADGroup = new List<String>();            

            foreach (var item in results)
            {
                if (!((((dynamic)item).VersionRecord != null)
                    && ((((dynamic)item).VersionRecord.Published == false)
                    || (((dynamic)item).VersionRecord.Published && ((dynamic)item).VersionRecord.Latest == false))))
                {
                    if (((dynamic)item).Id == sessionId)
                    {                        
                        List<String> termNames = item.As<SessionPart>().AgendaADGroups.Split(',').ToList();
                        foreach (var term in termNames)
                        {                            
                            SessionADGroup.Add(term);
                        }
                    }
                }
            }
            return SessionADGroup;
        }

        //Get all the AD Group of the selected circle
        public List<String> GetCirclesADGroups(int eventId, int circleId)
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser;

            var query = _contentManager.Query(VersionOptions.Published, "Circle")
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id)
                .Where<CirclePartRecord>(p =>
                    p.EventPickerIds.StartsWith(eventId.ToString() + ",")
                    || p.EventPickerIds.Contains("," + eventId.ToString() + ",")
                    || p.EventPickerIds.EndsWith("," + eventId.ToString())
                    || (!p.EventPickerIds.Contains(",") && p.EventPickerIds == eventId.ToString()));

            var results = query.List(); 

            List<String> CircleADGroup = new List<String>();            

            foreach (var item in results)
            {
                if (!((((dynamic)item).VersionRecord != null)
                    && ((((dynamic)item).VersionRecord.Published == false)
                    || (((dynamic)item).VersionRecord.Published && ((dynamic)item).VersionRecord.Latest == false))))
                {
                    if (((dynamic)item).Id == circleId)
                    {
                        List<String> termNames = item.As<CirclePart>().AdGroups.Split(',').ToList();
                        foreach (var term in termNames)
                        {                            
                            CircleADGroup.Add(term);
                        }
                    }
                }
            }
            return CircleADGroup;
        }
        
        //Get all the members of the selected AD Group in the directory.accenture.com
        public List<String> GetMemberOfADGroup(string ADgroup)
        {             
            Domain domain = Domain.GetCurrentDomain();
            var de = domain.GetDirectoryEntry();
            var ds = new DirectorySearcher { SearchRoot = de };
            string strUserFilter = "(&(objectCategory=user)(memberOf=cn=" + ADgroup + ",OU=Groups,DC=dir,DC=svc,DC=accenture,DC=com))";
            ds.Filter = strUserFilter;
            
            DirectoryEntry user = null;
            List<String> ADGroupMembers = new List<String>();

            foreach (SearchResult result in ds.FindAll())
            {
                user = result.GetDirectoryEntry();
                string str = user.Name;
                ADGroupMembers.Add(str.Substring(3));
                //ADGroupMembers.Add(str.Substring(3)+ ' ' + ADgroup);                
            }
            return ADGroupMembers;
        }

        //Get all the session of the selected event
        public dynamic GetSessions(int eventId)
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser;            

            var query = _contentManager.Query(VersionOptions.Published, "Session")          
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id)
                .Where<SessionPartRecord>(p =>
                    p.AgendaEventPickerIds.StartsWith(eventId.ToString() + ",")
                    || p.AgendaEventPickerIds.Contains("," + eventId.ToString() + ",")
                    || p.AgendaEventPickerIds.EndsWith("," + eventId.ToString())
                    || (!p.AgendaEventPickerIds.Contains(",") && p.AgendaEventPickerIds == eventId.ToString()));

            var results = query.List();           
            return results;
        }
       
        //Get all the circles of the selected event
        public dynamic GetCircles(int eventId)
        {
            bool isSiteOwner = _orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner);
            IUser owner = _orchardServices.WorkContext.CurrentUser;  

            var query = _contentManager.Query(VersionOptions.Published, "Circle")
                .Where<CommonPartRecord>(cr => isSiteOwner ? true : cr.OwnerId == owner.Id)
                .Where<CirclePartRecord>(p =>
                    p.EventPickerIds.StartsWith(eventId.ToString() + ",")
                    || p.EventPickerIds.Contains("," + eventId.ToString() + ",")
                    || p.EventPickerIds.EndsWith("," + eventId.ToString())
                    || (!p.EventPickerIds.Contains(",") && p.EventPickerIds == eventId.ToString()));

            var results = query.List();            
            return results;
        }
    }
}
