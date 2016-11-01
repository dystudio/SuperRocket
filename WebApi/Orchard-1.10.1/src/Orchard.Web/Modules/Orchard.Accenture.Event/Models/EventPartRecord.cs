using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class EventPartRecord : ContentPartRecord
    {
        public EventPartRecord()
        {

        }
        public virtual String StartDate { get; set; }
        public virtual String EndDate { get; set; }
        public virtual String Location { get; set; }
        public virtual String LocationDescription { get; set; }
        [StringLengthMax]
        public virtual String IntroduceVideoPlayer { get; set; }
        public virtual String IntroduceVideoSubject { get; set; }
        public virtual String IntroduceVideoDescription { get; set; }
        public virtual String SubTitle { get; set; }
        public virtual String Description { get; set; }
        public virtual String CircleID { get; set; }
        public virtual String CircleGUID { get; set; }
        public virtual String EventTitle { get; set; }
        public virtual String AppPickerIds { get; set; }
        [StringLengthMax]
        public virtual String ADGroups { get; set; }
        public virtual String CoverImageUrl { get; set; }
        public virtual String VideoCoverImageUrl { get; set; }
        public virtual String SkincssUrl { get; set; }
        [StringLengthMax]
        public virtual String ParticipantLayoutFullPath { get; set; }
        [StringLengthMax]
        public virtual String DocumentLayoutFullPath { get; set; }
        public virtual String ContactPickerIds { get; set; }
        public virtual bool EventIsPublished { get; set; }
        public virtual bool EventIsLatest { get; set; }

    }
}