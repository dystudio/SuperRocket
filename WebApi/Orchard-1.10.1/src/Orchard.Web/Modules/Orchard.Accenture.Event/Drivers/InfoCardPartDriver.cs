using Orchard.Accenture.Event.Models;
using Orchard.Accenture.Event.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Data;
using Orchard.Fields.Fields;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Accenture.Event.Drivers
{
    public class InfoCardPartDriver : ContentPartDriver<InfoCardPart>
    {
        private readonly IWorkContextAccessor _wca;
        private readonly ITransactionManager _transactionManager;

        public InfoCardPartDriver(
           IWorkContextAccessor wca,
            ITransactionManager transactionManager)
        {

            _wca = wca;
            _transactionManager = transactionManager;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        protected override string Prefix
        {
            get { return "Orchard.Accenture.Event.InfoCard"; }
        }

        protected override DriverResult Editor(InfoCardPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_InfoCard_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/InfoCard",
                    Model: new InfoCardViewModel
                    {
                        InfoCardPart = part
                    },
                    Prefix: Prefix));
        }

        protected override DriverResult Editor(InfoCardPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var model = new InfoCardViewModel()
            {
                InfoCardPart = part
            };

            if (updater.TryUpdateModel(model, Prefix, null, null))
            {
                var startTime = ((DateTimeField)((dynamic)part.ContentItem).InfoCardPart.StartDate).DateTime;
                var endTime = ((DateTimeField)((dynamic)part.ContentItem).InfoCardPart.EndDate).DateTime;

                if (startTime.Date.Year != 1 && endTime.Date.Year != 1)
                {
                    if (startTime > endTime)
                    {
                        updater.AddModelError(Prefix + T("Start date can't be later than end date."), T("Start date can't be later than end date."));
                        _transactionManager.Cancel();
                    } 
                }
            }

            return Editor(part, shapeHelper);
        }
    }
}