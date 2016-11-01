using Orchard.ContentManagement;
using Orchard.ContentManagement.FieldStorage.InfosetStorage;

namespace Orchard.Accenture.Event.Models
{
    public class PeopleServiceSettingsPart : ContentPart {

        public string BaseUrl {
            get { return this.As<InfosetPart>().Get<PeopleServiceSettingsPart>("BaseUrl"); }
            set { this.As<InfosetPart>().Set<PeopleServiceSettingsPart>("BaseUrl", value); }
        }

        public string Domain
        {
            get { return this.As<InfosetPart>().Get<PeopleServiceSettingsPart>("Domain"); }
            set { this.As<InfosetPart>().Set<PeopleServiceSettingsPart>("Domain", value); }
        }

        public string UserName
        {
            get { return this.As<InfosetPart>().Get<PeopleServiceSettingsPart>("UserName"); }
            set { this.As<InfosetPart>().Set<PeopleServiceSettingsPart>("UserName", value); }
        }

        public string Password
        {
            get { return this.As<InfosetPart>().Get<PeopleServiceSettingsPart>("Password"); }
            set { this.As<InfosetPart>().Set<PeopleServiceSettingsPart>("Password", value); }
        }

        public string Endpoint
        {
            get { return this.As<InfosetPart>().Get<PeopleServiceSettingsPart>("Endpoint"); }
            set { this.As<InfosetPart>().Set<PeopleServiceSettingsPart>("Endpoint", value); }
        }

        public string Scope
        {
            get { return this.As<InfosetPart>().Get<PeopleServiceSettingsPart>("Scope"); }
            set { this.As<InfosetPart>().Set<PeopleServiceSettingsPart>("Scope", value); }
        }
    }
}