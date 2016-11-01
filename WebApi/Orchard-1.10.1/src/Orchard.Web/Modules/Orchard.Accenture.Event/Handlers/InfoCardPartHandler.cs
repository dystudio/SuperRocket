using Orchard.Accenture.Event.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using System;

namespace Orchard.Accenture.Event.Handles
{
    public class InfoCardPartHandler : ContentHandler
    {
        public InfoCardPartHandler(IRepository<InfoCardPartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));

            OnPublishing<InfoCardPart>((context, part) => {

                var title = ((dynamic)part.ContentItem).InfoCardPart.TitlePart.Title;
                var startDate = ((dynamic)part.ContentItem).InfoCardPart.StartDate.DateTime;
                var endDate = ((dynamic)part.ContentItem).InfoCardPart.EndDate.DateTime;
                var eventPickerIds = ((dynamic)part.ContentItem).InfoCardPart.EventPicker.Ids;

                part.Title = title;
                part.CardStartDate = Convert.ToString(startDate);
                part.CardEndDate = Convert.ToString(endDate);
                part.EventPickerIds = string.Join(",", eventPickerIds);

                //When Publish update these two fields to true
                part.InfoCardIsPublished = true;
                part.InfoCardIsLatest = true;

            });

            OnUnpublishing<InfoCardPart>((context, part) => {
                //Unpublish only update IsLatest = false EventIsPublished = false
                part.InfoCardIsLatest = false;
                part.InfoCardIsPublished = false;
            });

            OnUpdated<InfoCardPart>((context, part) => {
                //Get the IsPublished and IsLatest, then update the event record accordingly.
                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                part.InfoCardIsPublished = published;
                part.InfoCardIsLatest = !hasDraft;
            });

            OnRemoving<InfoCardPart>((context, part) => {
                repository.Delete(part.Record);
            });

            OnLoaded<InfoCardPart>((context, part) =>
            {
                var title = ((dynamic)part.ContentItem).InfoCardPart.TitlePart.Title;
                var startDate = ((dynamic)part.ContentItem).InfoCardPart.StartDate.DateTime;
                var endDate = ((dynamic)part.ContentItem).InfoCardPart.EndDate.DateTime;
                var eventPickerIds = ((dynamic)part.ContentItem).InfoCardPart.EventPicker.Ids;
                var cardCoverImageUrl =  ((dynamic)part.ContentItem).InfoCardPart.CoverImage.FirstMediaUrl;

                var currentRecord = repository.Get(part.ContentItem.Id);
                currentRecord.Title = title;
                currentRecord.CardStartDate = Convert.ToString(startDate);
                currentRecord.CardEndDate = Convert.ToString(endDate);
                currentRecord.EventPickerIds = string.Join(",", eventPickerIds);
                currentRecord.CardCoverImageUrl = cardCoverImageUrl;

                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                currentRecord.InfoCardIsPublished = published;
                currentRecord.InfoCardIsLatest = !hasDraft;

                repository.Update(currentRecord);

            });
        }

        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            //var part = context.ContentItem.As<InfoCardPart>();

            //if (part != null)
            //{
            //    context.Metadata.Identity.Add("Sku", part.Sku);
            //}
        }
    }
}