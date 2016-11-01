using Orchard.Accenture.Event.ServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Accenture.Event.Models;

namespace Orchard.Accenture.Event.ViewModels
{
    public class EventViewModel
    {
        public IEnumerable<EventModel> Entities { get; set; }

        public EventSearch Search { get; set; }
        public EventSort Sort { get; set; }

        public dynamic Pager { get; set; }
        public dynamic Table { get; set; }
        public dynamic SearchDialog { get; set; }
        public EventPart Event { get; set; }
    }
}