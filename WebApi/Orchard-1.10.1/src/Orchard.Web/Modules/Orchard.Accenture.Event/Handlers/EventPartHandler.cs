using Orchard.Accenture.Event.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Models;
using System.Collections.Generic;

namespace Orchard.Accenture.Event
{

    public class EventPartHandler : ContentHandler
    {
        public EventPartHandler(IRepository<EventPartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));

            OnPublishing<EventPart>((context, part) =>
            {
                var title = ((dynamic)part.ContentItem).EventPart.TitlePart.Title;
                var appPickerIds = ((dynamic)part.ContentItem).EventPart.AppPicker.Ids;
                var contactPickerIds = ((dynamic)part.ContentItem).EventPart.ContactPickerIds;
                var aDGroupTerms = ((dynamic)part.ContentItem).EventPart.ADGroup.Terms;
                var participantLayoutTerms = ((dynamic)part.ContentItem).EventPart.ParticipantLayout.Terms;
                var documentLayoutTerms = ((dynamic)part.ContentItem).EventPart.DocumentLayout.Terms;

                part.EventTitle = title;
                if (appPickerIds != null)
                {
                    part.AppPickerIds = string.Join(",", appPickerIds);
                }
                if (contactPickerIds != null)
                {
                    part.ContactPickerIds = contactPickerIds;
                }

                part.ADGroups = string.Join(",", ConvertToStringList(aDGroupTerms));
                part.ParticipantLayoutFullPath = string.Join(",", ConvertToPathList(participantLayoutTerms));
                part.DocumentLayoutFullPath = string.Join(",", ConvertToPathList(documentLayoutTerms));
                //When Publish update these two fields to true
                part.EventIsPublished = true;
                part.EventIsLatest = true;
                
            });
            OnRemoving<EventPart>((context, part) => {
                repository.Delete(part.Record);
            });

            OnUnpublishing<EventPart>((context, part) => {
                //Unpublish only update IsLatest = false EventIsPublished = false
                part.EventIsLatest = false;
                part.EventIsPublished = false;
            });

            OnUpdated<EventPart>((context, part) => {
                //Get the IsPublished and IsLatest, then update the event record accordingly.
                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                part.EventIsPublished = published;
                part.EventIsLatest = !hasDraft;
            });

            OnLoaded<EventPart>((context, part) =>
            {
                var title = ((dynamic)part.ContentItem).EventPart.TitlePart.Title;
                var appPickerIds = ((dynamic)part.ContentItem).EventPart.AppPicker.Ids;
                var contactPickerIds = ((dynamic)part.ContentItem).EventPart.ContactPickerIds;
                var aDGroupTerms = ((dynamic)part.ContentItem).EventPart.ADGroup.Terms;
                var participantLayoutTerms = ((dynamic)part.ContentItem).EventPart.ParticipantLayout.Terms;
                var documentLayoutTerms = ((dynamic)part.ContentItem).EventPart.DocumentLayout.Terms;

                var coverImageUrl = ((dynamic)part.ContentItem).EventPart.CoverImage.FirstMediaUrl;
                var skincssUrl = ((dynamic)part.ContentItem).EventPart.EventSkinFile.FirstMediaUrl;
                var videoCoverImageUrl = ((dynamic)part.ContentItem).EventPart.VideoCoverImage.FirstMediaUrl;


                var currentRecord = repository.Get(part.ContentItem.Id);
                currentRecord.EventTitle = title;
                currentRecord.AppPickerIds = string.Join(",", appPickerIds); ;
                currentRecord.ContactPickerIds =contactPickerIds;
                currentRecord.ADGroups = string.Join(",", ConvertToStringList(aDGroupTerms));
                currentRecord.ParticipantLayoutFullPath = string.Join(",", ConvertToPathList(participantLayoutTerms));
                currentRecord.DocumentLayoutFullPath = string.Join(",", ConvertToPathList(documentLayoutTerms));

                currentRecord.CoverImageUrl = coverImageUrl;
                currentRecord.SkincssUrl = skincssUrl;
                currentRecord.VideoCoverImageUrl = videoCoverImageUrl;

                repository.Update(currentRecord);

            });
        }
        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            var part = context.ContentItem.As<EventPart>();

            if (part != null)
            {

            }
        }

        private List<string> ConvertToStringList(List<TermPart> terms)
        {
            List<string> groups = new List<string>();
            foreach (var item in terms)
            {
                groups.Add(item.Name);
            }
            return groups;
        }
        private List<string> ConvertToPathList(List<TermPart> terms)
        {
            List<string> groups = new List<string>();
            foreach (var item in terms)
            {
                groups.Add(item.FullPath);
            }
            return groups;
        }
    }
}
