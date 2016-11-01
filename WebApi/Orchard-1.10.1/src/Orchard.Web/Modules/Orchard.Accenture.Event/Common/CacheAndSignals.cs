using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Common
{
    public class CacheAndSignals
    {
          public const string EventSignal = "Orchard.Accenture.Event.Event.Signal";
          public const string EventCache = "Orchard.Accenture.Event.Event.Cache";


        #region CACHE
        public const string ODATA_BASE_URL_CACHE = "Orchard.Accenture.Event.OdataBaseUrl";

        public const string APP_CACHE = "Orchard.Accenture.Event.App";
        public const string DOCUMENT_CACHE = "Orchard.Accenture.Event.Document";
        public const string SESSION_CACHE = "Orchard.Accenture.Event.Session";
        public const string EVENT_CACHE = "Orchard.Accenture.Event.Event";
        public const string PARTICIPANT_CACHE = "Orchard.Accenture.Event.Participant";
        public const string PARTICIPANS_CACHE_FOR_EVENT = "Orchard.Accenture.Event.ParticipantsForEvent";
        public const string PARTICIPANT_LAYOUT_CACHE = "Orchard.Accenture.Event.ParticipantLayout";
        public const string INFORCARDS_CACHE = "Orchard.Accenture.Event.InfoCards";
        public const string POOLS_CACHE = "Orchard.Accenture.Event.Pools";
        public const string EVALUATIONS_CACHE = "Orchard.Accenture.Event.Evaluations";
        public const string AVATAR_CACHE = "Orchard.Accenture.Event.Avatar";
        public const string PROFILE_CACHE = "Orchard.Accenture.Event.Profile";
        public const string PEOPLE_AVATAR_CACHE = "Orchard.Accenture.Event.People.Avatar";

        public const string APP_CACHE_SIGNAL = "Orchard.Accenture.Event.App.Changed";
        public const string DOCUMENT_CACHE_SIGNAL = "Orchard.Accenture.Event.Document.Changed";
        public const string SESSION_CACHE_SIGNAL = "Orchard.Accenture.Event.Session.Changed";
        public const string EVENT_CACHE_SIGNAL = "Orchard.Accenture.Event.Event.Changed";
        public const string PARTICIPANT_CACHE_SIGNAL = "Orchard.Accenture.Event.Participant.Changed";
        public const string PARTICIPANT_LAYOUT_CACHE_SIGNAL = "Orchard.Accenture.Event.ParticipantLayout.Changed";
        public const string INFORCARDS_CACHE_SIGNAL = "Orchard.Accenture.Event.InfoCards.Changed";
        public const string POOLS_CACHE_SIGNAL = "Orchard.Accenture.Event.Pools.Changed";
        public const string EVALUATIONS_CACHE_SIGNAL = "Orchard.Accenture.Event.Evaluations.Changed";
        public const string AVATAR_CACHE_SIGNAL = "Orchard.Accenture.Event.Avatar.Changed";
        public const string PROFILE_CACHE_SIGNAL = "Orchard.Accenture.Event.Profile.Changed";
        public const string PEOPLE_AVATAR_CACHE_SIGNAL = "Orchard.Accenture.Event.People.Avatar.Changed";

        #endregion
    }
}