using Orchard.ContentManagement;
using Orchard.ContentManagement.FieldStorage.InfosetStorage;

namespace Orchard.Accenture.PushNotification.Models
{
    public class PushServiceSettingsPart : ContentPart {

       

        public string UserName
        {
            get { return this.As<InfosetPart>().Get<PushServiceSettingsPart>("UserName"); }
            set { this.As<InfosetPart>().Set<PushServiceSettingsPart>("UserName", value); }
        }

        public string Password
        {
            get { return this.As<InfosetPart>().Get<PushServiceSettingsPart>("Password"); }
            set { this.As<InfosetPart>().Set<PushServiceSettingsPart>("Password", value); }
        }
        public string Scope
        {
            get { return this.As<InfosetPart>().Get<PushServiceSettingsPart>("Scope"); }
            set { this.As<InfosetPart>().Set<PushServiceSettingsPart>("Scope", value); }
        }
        public string Endpoint
        {
            get { return this.As<InfosetPart>().Get<PushServiceSettingsPart>("Endpoint"); }
            set { this.As<InfosetPart>().Set<PushServiceSettingsPart>("Endpoint", value); }
        }

        public string PushTenant
        {
            get { return this.As<InfosetPart>().Get<PushServiceSettingsPart>("PushTenant"); }
            set { this.As<InfosetPart>().Set<PushServiceSettingsPart>("PushTenant", value); }
        }

        public string PushTopic
        {
            get { return this.As<InfosetPart>().Get<PushServiceSettingsPart>("PushTopic"); }
            set { this.As<InfosetPart>().Set<PushServiceSettingsPart>("PushTopic", value); }
        }
    }
}