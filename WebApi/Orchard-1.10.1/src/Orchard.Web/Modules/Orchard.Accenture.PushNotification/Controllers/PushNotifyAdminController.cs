using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;
using Orchard.Core.Contents.ViewModels;
using Orchard.Security;
using Orchard.Accenture.PushNotification.Services;
using Orchard.Accenture.PushNotification.Models;
using Orchard.Accenture.PushNotification.ViewModels;

namespace Orchard.Accenture.PushNotification.Controllers
{
    [Admin]
    public class PushNotifyAdminController : Controller {

        private readonly IContentManager _contentManager;
        private readonly ISiteService _siteService;
        private readonly ITransactionManager _transactionManager;
        private readonly IPushService _pushService;
        private readonly IEnumerable<ContentItem> _events;
        public IEnumerable<ContentItem> eventlist
        {
            get
            {
                if (_events == null)
                    return _pushService.GetEvents();
                else
                    return null;
            }
        }                      
        
        public Localizer T { get; set; }
        public IOrchardServices Services { get; private set; }

        public PushNotifyAdminController(
            IContentManager contentManager,
            IShapeFactory shapeFactory,
            ISiteService siteService,
            ITransactionManager transactionManager,
            IOrchardServices orchardServices,
            IPushService pushService) {

            _contentManager = contentManager;
            _siteService = siteService;
            _transactionManager = transactionManager;
            Services = orchardServices;
            _pushService = pushService;

            T = NullLocalizer.Instance;
        }
        [HttpGet, ActionName("PushNotification")]
        public ActionResult PushNotification()
        {
            var viewModel = new NotificationMessageViewModel();
            var events = eventlist;
            bool hasError = false;

            if(events.Count() > 0)
            {
                viewModel.Events = events;
            }
            else
            {
                hasError = ErrorNotifier("No Event Found.", viewModel, out hasError);
            }
            return View(viewModel);
           
        }
        [HttpGet, ActionName("GetSessions")]
        public ActionResult GetSessions(string selectedEvent)
        {
            
            List<SelectListItem> sessionNames = new List<SelectListItem>();
            if (!String.IsNullOrEmpty(selectedEvent) && selectedEvent != "undefined")
            {
                var viewModel = new NotificationMessageViewModel();
                int eventID = Convert.ToInt32(selectedEvent);               
                var sessions = _pushService.GetSessions(eventID);
                sessionNames.Add(new SelectListItem { Text = "All Sessions", Value = "0" });
                foreach (var item in sessions)
                {
                    if (!((((dynamic)item).VersionRecord != null)
                        && ((((dynamic)item).VersionRecord.Published == false)
                        || (((dynamic)item).VersionRecord.Published && ((dynamic)item).VersionRecord.Latest == false))))
                    {
                        sessionNames.Add(new SelectListItem { Text = item.TitlePart.Title, Value = item.Id.ToString() });
                    }
                }
                
            }
            return Json(sessionNames, JsonRequestBehavior.AllowGet);
        }
        [HttpGet, ActionName("GetCircles")]
        public ActionResult GetCircles(string selectedEvent)
        {
            List<SelectListItem> circleNames = new List<SelectListItem>();
            if (!String.IsNullOrEmpty(selectedEvent) && selectedEvent != "undefined")
            {
                var viewModel = new NotificationMessageViewModel();
                int eventID = Convert.ToInt32(selectedEvent);                
                var circles = _pushService.GetCircles(eventID);
                circleNames.Add(new SelectListItem { Text = "All Circles", Value = "0" });
                foreach (var item in circles)
                {
                    if (!((((dynamic)item).VersionRecord != null)
                        && ((((dynamic)item).VersionRecord.Published == false)
                        || (((dynamic)item).VersionRecord.Published && ((dynamic)item).VersionRecord.Latest == false))))
                    {
                        circleNames.Add(new SelectListItem { Text = item.TitlePart.Title, Value = item.Id.ToString() });
                    }
                }
            }
            
            return Json(circleNames, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ActionName("PushNotification")]
        public ActionResult PushNotificationPOST(NotificationMessageViewModel model)
        {
            #region Declarations
            var events = eventlist; 
            string title = null;
            string message = null;
            bool hasError = false;
            int eventId = 0;
            int sessionId = 0;
            int circleId = 0;                       
                        
            List<String> EventADGroupMembers = new List<String>();                         
            List<String> EventPushParticipants = new List<String>();
            List<String> EventPeopleKeys = new List<String>();

            List<String> SessionADGroupMembers = new List<String>();
            List<String> SessionPushParticipants = new List<String>();
            List<String> SessionPeopleKeys = new List<String>();

            List<String> CircleADGroupMembers = new List<String>();
            List<String> CirclePushParticipants = new List<String>();
            List<String> CirclePeopleKeys = new List<String>();

            List<String> NonParticipants = new List<String>();
            List<String> FinalPeopleKeys = new List<String>();
            List<String> CleanEids = new List<String>();            

            #endregion

            // Get the ID of the Selected Event
            eventId = Convert.ToInt32(Request.Form["event-picker"]);
            // Get the ID of the Selected Session
            sessionId = Convert.ToInt32(Request.Form["session-picker"]);
            // Get the ID of the Selected Circle
            circleId = Convert.ToInt32(Request.Form["circle-picker"]);
            // Get the Title of the Push Notification
            title = Request.Form["Title"].ToString();
            // Get the Message to be sent to the Participants
            message = Request.Form["Message"].ToString();

            // Check if there is a Title 
            if (String.IsNullOrWhiteSpace(title) == false && String.IsNullOrWhiteSpace(message) == false)
            {
                // Check if there is an event selected
                if ((eventId != 0 && sessionId == 0) && (eventId != 0 && circleId == 0))
                {
                    // Get the AD Groups of the event
                    List<String> SelectedEvent = _pushService.GetEventADGroups(eventId);
                    // Get the Members of the AD Groups
                    EventADGroupMembers = GetADGroupMembers(SelectedEvent);
                    // Check if the Event Participants are in the AD Group Participants                                  
                    EventPushParticipants = _pushService.CheckParticipants(EventADGroupMembers, eventId);
                    // Check if there are Participants in the selected event
                    if (EventPushParticipants.Count() > 0)
                    {
                        foreach (var participants in EventPushParticipants)
                        {
                            // Get the Peoplekey of the Participants of the event
                            string peoplekey = _pushService.GetPeopleKeys(participants);
                            EventPeopleKeys.Add(peoplekey);
                        }
                        // The PeopleKeys of the participants to be notified
                        FinalPeopleKeys = EventPeopleKeys;
                    }
                    else
                    {
                        hasError = ErrorNotifier("No Participants Available in this Event.", model, out hasError);
                    }
                }

                // Check if there is an event and session selected
                if (eventId != 0 && sessionId != 0)
                {
                    // Get the AD Groups of the event
                    List<String> SelectedSession = _pushService.GetSessionADGroups(eventId, sessionId);
                    // Get the Members of the AD Groups
                    SessionADGroupMembers = GetADGroupMembers(SelectedSession);
                    // Check if the Session Participants are in the AD Group Participants
                    SessionPushParticipants = _pushService.CheckParticipants(SessionADGroupMembers, eventId);
                    // Check if there are Participants in the selected session
                    if (SessionPushParticipants.Count() > 0 && sessionId != 0)
                    {
                        foreach (var participants in SessionPushParticipants)
                        {
                            // Get the Peoplekey of the Participants of the event
                            string peoplekey = _pushService.GetPeopleKeys(participants);
                            SessionPeopleKeys.Add(peoplekey);
                        }
                        // The PeopleKeys of the participants to be notified
                        FinalPeopleKeys = SessionPeopleKeys;
                    }
                    else
                    {
                        hasError = ErrorNotifier("No Participant Available in this Session.", model, out hasError);
                    }
                }

                // Check if there is an event and circle selected
                if (eventId != 0 && circleId != 0)
                {
                    // Get the AD Groups of the event
                    List<String> SelectedCircle = _pushService.GetCirclesADGroups(eventId, circleId);
                    // Get the Members of the AD Groups
                    CircleADGroupMembers = GetADGroupMembers(SelectedCircle);
                    // Check if the Circle Participants are in the AD Group Participants
                    CirclePushParticipants = _pushService.CheckParticipants(CircleADGroupMembers, eventId);
                    // Check if there are Participants in the selected circle
                    if (CirclePushParticipants.Count() > 0 && circleId != 0)
                    {
                        foreach (var participants in CirclePushParticipants)
                        {
                            // Get the Peoplekey of the Participants of the event
                            string peoplekey = _pushService.GetPeopleKeys(participants);
                            CirclePeopleKeys.Add(peoplekey);
                        }
                        // The PeopleKeys of the participants to be notified
                        FinalPeopleKeys = CirclePeopleKeys;
                    }
                    else
                    {
                        hasError = ErrorNotifier("No Participant Available in this Circle.", model, out hasError);
                    }
                }

                // Check if the Single Push is selected
                if (events.Count() > 0)
                {
                    if (eventId == 0)
                    {
                        // Get the inputed EIDS                           
                        var EID = Request.Form["EIDs"].ToString();
                        var EIDs = EID.Split(',').ToList();
                        if (String.IsNullOrWhiteSpace(EID) == false)
                        {
                            foreach (var item in EIDs)
                            {
                                // Get the Peoplekey of the inputed participants
                                string peoplekey = _pushService.GetPeopleKeys(item.Trim());
                                if (peoplekey == "")
                                {
                                    NonParticipants.Add(item);
                                }
                                else
                                {
                                    CleanEids.Add(peoplekey);
                                }
                            }
                            // The PeopleKeys of the inputed participants to be notified
                            FinalPeopleKeys = CleanEids;
                        }
                        else
                        {
                            hasError = ErrorNotifier("No Enterprise ID entered.", model, out hasError);
                        }
                    }                                        
                }
                else
                {
                    hasError = ErrorNotifier("No Event Found.", model, out hasError);
                }

                if (NonParticipants.Count() > 0)
                {
                    string NotFoundEIDs = String.Join(",", NonParticipants.Select(x => x));
                    hasError = ErrorNotifier("The inputed EIDS <b>" + NotFoundEIDs + "</b> are NOT found in the participants.", model, out hasError);
                }
                // How many PeopleKeys are available
                var count = FinalPeopleKeys.Count;

                
                // Check if there are no errors encountered
                if (hasError == false && events.Count() > 0)
                {
                    // Submits to the service 5 PeopleKeys at a time
                    int take = 5, skip = 0;
                    while (count > skip)
                    {
                        List<String> items = FinalPeopleKeys.OrderByDescending(x => x).Skip(skip).Take(take).ToList();
                        Request req = AssignRequest(items, title, message);

                        List<Request> requestList = new List<Request>();
                        requestList.Add(req);

                        PushRequest(requestList);
                        skip = skip + 5;
                    }

                    var viewModels = new NotificationMessageViewModel();
                    viewModels.Result = string.Format("The message was successfully been sent.", model.Title);
                    Services.Notifier.Information(T(viewModels.Result));
                }

            }
            else
            {
                var viewError = new NotificationMessageViewModel();
                viewError.Error = string.Format("There is NO Title/Message to be sent to the participants.", model.Title);
                Services.Notifier.Error(T(viewError.Error));                
            }                                                                           

            var viewModel = new NotificationMessageViewModel();           
            
            //For Events Picker
            viewModel.Events = _pushService.GetEvents();
            viewModel.CurrentEventId = eventId;
            
            //For Sessions Picker
            viewModel.Sessions = _pushService.GetSessions(eventId);                 

            return View(viewModel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="model"></param>
        /// <param name="hasError"></param>
        /// <returns></returns>
        private bool ErrorNotifier(string message ,NotificationMessageViewModel model, out bool hasError)
        {
            var viewError = new NotificationMessageViewModel();
            viewError.Error = string.Format(message, model.Title);
            Services.Notifier.Error(T(viewError.Error));
            hasError = true;
            return hasError;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventItems"></param>
        /// <returns></returns>
        private List<String> GetADGroupMembers(List<String> eventItems)
        {
            List<String> ADGroupMembers = new List<String>();

            foreach (var adgroups in eventItems)
            {
                List<string> members = _pushService.GetMemberOfADGroup(adgroups);

                if (members.Count() > 0)
                {
                    if (ADGroupMembers.Count() == 0)
                    {
                        ADGroupMembers = _pushService.GetMemberOfADGroup(adgroups);
                    }
                    else
                    {
                        ADGroupMembers = ADGroupMembers.Concat(members).ToList();
                    }
                }
            }
            return ADGroupMembers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private Request AssignRequest(List<string> items, string title, string message)
        {
            Request request = new Request();
            request.BadgeCount = "1";
            request.Title = title;
            request.Message = message;
            request.To = items;
            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestList"></param>
        private void PushRequest(List<Request> requestList)
        {
            NotificationMessage notificationMessage = new NotificationMessage();
            notificationMessage.request = requestList;
            _pushService.Notify(notificationMessage);
        }
    }
}
