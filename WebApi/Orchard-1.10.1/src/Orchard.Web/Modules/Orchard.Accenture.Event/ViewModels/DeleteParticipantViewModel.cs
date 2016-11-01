using Orchard.ContentManagement;
using System.Collections.Generic;

namespace Orchard.Accenture.Event.ViewModels
{
    public class DeleteParticipantViewModel
    {
        public int CurrentEventId { get; set; }
        public IEnumerable<ContentItem> Events { get; set; }
    }
}