using Orchard.ContentManagement.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class CirclePartRecord : ContentPartRecord
    {
        public CirclePartRecord()
        {

        }
        public virtual String Title { get; set; }
        public virtual String AnotherCircleId { get; set; }
        public virtual String AnotherCircleGUID { get; set; }
        public virtual String EventPickerIds { get; set; }
        public virtual String AdGroups { get; set; }
        public virtual bool CircleIsPublished { get; set; }
        public virtual bool CircleIsLatest { get; set; }

    }
}