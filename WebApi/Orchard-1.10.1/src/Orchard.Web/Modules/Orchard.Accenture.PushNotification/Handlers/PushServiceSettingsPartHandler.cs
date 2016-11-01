
using Orchard.Accenture.PushNotification.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization;

namespace Orchard.Accenture.PushNotification.Handlers
{

    public class PushServiceSettingsPartHandler : ContentHandler {
        public PushServiceSettingsPartHandler() {
            T = NullLocalizer.Instance;
            Filters.Add(new ActivatingFilter<PushServiceSettingsPart>("Site"));
            Filters.Add(new TemplateFilterForPart<PushServiceSettingsPart>("PushServiceSettings", "Parts/PushService.PushServiceSettings", "Accenture Notifier"));
        }

        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context) {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Accenture Notifier")));
        }
    }
}