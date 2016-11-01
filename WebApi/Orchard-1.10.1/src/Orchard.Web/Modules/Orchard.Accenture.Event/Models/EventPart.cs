using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using System.Collections.Generic;
using System;

namespace Orchard.Accenture.Event.Models
{
    public class EventPart : ContentPart<EventPartRecord>
    {
        public String StartDate
        {
            get { return Retrieve(r => r.StartDate); }
            set { Store(r => r.StartDate, value); }
        }

        public String EndDate
        {
            get { return Retrieve(r => r.EndDate); }
            set { Store(r => r.EndDate, value); }
        }

        public String Location
        {
            get { return Retrieve(r => r.Location); }
            set { Store(r => r.Location, value); }
        }

        public String LocationDescription
        {
            get { return Retrieve(r => r.LocationDescription); }
            set { Store(r => r.LocationDescription, value); }
        }

        public String IntroduceVideoPlayer
        {
            get { return Retrieve(r => r.IntroduceVideoPlayer); }
            set { Store(r => r.IntroduceVideoPlayer, value); }
        }

        public String IntroduceVideoSubject
        {
            get { return Retrieve(r => r.IntroduceVideoSubject); }
            set { Store(r => r.IntroduceVideoSubject, value); }
        }

        public String IntroduceVideoDescription
        {
            get { return Retrieve(r => r.IntroduceVideoDescription); }
            set { Store(r => r.IntroduceVideoDescription, value); }
        }

        public String SubTitle
        {
            get { return Retrieve(r => r.SubTitle); }
            set { Store(r => r.SubTitle, value); }
        }

        public String Description
        {
            get { return Retrieve(r => r.Description); }
            set { Store(r => r.Description, value); }
        }
        public String CircleID
        {
            get { return Retrieve(r => r.CircleID); }
            set { Store(r => r.CircleID, value); }
        }

        public String CircleGUID
        {
            get { return Retrieve(r => r.CircleGUID); }
            set { Store(r => r.CircleGUID, value); }
        }
        public String EventTitle
        {
            get { return Retrieve(r => r.EventTitle); }
            set { Store(r => r.EventTitle, value); }
        }
        public String AppPickerIds
        {
            get { return Retrieve(r => r.AppPickerIds); }
            set { Store(r => r.AppPickerIds, value); }
        }
        public String ADGroups
        {
            get { return Retrieve(r => r.ADGroups); }
            set { Store(r => r.ADGroups, value); }
        }
        public String CoverImageUrl
        {
            get { return Retrieve(r => r.CoverImageUrl); }
            set { Store(r => r.CoverImageUrl, value); }
        }
        public String VideoCoverImageUrl
        {
            get { return Retrieve(r => r.VideoCoverImageUrl); }
            set { Store(r => r.VideoCoverImageUrl, value); }
        }
        public String SkincssUrl
        {
            get { return Retrieve(r => r.SkincssUrl); }
            set { Store(r => r.SkincssUrl, value); }
        }

       public String ParticipantLayoutFullPath
        {
            get { return Retrieve(r => r.ParticipantLayoutFullPath); }
            set { Store(r => r.ParticipantLayoutFullPath, value); }
        }
        public String DocumentLayoutFullPath
        {
            get { return Retrieve(r => r.DocumentLayoutFullPath); }
            set { Store(r => r.DocumentLayoutFullPath, value); }
        }
        public String ContactPickerIds
        {
            get { return Retrieve(r => r.ContactPickerIds); }
            set { Store(r => r.ContactPickerIds, value); }
        }
        public bool EventIsPublished
        {
            get { return Retrieve(r => r.EventIsPublished); }
            set { Store(r => r.EventIsPublished, value); }
        }
        public bool EventIsLatest
        {
            get { return Retrieve(r => r.EventIsLatest); }
            set { Store(r => r.EventIsLatest, value); }
        }
    }
}
