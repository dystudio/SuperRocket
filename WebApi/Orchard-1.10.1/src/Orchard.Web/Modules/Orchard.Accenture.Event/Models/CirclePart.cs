using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using System.Collections.Generic;
using System;

namespace Orchard.Accenture.Event.Models
{
    public class CirclePart : ContentPart<CirclePartRecord>
    {
        public String Title
        {
            get { return Retrieve(r => r.Title); }
            set { Store(r => r.Title, value); }
        }

        public String AnotherCircleId
        {
            get { return Retrieve(r => r.AnotherCircleId); }
            set { Store(r => r.AnotherCircleId, value); }
        }

        public String AnotherCircleGUID
        {
            get { return Retrieve(r => r.AnotherCircleGUID); }
            set { Store(r => r.AnotherCircleGUID, value); }
        }

       
        public String EventPickerIds
        {
            get { return Retrieve(r => r.EventPickerIds); }
            set { Store(r => r.EventPickerIds, value); }
        }

        public String AdGroups
        {
            get { return Retrieve(r => r.AdGroups); }
            set { Store(r => r.AdGroups, value); }
        }

        public bool CircleIsPublished
        {
            get { return Retrieve(r => r.CircleIsPublished); }
            set { Store(r => r.CircleIsPublished, value); }
        }
        public bool CircleIsLatest
        {
            get { return Retrieve(r => r.CircleIsLatest); }
            set { Store(r => r.CircleIsLatest, value); }
        }
    }
}
