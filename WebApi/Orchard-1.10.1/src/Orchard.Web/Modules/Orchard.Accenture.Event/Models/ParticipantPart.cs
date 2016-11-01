using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using System.Collections.Generic;

namespace Orchard.Accenture.Event.Models
{
    public class ParticipantPart : ContentPart<ParticipantPartRecord>
    {
        //[Required]
        public string EnterpriseId
        {
            get { return Retrieve(r => r.EnterpriseId); }
            set { Store(r => r.EnterpriseId, value); }
        }

        public string PeopleKey
        {
            get { return Retrieve(r => r.PeopleKey); }
            set { Store(r => r.PeopleKey, value); }
        }

        public string DisplayName
        {
            get { return Retrieve(r => r.DisplayName); }
            set { Store(r => r.DisplayName, value); }
        }
        public string FirstName
        {
            get { return Retrieve(r => r.FirstName); }
            set { Store(r => r.FirstName, value); }
        }
        public string LastName
        {
            get { return Retrieve(r => r.LastName); }
            set { Store(r => r.LastName, value); }
        }
        public string Avatar
        {
            get { return Retrieve(r => r.Avatar); }
            set { Store(r => r.Avatar, value); }
        }
        public string Email
        {
            get { return Retrieve(r => r.Email); }
            set { Store(r => r.Email, value); }
        }
        public string Phone
        {
            get { return Retrieve(r => r.Phone); }
            set { Store(r => r.Phone, value); }
        }
        public string WorkPhone
        {
            get { return Retrieve(r => r.WorkPhone); }
            set { Store(r => r.WorkPhone, value); }
        }
        public string ExtendNumber
        {
            get { return Retrieve(r => r.ExtendNumber); }
            set { Store(r => r.ExtendNumber, value); }
        }

        public string Country
        {
            get { return Retrieve(r => r.Country); }
            set { Store(r => r.Country, value); }
        }
        public string City
        {
            get { return Retrieve(r => r.City); }
            set { Store(r => r.City, value); }
        }
        public string TalentSegment
        {
            get { return Retrieve(r => r.TalentSegment); }
            set { Store(r => r.TalentSegment, value); }
        }
        public string CareerTrack
        {
            get { return Retrieve(r => r.CareerTrack); }
            set { Store(r => r.CareerTrack, value); }
        }

        public string CareerLevel
        {
            get { return Retrieve(r => r.CareerLevel); }
            set { Store(r => r.CareerLevel, value); }
        }
        public string DomainSpecialty
        {
            get { return Retrieve(r => r.DomainSpecialty); }
            set { Store(r => r.DomainSpecialty, value); }
        }
        public string IndustrySpecialty
        {
            get { return Retrieve(r => r.IndustrySpecialty); }
            set { Store(r => r.IndustrySpecialty, value); }
        }

        public string FirstSecondarySpecialty
        {
            get { return Retrieve(r => r.FirstSecondarySpecialty); }
            set { Store(r => r.FirstSecondarySpecialty, value); }
        }

        public string SecondSecondarySpecialty
        {
            get { return Retrieve(r => r.SecondSecondarySpecialty); }
            set { Store(r => r.SecondSecondarySpecialty, value); }
        }

        public string StandardJobCode
        {
            get { return Retrieve(r => r.StandardJobCode); }
            set { Store(r => r.StandardJobCode, value); }
        }
        public string CurrentLocation
        {
            get { return Retrieve(r => r.CurrentLocation); }
            set { Store(r => r.CurrentLocation, value); }
        }
        public string Timezone
        {
            get { return Retrieve(r => r.Timezone); }
            set { Store(r => r.Timezone, value); }
        }
        public string ActiveProjects
        {
            get { return Retrieve(r => r.ActiveProjects); }
            set { Store(r => r.ActiveProjects, value); }
        }
        public string CurrentClient
        {
            get { return Retrieve(r => r.CurrentClient); }
            set { Store(r => r.CurrentClient, value); }
        }
        public string OrgLevel2Desc
        {
            get { return Retrieve(r => r.OrgLevel2Desc); }
            set { Store(r => r.OrgLevel2Desc, value); }
        }
        public string EventIds
        {
            get { return Retrieve(r => r.EventIds); }
            set { Store(r => r.EventIds, value); }
        }
        public string ParticipantLayoutFullPath
        {
            get { return Retrieve(r => r.ParticipantLayoutFullPath); }
            set { Store(r => r.ParticipantLayoutFullPath, value); }
        }
        public string ProfessionalBio
        {
            get { return Retrieve(r => r.ProfessionalBio); }
            set { Store(r => r.ProfessionalBio, value); }
        }
        public string MediaUrl
        {
            get { return Retrieve(r => r.MediaUrl); }
            set { Store(r => r.MediaUrl, value); }
        }

        public bool ParticipantIsPublished
        {
            get { return Retrieve(r => r.ParticipantIsPublished); }
            set { Store(r => r.ParticipantIsPublished, value); }
        }
        public bool ParticipantIsLatest
        {
            get { return Retrieve(r => r.ParticipantIsLatest); }
            set { Store(r => r.ParticipantIsLatest, value); }
        }
    }
}
