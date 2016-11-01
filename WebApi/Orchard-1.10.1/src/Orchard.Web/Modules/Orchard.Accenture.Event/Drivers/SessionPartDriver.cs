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
using Orchard.Fields.Fields;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Data;
using Orchard.UI.Notify;

namespace Orchard.Accenture.Event.Drivers {

    public class SessionPartDriver : ContentPartDriver<SessionPart> {
        private readonly IWorkContextAccessor _wca;
        private readonly ITransactionManager _transactionManager;
        private readonly INotifier _notifier;

        public SessionPartDriver(
            IWorkContextAccessor wca,
            INotifier notifier,
            ITransactionManager transactionManager) {

            _wca = wca;
            _transactionManager = transactionManager;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
            _notifier = notifier;
        }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        protected override string Prefix {
            get { return "Orchard.Accenture.Event.Session"; }
        }

        protected override DriverResult Display(
            SessionPart part, string displayType, dynamic shapeHelper)
        {

            var shapes = new List<DriverResult>();

            shapes.Add(ContentShape(
                "Parts_Product",
                () => shapeHelper.Parts_Session(              
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
        protected override DriverResult Editor(SessionPart part, dynamic shapeHelper)
        {

            return ContentShape("Parts_Session_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/Session",
                    Model: new SessionEditorViewModel
                    {
                        Session = part
                    },
                    Prefix: Prefix));
        }

        //POST
        protected override DriverResult Editor(
            SessionPart part, IUpdateModel updater, dynamic shapeHelper)
        {

            var model = new SessionEditorViewModel
                {
                    Session = part
                };
            if (updater.TryUpdateModel(model, Prefix, null, null)) {

              var startTime =   ((DateTimeField)((dynamic)part.ContentItem).SessionPart.StartTime).DateTime;
              var endTime =   ((DateTimeField)((dynamic)part.ContentItem).SessionPart.EndTime).DateTime;

                if (startTime > endTime)
                {
                    updater.AddModelError(Prefix + T("Start time can't be later than end time."), T("Start time can't be later than end time."));
                    _transactionManager.Cancel();
                }

                List<string> presenterEid = new List<string>();
                foreach (var i in model.Session.AgendaPresenterPickerIds.Split(','))
                {
                    presenterEid.Add(i.Trim());
                }
                int filterbefore = presenterEid.Count;
                presenterEid = presenterEid.Distinct().ToList();
                if (filterbefore != presenterEid.Count)
                {
                    _notifier.Information(T("A Presenter Enterprise ID has been selected more than once, but it will only display once in the app."));
                    
                }
            }
            return Editor(part, shapeHelper);
        }

    }
}
