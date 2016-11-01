using Orchard.ContentManagement.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class AppPartRecord : ContentPartRecord
    {
        public AppPartRecord()
        {

        }
        public virtual String WelcomeVideoLink { get; set; }
        public virtual String WelcomeTitle { get; set; }
        public virtual String DescriptionContext { get; set; }
        public virtual String MachineName { get; set; }
        public virtual String Message { get; set; }
        public virtual String AcceptText { get; set; }
        public virtual String DisagreeText { get; set; }
       
    }
}