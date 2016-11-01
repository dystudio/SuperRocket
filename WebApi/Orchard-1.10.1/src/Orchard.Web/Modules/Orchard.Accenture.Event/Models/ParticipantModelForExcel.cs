using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Models
{
    public class ParticipantModelForExcel
    {
        public string EnterpriseId { get; set; }
        public string UserGroup { get; set; }
        public string RowNumber { get; set; }
    }
}