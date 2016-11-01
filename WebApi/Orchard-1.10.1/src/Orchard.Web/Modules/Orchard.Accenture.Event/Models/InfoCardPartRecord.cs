using Orchard.ContentManagement.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class InfoCardPartRecord : ContentPartRecord
    {
        public virtual string HotelName { get; set; }
        public virtual string HotelAddress { get; set; }
        public virtual string WebSite { get; set; }
        public virtual string Telphone { get; set; }
        public virtual string ExtNumber { get; set; }
        public virtual string Title { get; set; }
        public virtual string CardStartDate { get; set; }
        public virtual string CardEndDate { get; set; }
        public virtual string CardCoverImageUrl { get; set; }
        public virtual string EventPickerIds { get; set; }
        public virtual bool InfoCardIsPublished { get; set; }
        public virtual bool InfoCardIsLatest { get; set; }
    }
}