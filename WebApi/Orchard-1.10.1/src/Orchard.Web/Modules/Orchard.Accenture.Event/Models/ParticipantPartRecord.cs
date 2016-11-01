using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class ParticipantPartRecord : ContentPartRecord
    {
        public ParticipantPartRecord()
        {

        }
        public virtual string EnterpriseId { get; set; }
        public virtual string PeopleKey { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        [StringLengthMax]
        public virtual string Avatar { get; set; }
        public virtual string Email { get; set; }
        public virtual string Phone { get; set; }
        public virtual string WorkPhone { get; set; }
        public virtual string ExtendNumber { get; set; }

        public virtual string Country { get; set; }
        public virtual string City { get; set; }

        public virtual string TalentSegment { get; set; }
        public virtual string CareerTrack { get; set; }
        public virtual string CareerLevel { get; set; }
        public virtual string DomainSpecialty { get; set; }
        public virtual string IndustrySpecialty { get; set; }
        public virtual string FirstSecondarySpecialty { get; set; }
        public virtual string SecondSecondarySpecialty { get; set; }

        public virtual string StandardJobCode { get; set; } // new request from profile
        public virtual string CurrentLocation { get; set; }
        public virtual string Timezone { get; set; }
        [StringLengthMax]
        public virtual string ActiveProjects { get; set; }
        public virtual string CurrentClient { get; set; }
        public virtual string OrgLevel2Desc { get; set; }
        public virtual string EventIds { get; set; }
        public virtual string ParticipantLayoutFullPath { get; set; }
        [StringLengthMax]
        public virtual string ProfessionalBio { get; set; }
        public virtual string MediaUrl { get; set; }
        public virtual bool ParticipantIsPublished { get; set; }
        public virtual bool ParticipantIsLatest { get; set; }
    }
}