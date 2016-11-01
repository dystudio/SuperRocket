using Orchard.Accenture.Event.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Models;
using System;
using System.Collections.Generic;

namespace Orchard.Accenture.Event {

    public class SessionPartHandler : ContentHandler {
        public SessionPartHandler(IRepository<SessionPartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));


            OnPublishing<SessionPart>((context, part) => {

                var title = ((dynamic)part.ContentItem).SessionPart.TitlePart.Title;
                var startTime = ((dynamic)part.ContentItem).SessionPart.StartTime.DateTime;
                var endTime = ((dynamic)part.ContentItem).SessionPart.EndTime.DateTime;
                var sessionType = ((dynamic)part.ContentItem).SessionPart.SessionType.Terms;
                var sessionCategory = ((dynamic)part.ContentItem).SessionPart.SessionCategory.Terms;
                var aDGroupTerms = ((dynamic)part.ContentItem).SessionPart.ADGroup.Terms;
                var sessionDescription = ((dynamic)part.ContentItem).SessionPart.BodyPart.Text;
                var presenterPickerIds = ((dynamic)part.ContentItem).SessionPart.AgendaPresenterPickerIds;
                var eventPickerIds = ((dynamic)part.ContentItem).SessionPart.EventPicker.Ids;

                part.AgendaTitle = title;
                part.AgendaStartTime = ConvertFromLocalizedString(startTime);
                part.AgendaEndTime = ConvertFromLocalizedString(endTime);
                part.AgendaType = string.Join(",", ConvertToStringList(sessionType));
                part.AgendaCategory = string.Join(",", ConvertToStringList(sessionCategory));
                part.AgendaADGroups = string.Join(",", ConvertToStringList(aDGroupTerms));
                part.AgendaFullDescription = sessionDescription;
                part.AgendaPresenterPickerIds = presenterPickerIds;
                part.AgendaEventPickerIds = string.Join(",", eventPickerIds);
                //When Publish update these two fields to true
                part.SessionIsPublished = true;
                part.SessionIsLatest = true;

            });

            OnUnpublishing<SessionPart>((context, part) => {
                //Unpublish only update IsLatest = false EventIsPublished = false
                part.SessionIsLatest = false;
                part.SessionIsPublished = false;
            });

            OnUpdated<SessionPart>((context, part) => {
                //Get the IsPublished and IsLatest, then update the event record accordingly.
                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                part.SessionIsPublished = published;
                part.SessionIsLatest = !hasDraft;
            });
            OnRemoving<SessionPart>((context, part) => {
                repository.Delete(part.Record);
            });
            OnLoaded<SessionPart>((context, part) =>
            {
                var title = ((dynamic)part.ContentItem).SessionPart.TitlePart.Title;
                var startTime = ((dynamic)part.ContentItem).SessionPart.StartTime.DateTime;
                var endTime = ((dynamic)part.ContentItem).SessionPart.EndTime.DateTime;
                var sessionType = ((dynamic)part.ContentItem).SessionPart.SessionType.Terms;
                var sessionCategory = ((dynamic)part.ContentItem).SessionPart.SessionCategory.Terms;
                var aDGroupTerms = ((dynamic)part.ContentItem).SessionPart.ADGroup.Terms;
                var sessionDescription = ((dynamic)part.ContentItem).SessionPart.BodyPart.Text;
                var presenterPickerIds = ((dynamic)part.ContentItem).SessionPart.AgendaPresenterPickerIds;
                var eventPickerIds = ((dynamic)part.ContentItem).SessionPart.EventPicker.Ids;



                var currentRecord = repository.Get(part.ContentItem.Id);
                currentRecord.AgendaStartTime = ConvertFromLocalizedString(startTime);
                currentRecord.AgendaEndTime = ConvertFromLocalizedString(endTime);
                currentRecord.AgendaType = string.Join(",", ConvertToStringList(sessionType));
                currentRecord.AgendaCategory = string.Join(",", ConvertToStringList(sessionCategory));
                currentRecord.AgendaADGroups = string.Join(",", ConvertToStringList(aDGroupTerms));
                currentRecord.AgendaFullDescription = sessionDescription;
                currentRecord.AgendaPresenterPickerIds = presenterPickerIds;
                currentRecord.AgendaEventPickerIds = string.Join(",", eventPickerIds);

                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                currentRecord.SessionIsPublished = published;
                currentRecord.SessionIsLatest = !hasDraft;

                repository.Update(currentRecord);

            });
        }
         protected override void GetItemMetadata(GetContentItemMetadataContext context) {
             var part = context.ContentItem.As<SessionPart>();

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
