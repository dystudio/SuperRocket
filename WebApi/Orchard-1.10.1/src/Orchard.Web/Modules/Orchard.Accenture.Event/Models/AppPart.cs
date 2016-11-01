using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using System.Collections.Generic;
using System;

namespace Orchard.Accenture.Event.Models
{
    public class AppPart : ContentPart<AppPartRecord>
    {
        public String WelcomeVideoLink
        {
            get { return Retrieve(r => r.WelcomeVideoLink); }
            set { Store(r => r.WelcomeVideoLink, value); }
        }

        public String WelcomeTitle
        {
            get { return Retrieve(r => r.WelcomeTitle); }
            set { Store(r => r.WelcomeTitle, value); }
        }

        public String DescriptionContext
        {
            get { return Retrieve(r => r.DescriptionContext); }
            set { Store(r => r.DescriptionContext, value); }
        }

        public String MachineName
        {
            get { return Retrieve(r => r.MachineName); }
            set { Store(r => r.MachineName, value); }
        }
        public String Message
        {
            get { return Retrieve(r => r.Message); }
            set { Store(r => r.Message, value); }
        }
        public String AcceptText
        {
            get { return Retrieve(r => r.AcceptText); }
            set { Store(r => r.AcceptText, value); }
        }
        public String DisagreeText
        {
            get { return Retrieve(r => r.DisagreeText); }
            set { Store(r => r.DisagreeText, value); }
        }

    }
}
