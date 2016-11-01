//using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization;
using Orchard.Accenture.Event.Models;

namespace Orchard.Accenture.Event.Handlers
{
    //[UsedImplicitly]
    public class PeopleServiceSettingsPartHandler : ContentHandler {
        public PeopleServiceSettingsPartHandler() {
            T = NullLocalizer.Instance;
            Filters.Add(new ActivatingFilter<PeopleServiceSettingsPart>("Site"));
            Filters.Add(new TemplateFilterForPart<PeopleServiceSettingsPart>("PeopleServiceSettings", "Parts/PeopleService.PeopleServiceSettings", "People Service"));
        }

        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context) {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("People Service")));
        }
    }
}