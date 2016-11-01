using Orchard.Accenture.Event.Common;
using Orchard.Accenture.Event.Models;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;

namespace Orchard.Accenture.Event {

    public class AppPartHandler : ContentHandler {
        public AppPartHandler(IRepository<AppPartRecord> repository, ISignals signals)
        {
            Filters.Add(StorageFilter.For(repository));

            OnPublished<AppPart>((context, part) => signals.Trigger(CacheAndSignals.APP_CACHE_SIGNAL));
        }
         protected override void GetItemMetadata(GetContentItemMetadataContext context) {
             var part = context.ContentItem.As<EventPart>();

            if (part != null) {
                
            }
        }
    }
}
