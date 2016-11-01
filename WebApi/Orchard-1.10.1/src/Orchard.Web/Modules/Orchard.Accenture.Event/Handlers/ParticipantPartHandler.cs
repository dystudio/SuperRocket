using Orchard.Accenture.Event.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Models;
using System;
using System.Collections.Generic;
using Orchard.MediaLibrary;
namespace Orchard.Accenture.Event {

    public class ParticipantPartHandler : ContentHandler {
        public ParticipantPartHandler(IRepository<ParticipantPartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));
            OnPublishing<ParticipantPart>((context, part) => {
                int[] ids = ((dynamic)part.ContentItem).ParticipantPart.EventPicker.Ids;
                if (ids != null)
                {
                    part.EventIds = string.Join(",", ids);
                }

                IEnumerable<TermPart> layout = ((dynamic)part.ContentItem).ParticipantPart.ParticipantLayout.Terms;
                if (layout != null)
                {
                    List<string> fullPathList = new List<string>();
                    foreach (var item in layout)
                    {
                        fullPathList.Add(item.FullPath);
                    }
                    part.ParticipantLayoutFullPath = string.Join(",", fullPathList);
                }

                var bio = ((dynamic)part.ContentItem).ParticipantPart.BodyPart.Text;
                if (!string.IsNullOrEmpty(bio))
                {
                    part.ProfessionalBio = bio;
                }

                IEnumerable<MediaLibrary.Models.MediaPart> mediaParts = ((dynamic)part.ContentItem).ParticipantPart.ParticipantAvatar.MediaParts;
                
                if (mediaParts != null)
                {
                    part.MediaUrl = string.Empty;
                }

                //When Publish update these two fields to true
                part.ParticipantIsLatest = true;
                part.ParticipantIsPublished = true;

            });
            OnUnpublishing<ParticipantPart>((context, part) => {
                //Unpublish only update IsLatest = false EventIsPublished = false
                part.ParticipantIsLatest = false;
                part.ParticipantIsPublished = false;
            });

            OnUpdated<ParticipantPart>((context, part) => {
                //Get the IsPublished and IsLatest, then update the event record accordingly.
                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                part.ParticipantIsPublished = published;
                part.ParticipantIsLatest = !hasDraft;
            });
            OnRemoving<ParticipantPart>((context, part) => {
                repository.Delete(part.Record);
            });

            OnLoaded<ParticipantPart>((context, part) => {
                var mediaUrl = ((dynamic)part.ContentItem).ParticipantPart.ParticipantAvatar.FirstMediaUrl;
                var currentRecord = repository.Get(part.ContentItem.Id);
                currentRecord.MediaUrl = mediaUrl;
                var published = part.ContentItem.HasPublished();
                var hasDraft = part.ContentItem.HasDraft();

                currentRecord.ParticipantIsPublished = published;
                currentRecord.ParticipantIsLatest = !hasDraft;
                repository.Update(currentRecord);

            });
            
        }
         protected override void GetItemMetadata(GetContentItemMetadataContext context) {
             var part = context.ContentItem.As<ParticipantPart>();

            if (part != null) {
                //context.Metadata.Identity.Add("Sku", part.Sku);
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
