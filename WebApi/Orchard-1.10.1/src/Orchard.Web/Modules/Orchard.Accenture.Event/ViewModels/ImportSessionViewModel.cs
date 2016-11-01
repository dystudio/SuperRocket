using Orchard.ContentManagement;
using System.Collections.Generic;

namespace Orchard.Accenture.Event.ViewModels
{
    public class ImportSessionViewModel
    {
        public IEnumerable<ContentItem> Events { get; set; }
        public int CurrentEventId { get; set; }
        public string Owner { get; set; }
    }
}