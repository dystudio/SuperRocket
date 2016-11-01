using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class SessionPartRecord : ContentPartRecord
    {
        public SessionPartRecord()
        {

        }
        public virtual string AgendaTitle { get; set; }

        public virtual string AgendaStartTime { get; set; }

        public virtual string AgendaEndTime { get; set; }

        public virtual string AgendaType { get; set; }

        public virtual string AgendaCategory { get; set; }
        [StringLengthMax]
        public virtual string AgendaADGroups { get; set; }
        [StringLengthMax]
        public virtual string AgendaFullDescription { get; set; }

        public virtual string AgendaPresenterPickerIds { get; set; }

        public virtual string AgendaEventPickerIds { get; set; }

        public virtual bool SessionIsPublished { get; set; }
        public virtual bool SessionIsLatest { get; set; }
    }
}