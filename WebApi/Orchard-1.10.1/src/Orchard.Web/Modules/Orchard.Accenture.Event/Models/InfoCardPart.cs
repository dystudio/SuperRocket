using Orchard.ContentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class InfoCardPart : ContentPart<InfoCardPartRecord>
    {
        public string HotelName
        {
            get { return Retrieve(r => r.HotelName); }
            set { Store(r => r.HotelName, value); }
        }
        public string HotelAddress
        {
            get { return Retrieve(r => r.HotelAddress); }
            set { Store(r => r.HotelAddress, value); }
        }
        public string WebSite
        {
            get { return Retrieve(r => r.WebSite); }
            set { Store(r => r.WebSite, value); }
        }
        public string Telphone
        {
            get { return Retrieve(r => r.Telphone); }
            set { Store(r => r.Telphone, value); }
        }
        public string ExtNumber
        {
            get { return Retrieve(r => r.ExtNumber); }
            set { Store(r => r.ExtNumber, value); }
        }

        public string Title
        {
            get { return Retrieve(r => r.Title); }
            set { Store(r => r.Title, value); }
        }
        public string CardStartDate
        {
            get { return Retrieve(r => r.CardStartDate); }
            set { Store(r => r.CardStartDate, value); }
        }
        public string CardEndDate
        {
            get { return Retrieve(r => r.CardEndDate); }
            set { Store(r => r.CardEndDate, value); }
        }
        public string CardCoverImageUrl
        {
            get { return Retrieve(r => r.CardCoverImageUrl); }
            set { Store(r => r.CardCoverImageUrl, value); }
        }
        public string EventPickerIds
        {
            get { return Retrieve(r => r.EventPickerIds); }
            set { Store(r => r.EventPickerIds, value); }
        }
        public bool InfoCardIsPublished
        {
            get { return Retrieve(r => r.InfoCardIsPublished); }
            set { Store(r => r.InfoCardIsPublished, value); }
        }
        public bool InfoCardIsLatest
        {
            get { return Retrieve(r => r.InfoCardIsLatest); }
            set { Store(r => r.InfoCardIsLatest, value); }
        }
    }
}