using Orchard.Accenture.Event.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Models;
using System;
using System.Collections.Generic;

namespace Orchard.Accenture.Event {

    public class CirclePartHandler : ContentHandler {
        public CirclePartHandler(IRepository<CirclePartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));

            OnPublishing<CirclePart>((context, part) => {

                var title = ((dynamic)part.ContentItem).CirclePart.TitlePart.Title;
                var circleId = ((dynamic)part.ContentItem).CirclePart.WorkCircleId.Value;
                var cirlceGUID = ((dynamic)part.ContentItem).CirclePart.WorkCircleGUID.Value;
                var eventPickerIds = ((dynamic)part.ContentItem).CirclePart.EventPicker.Ids;
                var aDGroupTerms = ((dynamic)part.ContentItem).CirclePart.ADGroup.Terms;

                part.AdGroups = string.Join(",", ConvertToStringList(aDGroupTerms));
                part.Title = title;
                part.AnotherCircleId = circleId;
                part.AnotherCircleGUID = cirlceGUID;
                part.EventPickerIds = string.Join(",", eventPickerIds);

                //When Publish update these two fields to true
                part.CircleIsPublished = true;
                part.CircleIsLatest = true;

            });

            OnUnpublishing<CirclePart>((context, part) => {
                //Unpublish only update IsLatest = false EventIsPublished = false
                part.CircleIsLatest = false;
                part.CircleIsPublished = false;
            });

            OnUpdated<CirclePart>((context, part) => {
                //Get the IsPublished and IsLatest, then update the event record accordingly.
                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                part.CircleIsPublished = published;
                part.CircleIsLatest = !hasDraft;
            });

            OnRemoving<CirclePart>((context, part) => {
                repository.Delete(part.Record);
            });

            OnLoaded<CirclePart>((context, part) =>
            {
                var title = ((dynamic)part.ContentItem).CirclePart.TitlePart.Title;
                var circleId = ((dynamic)part.ContentItem).CirclePart.WorkCircleId.Value;
                var cirlceGUID = ((dynamic)part.ContentItem).CirclePart.WorkCircleGUID.Value;
                var eventPickerIds = ((dynamic)part.ContentItem).CirclePart.EventPicker.Ids;
                var aDGroupTerms = ((dynamic)part.ContentItem).CirclePart.ADGroup.Terms;

                var currentRecord = repository.Get(part.ContentItem.Id);
                currentRecord.Title = title;
                currentRecord.AnotherCircleId = circleId;
                currentRecord.AnotherCircleGUID = cirlceGUID;
                currentRecord.EventPickerIds = string.Join(",", eventPickerIds);
                currentRecord.AdGroups = string.Join(",", ConvertToStringList(aDGroupTerms));


                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                currentRecord.CircleIsPublished = published;
                currentRecord.CircleIsLatest = !hasDraft;

                repository.Update(currentRecord);

            });
        }
         protected override void GetItemMetadata(GetContentItemMetadataContext context) {
             var part = context.ContentItem.As<CirclePart>();

            if (part != null) {
                
            }
        }
        private string ConvertFromLocalizedString(DateTime utcTime)
        {
            var result = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc).ToString("O");
            return result;
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
