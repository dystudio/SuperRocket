using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Orchard.Accenture.Event.Models;
using Orchard.Accenture.Event.Services;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Environment.Extensions;
using Orchard.Accenture.Event.ViewModels;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Data;
using Orchard.UI.Notify;

namespace Orchard.Accenture.Event.Drivers {

    public class EventPartDriver : ContentPartDriver<EventPart> {
        private readonly IWorkContextAccessor _wca;
        private readonly ITransactionManager _transactionManager;
        private readonly INotifier _notifier;

        public EventPartDriver(
            IWorkContextAccessor wca,
            INotifier notifier,
            ITransactionManager transactionManager)
        {

            _wca = wca;
            _transactionManager = transactionManager;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
            _notifier = notifier;
        }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        protected override string Prefix {
            get { return "Orchard.Accenture.Event.Event"; }
        }

        protected override DriverResult Display(
            EventPart part, string displayType, dynamic shapeHelper)
        {

            var shapes = new List<DriverResult>();

            shapes.Add(ContentShape(
                "Parts_Product",
                () => shapeHelper.Parts_Event(              
                    ContentPart: part
                    )
                ));

            if (part != null)
            {
                shapes.Add(ContentShape(
                        "Parts_Product_PriceTiers",
                        () => {
                            return shapeHelper.Parts_Product_PriceTiers(
                                ContentPart: part
                                );
                        })
                    );
            }
            return Combined(shapes.ToArray());
        }

       

        //GET
        protected override DriverResult Editor(EventPart part, dynamic shapeHelper)
        {

            return ContentShape("Parts_Event_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/Event",
                    Model: new EventViewModel
                    {
                        Event = part
                    },
                    Prefix: Prefix));
        }

        //POST
        protected override DriverResult Editor(
            EventPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var model = new EventViewModel
                {
                    Event = part
                };
            if (updater.TryUpdateModel(model, Prefix, null, null))
            {

                if (!string.IsNullOrEmpty(part.StartDate) && !string.IsNullOrEmpty(part.EndDate))
                {
                    var startDate = Convert.ToDateTime(part.StartDate);
                    var endDate = Convert.ToDateTime(part.EndDate);
                    if (startDate > endDate)
                    {
                        updater.AddModelError(Prefix + T("Start date can't be later than end date."), T("Start date can't be later than end date."));
                        _transactionManager.Cancel();
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(part.StartDate) && string.IsNullOrEmpty(part.EndDate))
                    {
                        updater.AddModelError(Prefix + T("Start date and End Date should not be empty."), T("Start date and End Date should not be empty."));
                    }
                    else if (string.IsNullOrEmpty(part.StartDate))
                    {
                        updater.AddModelError(Prefix + T("Start date should not be empty."), T("Start date should not be empty."));
                    }
                    else
                    {
                        updater.AddModelError(Prefix + T("End date should not be empty."), T("End date should not be empty."));
                    }
                    _transactionManager.Cancel();
                }

                List<string> contactEid = new List<string>();
                foreach (var i in model.Event.ContactPickerIds.Split(','))
                {
                    contactEid.Add(i.Trim());
                }
                int filterbefore = contactEid.Count;
                contactEid = contactEid.Distinct().ToList();
                if (filterbefore != contactEid.Count)
                {
                    _notifier.Information(T("A Contact Enterprise ID has been selected more than once, but it will only display once in the app."));

                }

            }
            return Editor(part, shapeHelper);
        }

    }
}
