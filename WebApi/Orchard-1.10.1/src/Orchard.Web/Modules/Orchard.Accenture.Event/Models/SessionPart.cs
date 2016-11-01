using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using System.Collections.Generic;
using System;

namespace Orchard.Accenture.Event.Models
{
    public class SessionPart : ContentPart<SessionPartRecord>
    {
        public string AgendaTitle
        {
            get { return Retrieve(r => r.AgendaTitle); }
            set { Store(r => r.AgendaTitle, value); }
        }
        public string AgendaStartTime
        {
            get { return Retrieve(r => r.AgendaStartTime); }
            set { Store(r => r.AgendaStartTime, value); }
        }
        public string AgendaEndTime
        {
            get { return Retrieve(r => r.AgendaEndTime); }
            set { Store(r => r.AgendaEndTime, value); }
        }
        public string AgendaType
        {
            get { return Retrieve(r => r.AgendaType); }
            set { Store(r => r.AgendaType, value); }
        }
        public string AgendaCategory
        {
            get { return Retrieve(r => r.AgendaCategory); }
            set { Store(r => r.AgendaCategory, value); }
        }
        public string AgendaADGroups
        {
            get { return Retrieve(r => r.AgendaADGroups); }
            set { Store(r => r.AgendaADGroups, value); }
        }
        public string AgendaFullDescription
        {
            get { return Retrieve(r => r.AgendaFullDescription); }
            set { Store(r => r.AgendaFullDescription, value); }
        }
        public string AgendaPresenterPickerIds
        {
            get { return Retrieve(r => r.AgendaPresenterPickerIds); }
            set { Store(r => r.AgendaPresenterPickerIds, value); }
        }
        public string AgendaEventPickerIds
        {
            get { return Retrieve(r => r.AgendaEventPickerIds); }
            set { Store(r => r.AgendaEventPickerIds, value); }
        }

        public bool SessionIsPublished
        {
            get { return Retrieve(r => r.SessionIsPublished); }
            set { Store(r => r.SessionIsPublished, value); }
        }
        public bool SessionIsLatest
        {
            get { return Retrieve(r => r.SessionIsLatest); }
            set { Store(r => r.SessionIsLatest, value); }
        }
    }
}
