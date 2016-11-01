using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class ContentItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Owner { get; set; }
        public DateTime? CreateTime { get; set; }
        public Dictionary<string, object> Fields { get; set; }
    }
}